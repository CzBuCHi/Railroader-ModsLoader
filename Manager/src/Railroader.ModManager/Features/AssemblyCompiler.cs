using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using Mono.CSharp;
using Railroader.ModManager.Delegates.Mono.CSharp.CompilerCallableEntryPoint;
using Railroader.ModManager.Extensions;
using Serilog;
using Serilog.Events;

namespace Railroader.ModManager.Features;

/// <summary>
///     Represents a method that compiles one or more C# source files into an assembly.
/// </summary>
/// <param name="outputPath">The path where the compiled assembly will be written.</param>
/// <param name="sources">A collection of C# source file paths to compile.</param>
/// <param name="references">A collection of reference assembly paths to include during compilation.</param>
/// <param name="resources"></param>
/// <returns>
///     <see langword="true" /> if compilation succeeds; otherwise, <see langword="false" />.
/// </returns>
public delegate bool AssemblyCompilerDelegate(
    string outputPath, ICollection<string> sources, ICollection<string> references, string[] resources
);

/// <summary>
///     Provides functionality for compiling C# source files into assemblies using the Mono C# compiler.
/// </summary>
[PublicAPI]
public static class AssemblyCompiler
{
    /// <summary>
    ///     Compiles a set of C# source files into an assembly using the default compiler invocation and logger.
    /// </summary>
    /// <param name="outputPath">The path to write the compiled assembly to.</param>
    /// <param name="sourceFiles">The collection of source file paths to compile.</param>
    /// <param name="referenceAssemblies">The collection of reference assemblies required for compilation.</param>
    /// <param name="resources"></param>
    /// <returns>
    ///     <see langword="true" /> if compilation succeeds; otherwise, <see langword="false" />.
    /// </returns>
    [ExcludeFromCodeCoverage]
    public static bool Compile(
        string outputPath, ICollection<string> sourceFiles, ICollection<string> referenceAssemblies, string[] resources
    ) =>
        Compile(CompilerCallableEntryPoint.InvokeCompiler, Log.Logger.ForSourceContext(), outputPath, sourceFiles, referenceAssemblies, resources);

    /// <summary>
    ///     Compiles C# source files into an assembly using a specified compiler invoker and logger.
    /// </summary>
    /// <param name="compilerInvoker">A delegate that invokes the Mono compiler with the provided arguments.</param>
    /// <param name="logger">The <see cref="ILogger" /> instance used to record compilation progress and results.</param>
    /// <param name="outputPath">The path to write the compiled assembly to.</param>
    /// <param name="sourceFiles">The collection of source file paths to compile.</param>
    /// <param name="referenceAssemblies">The collection of reference assemblies required for compilation.</param>
    /// <param name="resources"></param>
    /// <returns>
    ///     <see langword="true" /> if compilation succeeds; otherwise, <see langword="false" />.
    /// </returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool Compile(
        InvokeCompiler compilerInvoker, ILogger logger, string outputPath, ICollection<string> sourceFiles, ICollection<string> referenceAssemblies,
        string[] resources
    ) {
        if (sourceFiles.Count == 0) {
            logger.Error("No source files provided for assembly compilation at {outputPath}.", outputPath);
            return false;
        }

        logger.Information("Compiling assembly {outputPath} ...", outputPath);
        logger.Debug("References:\n{references}", string.Join("\n", referenceAssemblies));
        logger.Debug("Sources:\n{sources}", string.Join("\n", sourceFiles));
        logger.Debug("Resources:\n{sources}", string.Join("\n", resources));

        var args = CompilerArguments(outputPath, sourceFiles, referenceAssemblies, resources);

        bool result;
        var  sb = new StringBuilder();
        using (var error = new StringWriter(sb)) {
            result = compilerInvoker(args, error);
        }

        var messages = sb.ToString();
        if (!string.IsNullOrEmpty(messages)) {
            logger.Write(result ? LogEventLevel.Debug : LogEventLevel.Information, "Compilation messages:\r\n{messages}", messages);
        }

        if (result) {
            logger.Information("Assembly {outputPath} compiled successfully", outputPath);
            return true;
        }

        logger.Error("Compilation of assembly {outputPath} failed", outputPath);
        return false;
    }

    /// <summary>
    ///     Generates the command-line arguments for the Mono C# compiler.
    /// </summary>
    /// <param name="assemblyPath">The output path for the compiled assembly.</param>
    /// <param name="sources">The collection of source file paths to compile.</param>
    /// <param name="references">The collection of reference assembly paths.</param>
    /// <param name="resources"></param>
    /// <returns>
    ///     An array of arguments passed to the Mono compiler (<c>mcs</c>).
    /// </returns>
    private static string[] CompilerArguments(
        string assemblyPath, ICollection<string> sources, ICollection<string> references, string[] resources
    ) => [
        ..sources,
        "-debug-",
        "-fullpaths",
        "-optimize",
        $"-out:{assemblyPath}",
        $"-reference:{string.Join(",", references)}",
        "-target:library",
        "-warn:4",
        ..resources
    ];
}
