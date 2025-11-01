using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Railroader.ModManager.Delegates.System.IO;
using Railroader.ModManager.Delegates.System.IO.Directory;
using Railroader.ModManager.Delegates.System.IO.File;
using Railroader.ModManager.Extensions;
using Serilog;
using ILogger = Serilog.ILogger;
using Path = System.IO.Path;
using SearchOption = System.IO.SearchOption;

namespace Railroader.ModManager.Features;

public delegate string? CompileModDelegate(ModDefinition definition, string[]? referenceNames = null);

[PublicAPI]
public static class CodeCompiler
{
    [ExcludeFromCodeCoverage]
    public static CompileModDelegate Factory() =>
        (definition, names) => CompileMod(Log.Logger.ForSourceContext(),
            CompileAssembly.Execute,
            DirectoryInfoWrapper.Create,
            Directory.GetCurrentDirectory,
            File.Exists,
            File.GetLastWriteTime,
            File.Delete,
            definition,
            names ?? DefaultReferenceNames
        );

    public static string[] DefaultReferenceNames => [
        "Assembly-CSharp",
        "0Harmony",
        "Railroader.ModManager.Interfaces",
        "Serilog",
        "UnityEngine.CoreModule"
    ];

    public static string? CompileMod(
        ILogger logger,
        CompileAssemblyDelegate compileAssembly,
        DirectoryInfoFactory directoryInfo,
        GetCurrentDirectory getCurrentDirectory,
        Exists exists,
        GetLastWriteTime getLastWriteTime,
        Delete delete,
        ModDefinition definition, 
        string[] referenceNames
        ) {

        var csFiles = directoryInfo(definition.BasePath)
                      .EnumerateFiles("*.cs", SearchOption.AllDirectories)
                      .OrderByDescending(o => o.LastWriteTime)
                      .ToArray();
        if (csFiles.Length == 0) {
            return null;
        }

        var assemblyPath = Path.Combine(definition.BasePath, definition.Identifier + ".dll");
        if (exists(assemblyPath)) {
            var newestFile = csFiles[0];
            if (getLastWriteTime(assemblyPath) >= newestFile.LastWriteTime) {
                logger.Information("Using existing mod {ModId} DLL at {Path}", definition.Identifier, assemblyPath);
                return assemblyPath;
            }

            logger.Information("Deleting mod {ModId} DLL at {Path} because it is outdated", definition.Identifier, assemblyPath);
            delete(assemblyPath);
        }

        logger.Information("Compiling mod {ModId} ...", definition.Identifier);

        var sources = csFiles.Select(o => o.FullName).ToArray();

        var managedPath = Path.Combine(getCurrentDirectory(), "Railroader_Data", "Managed");
        var references  = referenceNames.Select(o => Path.Combine(managedPath, o + ".dll")).ToList();

        if (definition.Requires?.Count > 0) {
            logger.Information("Adding references to {Mods} ...", definition.Requires.Keys);
            var modsPath      = Path.Combine(getCurrentDirectory(), "Mods");
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
