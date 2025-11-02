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

public enum CompileModResult
{
    None,
    Success,
    Error,
    Skipped
}

public delegate CompileModResult CompileModDelegate(ModDefinition definition, string[]? referenceNames = null);

[PublicAPI]
public static class CodeCompiler
{
    [ExcludeFromCodeCoverage]
    public static CompileModDelegate Factory() =>
        (definition, names) => CompileMod(Log.Logger.ForSourceContext(), CompileAssembly.Execute,
            DirectoryInfoWrapper.Create, Directory.GetCurrentDirectory, File.Exists, File.GetLastWriteTime, File.Delete,
            definition, names ?? DefaultReferenceNames);

    public static string[] DefaultReferenceNames => [
        "Assembly-CSharp",
        "0Harmony",
        "Railroader.ModManager.Interfaces",
        "Serilog",
        "UnityEngine.CoreModule"
    ];

    private sealed record CompilerContext(
        ILogger Logger,
        CompileAssemblyDelegate CompileAssembly,
        DirectoryInfoFactory DirectoryInfo,
        GetCurrentDirectory GetCurrentDirectory,
        Exists Exists,
        GetLastWriteTime GetLastWriteTime,
        Delete Delete,
        ModDefinition Definition,
        string[] ReferenceNames
    )
    {
        public string AssemblyPath => Path.Combine(Definition.BasePath, $"{Definition.Identifier}.dll");
        public string ManagedPath  => Path.Combine(GetCurrentDirectory(), "Railroader_Data", "Managed");
        public string ModsPath     => Path.Combine(GetCurrentDirectory(), "Mods");
    }

    public static CompileModResult CompileMod(
        ILogger logger,
        CompileAssemblyDelegate compileAssembly,
        DirectoryInfoFactory directoryInfo,
        GetCurrentDirectory getCurrentDirectory,
        Exists exists,
        GetLastWriteTime getLastWriteTime,
        Delete delete,
        ModDefinition definition,
        string[] referenceNames
    ) =>
        new CompilerContext(logger, compileAssembly, directoryInfo, getCurrentDirectory, exists, getLastWriteTime,
            delete, definition, referenceNames).Compile();

    private static CompileModResult Compile(this CompilerContext ctx) {
        var csFiles = ctx.DirectoryInfo(ctx.Definition.BasePath)
                         .EnumerateFiles("*.cs", SearchOption.AllDirectories)
                         .OrderByDescending(f => f.LastWriteTime)
                         .ToArray();

        if (csFiles.Length == 0) {
            return CompileModResult.None;
        }

        if (ctx.TrySkipCompilation(csFiles, out var result)) {
            return result;
        }

        ctx.Logger.Information("Compiling mod {ModId} ...", ctx.Definition.Identifier);

        var sources    = csFiles.Select(f => f.FullName).ToArray();
        var references = ctx.BuildReferences();

        if (!ctx.CompileAssembly(ctx.AssemblyPath, sources, references, out _)) {
            ctx.Logger.Error("Compilation failed for mod {ModId} ...", ctx.Definition.Identifier);
            return CompileModResult.Error;
        }

        ctx.Logger.Information("Compilation complete for mod {ModId}", ctx.Definition.Identifier);
        return CompileModResult.Success;
    }

    private static bool TrySkipCompilation(this CompilerContext ctx, IFileInfo[] csFiles, out CompileModResult result) {
        result = CompileModResult.None;

        if (!ctx.Exists(ctx.AssemblyPath)) {
            return false;
        }

        var assemblyTime     = ctx.GetLastWriteTime(ctx.AssemblyPath);
        var newestSourceTime = csFiles[0].LastWriteTime;

        if (assemblyTime >= newestSourceTime) {
            ctx.Logger.Information("Using existing mod {ModId} DLL at {Path}", ctx.Definition.Identifier,
                ctx.AssemblyPath);
            result = CompileModResult.Skipped;
            return true;
        }

        ctx.Logger.Information("Deleting mod {ModId} DLL at {Path} because it is outdated", ctx.Definition.Identifier,
            ctx.AssemblyPath);
        ctx.Delete(ctx.AssemblyPath);
        return false;
    }

    private static string[] BuildReferences(this CompilerContext ctx) {
        var references = ctx.ReferenceNames.Select(name => Path.Combine(ctx.ManagedPath, $"{name}.dll")).ToList();

        if (ctx.Definition.Requires?.Count > 0) {
            var requiredMods = ctx.Definition.Requires.Keys.ToArray();
            ctx.Logger.Information("Adding references to {Mods} ...", requiredMods);

            var modRefs = requiredMods.Select(mod => Path.Combine(ctx.ModsPath, mod, $"{mod}.dll"));
            references.AddRange(modRefs);
        }

        return references.ToArray();
    }
}
