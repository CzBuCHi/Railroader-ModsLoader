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

/// <summary>
///     Represents the possible outcomes of a mod compilation attempt.
/// </summary>
public enum CompileModResult
{
    /// <summary>
    ///     No source files were found – nothing to compile.
    /// </summary>
    None,

    /// <summary>
    ///     Compilation succeeded and a new DLL was produced.
    /// </summary>
    Success,

    /// <summary>
    ///     Compilation failed due to errors in the source code.
    /// </summary>
    Error,

    /// <summary>
    ///     An up-to-date DLL already exists – compilation was skipped.
    /// </summary>
    Skipped
}

/// <summary>
///     Delegate used to compile a single mod. Allows full dependency-injection for testing
///     without touching the real file system.
/// </summary>
/// <param name="definition">The mod definition containing path and metadata.</param>
/// <param name="referenceNames">
///     Optional array of reference assembly names. If <c>null</c>, <see cref="CodeCompiler.DefaultReferenceNames" />
///     are used.
/// </param>
/// <returns>The result of the compilation attempt.</returns>
public delegate CompileModResult CompileModAction(ModDefinition definition, string[]? referenceNames = null);

/// <summary>
///     Provides facilities to compile C# source files of a mod into a managed assembly.
///     All file-system operations are injected via delegates so the compiler can be fully
///     unit-tested in isolation.
/// </summary>
[PublicAPI]
public static class CodeCompiler
{
    private const string DllExtension = ".dll";

    /// <summary>
    ///     Default set of reference assemblies required by every mod.
    /// </summary>
    public static string[] DefaultReferenceNames => [
        "Assembly-CSharp",
        "0Harmony",
        "Railroader.ModManager.Interfaces",
        "Serilog",
        "UnityEngine.CoreModule"
    ];

    /// <summary>
    ///     Creates a <see cref="CompileModAction" /> that is pre-configured with production
    ///     dependencies (real logger, real file-system wrappers, etc.).
    /// </summary>
    /// <returns>A fully-wired delegate ready for use in the application.</returns>
    [ExcludeFromCodeCoverage]
    public static CompileModAction Create() =>
        (definition, names) =>
            CompileMod(
                Log.Logger.ForSourceContext(),
                AssemblyCompiler.Compile,
                DirectoryInfoWrapper.Create,
                Directory.GetCurrentDirectory,
                File.Exists,
                File.GetLastWriteTime,
                File.Delete,
                definition,
                names ?? DefaultReferenceNames
            );

    /// <summary>
    ///     Compiles the C# sources of a mod into a DLL, respecting existing assemblies
    ///     when they are already up-to-date.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    /// <param name="compileAssembly">
    ///     Delegate that performs the actual Roslyn compilation.
    /// </param>
    /// <param name="directoryInfo">
    ///     Factory to obtain a <see cref="DirectoryInfoWrapper" /> for a path.
    /// </param>
    /// <param name="getCurrentDirectory">Delegate returning the process current directory.</param>
    /// <param name="exists">Delegate that checks file existence.</param>
    /// <param name="getLastWriteTime">Delegate that retrieves a file's last write time.</param>
    /// <param name="delete">Delegate that deletes a file.</param>
    /// <param name="definition">Mod definition containing base path and identifier.</param>
    /// <param name="referenceNames">Names of assemblies to reference during compilation.</param>
    /// <returns>
    ///     <see cref="CompileModResult.Success" /> on successful compilation,
    ///     <see cref="CompileModResult.Skipped" /> when an up-to-date DLL exists,
    ///     <see cref="CompileModResult.Error" /> on compilation failure, or
    ///     <see cref="CompileModResult.None" /> when no source files are present.
    /// </returns>
    public static CompileModResult CompileMod(
        ILogger logger,
        AssemblyCompilerDelegate compileAssembly,
        DirectoryInfoFactory directoryInfo,
        GetCurrentDirectory getCurrentDirectory,
        FileExists exists,
        GetLastWriteTime getLastWriteTime,
        Delete delete,
        ModDefinition definition,
        string[] referenceNames
    ) {
        var csFiles = directoryInfo(definition.BasePath)
                      .EnumerateFiles("*.cs", SearchOption.AllDirectories)
                      .ToArray();
        if (csFiles.Length == 0) {
            return CompileModResult.None;
        }

        var assemblyPath = Path.Combine(definition.BasePath, definition.Identifier + DllExtension);
        if (exists(assemblyPath)) {
            var newestFileTime = csFiles.Max(f => f.LastWriteTime);
            if (getLastWriteTime(assemblyPath) >= newestFileTime) {
                logger.Information("Using existing mod {ModId} DLL at {Path}", definition.Identifier, assemblyPath);
                return CompileModResult.Skipped;
            }

            logger.Information("Deleting mod {ModId} DLL at {Path} because it is outdated", definition.Identifier, assemblyPath);
            delete(assemblyPath);
        }

        logger.Information("Compiling mod {ModId} ...", definition.Identifier);

        var sources = csFiles.Select(o => o.FullName).ToArray();

        var managedPath = Path.Combine(getCurrentDirectory(), "Railroader_Data", "Managed");
        var references  = referenceNames.Select(o => Path.Combine(managedPath, o + DllExtension)).ToArray();

        if (definition.Requires?.Count > 0) {
            logger.Information("Adding references to {Mods} ...", definition.Requires.Keys);
            var modsPath      = Path.Combine(getCurrentDirectory(), "Mods");
            var modReferences = definition.Requires.Keys.Select(o => Path.Combine(modsPath, o, o + DllExtension));
            references = references.Concat(modReferences).ToArray();
        }

        if (!compileAssembly(assemblyPath, sources, references.ToArray())) {
            logger.Error("Compilation failed for mod {ModId} ...", definition.Identifier);
            return CompileModResult.Error;
        }

        logger.Information("Compilation complete for mod {ModId}", definition.Identifier);
        return CompileModResult.Success;
    }
}