using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Railroader.ModManager.Interfaces;
using Railroader.ModManager.Patchers;
using Railroader.ModManager.Patchers.Special;
using Railroader.ModManager.Wrappers;
using Railroader.ModManager.Wrappers.FileSystem;
using ILogger = Serilog.ILogger;
using Path = System.IO.Path;
using SearchOption = System.IO.SearchOption;

namespace Railroader.ModManager.Services;

/// <summary>
/// Defines a contract for compiling mod source code and applying patches to the resulting assembly.
/// </summary>
internal interface ICodeCompiler
{
    /// <summary> Gets or sets the mapping of plugin marker interfaces to their corresponding patcher types. </summary>
    /// <remarks>Plugins are applied in reverse order.</remarks>
    List<(Type InterfaceType, Type PluginPatcherType)> PluginPatchers { get; }

    /// <summary>
    /// Gets or sets the names of reference assemblies required for compilation.
    /// </summary>
    string[] ReferenceNames { get; }

    /// <summary>
    /// Compiles the mod's source code and applies patches to the resulting assembly.
    /// </summary>
    /// <param name="definition">The mod definition containing the base path and identifier. Must not be null.</param>
    /// <returns>The path to the compiled and patched assembly DLL, or null if compilation or patching fails.</returns>
    string? CompileMod(ModDefinition definition);
}

/// <summary> Compiles mod source code into a DLL and applies registered patches to the assembly. </summary>
internal sealed class CodeCompiler : ICodeCompiler
{
    public required IFileSystem                FileSystem                { get; init; }
    public required ILogger                    Logger                    { get; init; }
    public required IAssemblyDefinitionWrapper AssemblyDefinitionWrapper { get; init; }
    public required IAssemblyCompiler          AssemblyCompiler          { get; init; }

    private readonly ConcurrentDictionary<Type, ITypePatcher> _PluginPatchers = new();

    /// <inheritdoc />
    public List<(Type InterfaceType, Type PluginPatcherType)> PluginPatchers { get; init; } = [
        (typeof(ITopRightButtonPlugin), typeof(TopRightButtonPluginPatcher)),
        (typeof(IHarmonyPlugin), typeof(HarmonyPluginPatcher))
    ];

    /// <inheritdoc />
    public string[] ReferenceNames { get; init; } = [
        "Assembly-CSharp",
        "0Harmony",
        "Railroader-ModInterfaces",
        "Serilog",
        "UnityEngine.CoreModule"
    ];

    /// <inheritdoc />
    public string? CompileMod(ModDefinition definition) {
        var csFiles = FileSystem.DirectoryInfo(definition.BasePath)
                                .EnumerateFiles("*.cs", SearchOption.AllDirectories)
                                .OrderByDescending(o => o.LastWriteTime)
                                .ToArray();
        if (csFiles.Length == 0) {
            return null;
        }

        var assemblyPath = Path.Combine(definition.BasePath, definition.Identifier + ".dll");
        if (FileSystem.File.Exists(assemblyPath)) {
            var newestFile = csFiles[0];
            if (FileSystem.File.GetLastWriteTime(assemblyPath) >= newestFile.LastWriteTime) {
                Logger.Information("Using existing mod {ModId} DLL at {Path}", definition.Identifier, assemblyPath);
                return assemblyPath;
            }

            Logger.Information("Deleting mod {ModId} DLL at {Path} because it is outdated", definition.Identifier, assemblyPath);
            FileSystem.File.Delete(assemblyPath);
        }

        Logger.Information("Compiling mod {ModId} ...", definition.Identifier);

        var sources = csFiles.Select(o => o.FullName).ToArray();

        var managedPath = Path.Combine(FileSystem.Directory.GetCurrentDirectory(), "Railroader_Data", "Managed");
        var references  = ReferenceNames.Select(o => Path.Combine(managedPath, o + ".dll")).ToList();

        if (definition.Requires?.Count > 0) {
            Logger.Information("Adding references to {Mods} ...", definition.Requires.Keys);
            var modsPath   = Path.Combine(FileSystem.Directory.GetCurrentDirectory(), "Mods");
            var modReferences = definition.Requires.Keys.Select(o => Path.Combine(modsPath, o, o + ".dll"));
            references.AddRange(modReferences);
        }

        if (!AssemblyCompiler.CompileAssembly(assemblyPath, sources, references.ToArray())) {
            Logger.Error("Compilation failed for mod {ModId} ...", definition.Identifier);
            return null;
        }

        Logger.Information("Compilation complete for mod {ModId}", definition.Identifier);

        if (PluginPatchers.Count > 0) {
            Logger.Information("Patching mod {ModId} ...", definition.Identifier);

            if (!ApplyPatches(assemblyPath, definition.Identifier)) {
                Logger.Error("Failed to apply patches to assembly {AssemblyPath} for mod {ModId}", assemblyPath, definition.Identifier);
                return null;
            }

            Logger.Information("Patching complete for mod {ModId}", definition.Identifier);
        }

        return assemblyPath;
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
            resolver.AddSearchDirectory(Path.Combine(FileSystem.Directory.GetCurrentDirectory(), "Railroader_Data", "Managed"));

            // other mods DLLs
            var thisModDir = Path.GetDirectoryName(assemblyPath);
            var modDirs = FileSystem.Directory.EnumerateDirectories(Path.Combine(FileSystem.Directory.GetCurrentDirectory(), "Mods"))
                                    .Where(o => o != thisModDir);
            foreach (var modDir in modDirs) {
                resolver.AddSearchDirectory(modDir);
            }

            var readParameters = new ReaderParameters { AssemblyResolver = resolver };
            assemblyDefinition = AssemblyDefinitionWrapper.ReadAssembly(assemblyPath, readParameters);
            if (assemblyDefinition == null) {
                Logger.Error("Failed to load definition for assembly {AssemblyPath} for mod {ModId}", assemblyPath, modId);
                return false;
            }

            var hasPatch = false;
            var hasError = false;
            foreach (var type in assemblyDefinition.MainModule.Types) {
                try {
                    var interfaces = type.Interfaces.Select(i => i.InterfaceType?.FullName).ToList();
                    var patchers = PluginPatchers.Where(pair => interfaces.Contains(pair.InterfaceType!.FullName))
                                                 .Select(pair => _PluginPatchers.GetOrAdd(pair.InterfaceType,
                                                     _ => (ITypePatcher)Activator.CreateInstance(pair.PluginPatcherType!, Logger)!
                                                 ));
                    foreach (var patcher in patchers) {
                        patcher!.Patch(assemblyDefinition, type);
                        hasPatch = true;
                    }
                } catch (Exception ex) {
                    Logger.Error(ex, "Failed to patch type {TypeName} for mod {ModId}", type.FullName, modId);
                    hasError = true;
                }
            }

            success = hasPatch && !hasError;

            if (success) {
                AssemblyDefinitionWrapper.Write(assemblyDefinition, tempFilePath);
                Logger.Debug("Wrote patched assembly to temporary file {TempPath} for mod {ModId}", tempFilePath, modId);
            } else {
                Logger.Information("No patches to assembly {AssemblyPath} for mod {ModId} where applied", assemblyPath, modId);
            }

            return true;
        } finally {
            assemblyDefinition?.Dispose();
            if (success) {
                FileSystem.File.Delete(assemblyPath);
                FileSystem.File.Move(tempFilePath, assemblyPath);
            }
        }
    }
}
