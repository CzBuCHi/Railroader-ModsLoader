using System.Linq;
using Railroader.ModManager.Services.Wrappers.FileSystem;
using ILogger = Serilog.ILogger;
using Path = System.IO.Path;
using SearchOption = System.IO.SearchOption;

namespace Railroader.ModManager.Services;

/// <summary>
/// Defines a contract for compiling mod source code and applying patches to the resulting assembly.
/// </summary>
internal interface ICodeCompiler
{
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
internal sealed class CodeCompiler(IFileSystem fileSystem, CompileAssemblyDelegate compileAssembly, ILogger logger)
    : ICodeCompiler
{
    /// <inheritdoc />
    public string[] ReferenceNames { get; init; } = [
        "Assembly-CSharp",
        "0Harmony",
        "Railroader.ModManager.Interfaces",
        "Serilog",
        "UnityEngine.CoreModule"
    ];

    /// <inheritdoc />
    public string? CompileMod(ModDefinition definition) {
        var csFiles = fileSystem.DirectoryInfo(definition.BasePath)
                                .EnumerateFiles("*.cs", SearchOption.AllDirectories)
                                .OrderByDescending(o => o.LastWriteTime)
                                .ToArray();
        if (csFiles.Length == 0) {
            return null;
        }

        var assemblyPath = Path.Combine(definition.BasePath, definition.Identifier + ".dll");
        if (fileSystem.File.Exists(assemblyPath)) {
            var newestFile = csFiles[0];
            if (fileSystem.File.GetLastWriteTime(assemblyPath) >= newestFile.LastWriteTime) {
                logger.Information("Using existing mod {ModId} DLL at {Path}", definition.Identifier, assemblyPath);
                return assemblyPath;
            }

            logger.Information("Deleting mod {ModId} DLL at {Path} because it is outdated", definition.Identifier, assemblyPath);
            fileSystem.File.Delete(assemblyPath);
        }

        logger.Information("Compiling mod {ModId} ...", definition.Identifier);

        var sources = csFiles.Select(o => o.FullName).ToArray();

        var managedPath = Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "Railroader_Data", "Managed");
        var references  = ReferenceNames.Select(o => Path.Combine(managedPath, o + ".dll")).ToList();

        if (definition.Requires?.Count > 0) {
            logger.Information("Adding references to {Mods} ...", definition.Requires.Keys);
            var modsPath      = Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "Mods");
            var modReferences = definition.Requires.Keys.Select(o => Path.Combine(modsPath, o, o + ".dll"));
            references.AddRange(modReferences);
        }

        if (!compileAssembly(assemblyPath, sources, references.ToArray(), out _)) {
            logger.Error("Compilation failed for mod {ModId} ...", definition.Identifier);
            return null;
        }

        logger.Information("Compilation complete for mod {ModId}", definition.Identifier);
        return assemblyPath;
    }
}
