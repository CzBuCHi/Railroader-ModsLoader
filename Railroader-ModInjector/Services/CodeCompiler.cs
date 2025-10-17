using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Mono.Cecil;
using Railroader.ModInjector.PluginWrappers;
using Railroader.ModInjector.Wrappers;
using Railroader.ModInterfaces;
using ILogger = Serilog.ILogger;

namespace Railroader.ModInjector.Services;

/// <summary>
/// Defines a contract for compiling mod source code and applying patches to the resulting assembly.
/// </summary>
internal interface ICodeCompiler
{
    /// <summary> Gets or sets the mapping of plugin marker interfaces to their corresponding patcher types. </summary>
    /// <remarks>Plugins are applied in reverse order.</remarks>
    List<(Type InterfaceType, Type PluginPatcherType)> PluginPatchers { get; set; }

    /// <summary>
    /// Gets or sets the names of reference assemblies required for compilation.
    /// </summary>
    string[] ReferenceNames { get; set; }

    /// <summary>
    /// Compiles the mod's source code and applies patches to the resulting assembly.
    /// </summary>
    /// <param name="definition">The mod definition containing the base path and identifier. Must not be null.</param>
    /// <returns>The path to the compiled and patched assembly DLL, or null if compilation or patching fails.</returns>
    string? CompileMod(ModDefinition definition);
}

/// <summary> Compiles mod source code into a DLL and applies registered patches to the assembly. </summary>
/// <param name="fileSystem">The file system abstraction for accessing source files and assemblies. Must not be null.</param>
/// <param name="compilerCallableEntryPoint">The entry point for invoking the compiler. Must not be null.</param>
/// <param name="logger">The logger for diagnostic messages. Must not be null.</param>
internal sealed class CodeCompiler(IFileSystem fileSystem, ICompilerCallableEntryPoint compilerCallableEntryPoint, ILogger logger) : ICodeCompiler
{
    private readonly ConcurrentDictionary<Type, IPluginPatcher> _PluginPatchers = new();

    /// <inheritdoc />
    public List<(Type InterfaceType, Type PluginPatcherType)> PluginPatchers { get; set; } = [
        (typeof(ITopRightButtonPlugin), typeof(TopRightButtonPluginPatcher)),
        (typeof(IHarmonyPlugin), typeof(HarmonyPluginPatcher)),
    ];

    /// <inheritdoc />
    public string[] ReferenceNames { get; set; } = [
        "Assembly-CSharp",
        "0Harmony",
        "Railroader-ModInterfaces",
        "Serilog",
        "UnityEngine.CoreModule"
    ];

    /// <inheritdoc />
    public string? CompileMod(ModDefinition definition)
    {
        var csFiles = fileSystem.DirectoryInfo(definition.BasePath).EnumerateFiles("*.cs", SearchOption.AllDirectories).ToArray();
        if (csFiles.Length == 0) {
            logger.Warning("No .cs files found for mod {ModId} in {BasePath}", definition.Identifier, definition.BasePath);
            return null;
        }

        var assemblyPath = Path.Combine(definition.BasePath, definition.Identifier + ".dll");
        if (fileSystem.File.Exists(assemblyPath)) {
            var newestFile = csFiles.OrderByDescending(o => o.LastWriteTime).First();
            if (fileSystem.File.GetLastWriteTime(assemblyPath) > newestFile.LastWriteTime) {
                logger.Information("Using existing mod {ModId} DLL at {Path}", definition.Identifier, assemblyPath);
                return assemblyPath;
            }

            int retries = 3;
            while (retries > 0) {
                try {
                    fileSystem.File.Delete(assemblyPath);
                    break;
                }
                catch (IOException ex) {
                    if (--retries == 0) {
                        logger.Error(ex, "Failed to delete existing assembly {AssemblyPath} for mod {ModId} after retries", assemblyPath, definition.Identifier);
                        return null;
                    }
                    logger.Warning(ex, "Retrying delete of {AssemblyPath} for mod {ModId}", assemblyPath, definition.Identifier);
                    Thread.Sleep(100);
                }
            }
        }

        logger.Information("Compiling mod {ModId} ...", definition.Identifier);

        var sources = csFiles.Select(o => o.FullName).ToArray();
        var references = ReferenceNames.Select(o => Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "Railroader_Data", "Managed", o + ".dll")).ToArray();

        logger.Debug("outputDllPath: {OutputDllPath}, Sources: {Sources}, References: {References}", assemblyPath, string.Join(", ", sources), string.Join(", ", references));

        var args = CompilerArguments(sources, assemblyPath, references).ToArray();

        var sb = new StringBuilder();
        using (var error = new StringWriter(sb)) {
            compilerCallableEntryPoint.InvokeCompiler(args, error);
        }

        var errors = sb.ToString();
        if (!string.IsNullOrEmpty(errors)) {
            logger.Error("Compilation failed for mod {ModId} with errors:\r\n{Errors}", definition.Identifier, errors);
            return null;
        }

        logger.Information("Compilation complete for mod {ModId}", definition.Identifier);

        if (PluginPatchers.Count > 0) {
            if (!ApplyPatches(assemblyPath, definition.Identifier)) {
                logger.Error("Failed to apply patches to assembly {AssemblyPath} for mod {ModId}", assemblyPath, definition.Identifier);
                return null;
            }
        } else {
            logger.Debug("No patchers configured for mod {ModId}", definition.Identifier);
        }

        return assemblyPath;
    }

    /// <summary> Generates the command-line arguments for the Mono C# compiler. </summary>
    /// <param name="sources">The source file paths to compile.</param>
    /// <param name="assemblyPath">The output path for the compiled assembly.</param>
    /// <param name="references">The paths to reference assemblies.</param>
    /// <returns>An enumeration of compiler arguments.</returns>
    [SuppressMessage("ReSharper", "GrammarMistakeInComment")]
    [SuppressMessage("ReSharper", "CommentTypo")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static IEnumerable<string> CompilerArguments(string[] sources, string assemblyPath, string[] references) {
        foreach (var source in sources) {
            yield return source;
        }

        //   --about              About the Mono C# compiler
        //   -addmodule:M1[,Mn]   Adds the module to the generated assembly
        //   -checked[+|-]        Sets default aritmetic overflow context
        //   -clscheck[+|-]       Disables CLS Compliance verifications
        //   -codepage:ID         Sets code page to the one in ID (number, utf8, reset)
        //   -define:S1[;S2]      Defines one or more conditional symbols (short: -d)
        //   -debug[+|-], -g      Generate debugging information
        yield return "-debug-";
        //   -delaysign[+|-]      Only insert the public key into the assembly (no signing)
        //   -doc:FILE            Process documentation comments to XML file
        //   -fullpaths           Any issued error or warning uses absolute file path
        yield return "-fullpaths";
        //   -help                Lists all compiler options (short: -?)
        //   -keycontainer:NAME   The key pair container used to sign the output assembly
        //   -keyfile:FILE        The key file used to strongname the ouput assembly
        //   -langversion:TEXT    Specifies language version: ISO-1, ISO-2, 3, 4, 5, Default or Future
        //yield return "-langversion:Default";
        //   -lib:PATH1[,PATHn]   Specifies the location of referenced assemblies
        //   -main:CLASS          Specifies the class with the Main method (short: -m)
        //   -noconfig            Disables implicitly referenced assemblies
        //   -nostdlib[+|-]       Does not reference mscorlib.dll library
        //   -nowarn:W1[,Wn]      Suppress one or more compiler warnings
        //   -optimize[+|-]       Enables advanced compiler optimizations (short: -o)
        yield return "-optimize";
        //   -out:FILE            Specifies output assembly name
        yield return $"-out:{assemblyPath}";
        //   -pkg:P1[,Pn]         References packages P1..Pn
        //   -platform:ARCH       Specifies the target platform of the output assembly
        //                        ARCH can be one of: anycpu, anycpu32bitpreferred, arm,
        //                        x86, x64 or itanium. The default is anycpu.
        //yield return  "-platform:anycpu",
        //   -recurse:SPEC        Recursively compiles files according to SPEC pattern
        //   -reference:A1[,An]   Imports metadata from the specified assembly (short: -r)
        yield return $"-reference:{string.Join(",", references)}";
        //   -reference:ALIAS=A   Imports metadata using specified extern alias (short: -r)
        //   -sdk:VERSION         Specifies SDK version of referenced assemblies
        //                        VERSION can be one of: 2, 4, 4.5 (default) or a custom value
        //   -target:KIND         Specifies the format of the output assembly (short: -t)
        //                        KIND can be one of: exe, winexe, library, module
        yield return "-target:library";
        //   -unsafe[+|-]         Allows to compile code which uses unsafe keyword
        //   -warnaserror[+|-]    Treats all warnings as errors
        //   -warnaserror[+|-]:W1[,Wn] Treats one or more compiler warnings as errors
        //   -warn:0-4            Sets warning level, the default is 4 (short -w:)
        yield return "-warn:4";
    }

    /// <summary> Applies registered patchers to the compiled assembly. </summary>
    /// <param name="assemblyPath">The path to the assembly to patch. Must not be null.</param>
    /// <param name="modId">The identifier of the mod, used for logging.</param>
    /// <returns>True if patching succeeds, false otherwise.</returns>
    private bool ApplyPatches(string assemblyPath, string modId) {
        var tempFilePath = Path.ChangeExtension(assemblyPath, ".patched.dll");
        var success      = false;

        AssemblyDefinition? assemblyDefinition = null;
        try {
            var resolver = new DefaultAssemblyResolver();

            // game DLLs
            resolver.AddSearchDirectory(Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "Railroader_Data", "Managed"));

            // other mods DLLs
            var thisModDir = Path.GetDirectoryName(assemblyPath);
            var modDirs = Directory.EnumerateDirectories(Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "Mods"))
                                   .Where(o => o != thisModDir);
            foreach (var modDir in modDirs) {
                resolver.AddSearchDirectory(modDir);
            }

            var readParameters = new ReaderParameters { AssemblyResolver = resolver };
            assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, readParameters);
            if (assemblyDefinition == null) {
                logger.Error("Failed to load definition for assembly {AssemblyPath} for mod {ModId}", assemblyPath, modId);
                return false;
            }

            logger.Debug("Applying patches to assembly {AssemblyPath} for mod {ModId}", assemblyPath, modId);

            foreach (var type in assemblyDefinition.MainModule?.Types ?? Enumerable.Empty<TypeDefinition>()) {
                try {
                    var interfaces = type.Interfaces?.Select(i => i.InterfaceType?.FullName).ToList() ?? [];
                    foreach (var pair in PluginPatchers)
                    {
                        if (pair.InterfaceType == null || pair.PluginPatcherType == null) {
                            logger.Warning("Invalid patcher mapping: marker={MarkerType}, patcher={PatcherType} for mod {ModId}", pair.InterfaceType?.FullName ?? "null", pair.PluginPatcherType?.FullName ?? "null", modId);
                            continue;
                        }

                        if (interfaces.Contains(pair.InterfaceType.FullName)) {
                            var patcher = _PluginPatchers.GetOrAdd(pair.InterfaceType, _ => {
                                try {
                                    return (IPluginPatcher)Activator.CreateInstance(pair.PluginPatcherType, logger)!;
                                } catch (Exception ex) {
                                    logger.Error(ex, "Failed to create patcher {PatcherType} for mod {ModId}", pair.PluginPatcherType.FullName, modId);
                                    throw;
                                }
                            })!;
                            patcher.Patch(assemblyDefinition, type);
                        }
                    }
                } catch (Exception ex) {
                    logger.Error(ex, "Failed to patch type {TypeName} for mod {ModId}", type.FullName, modId);
                }
            }

            assemblyDefinition.Write(tempFilePath);
            logger.Debug("Wrote patched assembly to temporary file {TempPath} for mod {ModId}", tempFilePath, modId);
            success = true;
            return true;
        }
        catch (Exception ex) {
            logger.Error(ex, "Failed to apply patches to assembly {AssemblyPath} for mod {ModId}", assemblyPath, modId);
            return false;
        } finally {
            assemblyDefinition?.Dispose();
            if (success) {
                if (fileSystem.File.Exists(assemblyPath)) {
                    fileSystem.File.Delete(assemblyPath);
                }
            
                fileSystem.File.Move(tempFilePath, assemblyPath);
            }
            
            if (fileSystem.File.Exists(tempFilePath)) {
                fileSystem.File.Delete(tempFilePath);
            }
        }
    }
}