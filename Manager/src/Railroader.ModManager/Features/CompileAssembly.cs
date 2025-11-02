using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Mono.CSharp;
using Railroader.ModManager.Delegates.Mono.CSharp.CompilerCallableEntryPoint;
using Railroader.ModManager.Extensions;
using Serilog;

namespace Railroader.ModManager.Features;

public delegate bool CompileAssemblyDelegate(
    string outputPath,
    ICollection<string> sources,
    ICollection<string> references,
    out string messages
);

[PublicAPI]
public static class CompileAssembly
{
    private sealed record CompileContext(
        InvokeCompiler InvokeCompiler,
        ILogger Logger,
        string OutputPath,
        ICollection<string> Sources,
        ICollection<string> References
    )
    {
        public string[] Args     { get; init; } = [];
        public string   Messages { get; init; } = "";
        public bool     Success  { get; init; }
    }

    [ExcludeFromCodeCoverage]
    public static bool Execute(
        string outputPath,
        ICollection<string> sources,
        ICollection<string> references,
        out string messages
    ) =>
        Execute(CompilerCallableEntryPoint.InvokeCompiler, Log.Logger.ForSourceContext(), outputPath, sources,
            references, out messages);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool Execute(
        InvokeCompiler invokeCompiler,
        ILogger logger,
        string outputPath,
        ICollection<string> sources,
        ICollection<string> references,
        out string messages
    ) {
        var result = new CompileContext(invokeCompiler, logger, outputPath, sources, references).BuildArguments()
            .LogInputs()
            .Compile()
            .LogOutput();

        messages = result.Messages;
        return result.Success;
    }

    private static CompileContext BuildArguments(this CompileContext ctx) =>
        ctx with { Args = CompilerArguments(ctx.OutputPath, ctx.Sources, ctx.References).ToArray() };

    private static CompileContext LogInputs(this CompileContext ctx) {
        ctx.Logger.Information("Compiling assembly {outputPath} ...", ctx.OutputPath);

        foreach (var reference in ctx.References) {
            ctx.Logger.Debug("reference: {source}", reference);
        }

        foreach (var source in ctx.Sources) {
            ctx.Logger.Debug("source: {source}", source);
        }

        return ctx;
    }

    private static CompileContext Compile(this CompileContext ctx) {
        var       sb      = new StringBuilder();
        using var writer  = new StringWriter(sb);
        var       success = ctx.InvokeCompiler(ctx.Args, writer);
        return ctx with { Success = success, Messages = sb.ToString() };
    }

    private static CompileContext LogOutput(this CompileContext ctx) {
        if (!string.IsNullOrEmpty(ctx.Messages)) {
            ctx.Logger.Information("Compilation messages:\r\n{messages}", ctx.Messages);
        }

        if (ctx.Success) {
            ctx.Logger.Information("Assembly {outputPath} compiled successfully", ctx.OutputPath);
        } else {
            ctx.Logger.Error("Compilation of assembly {outputPath} failed", ctx.OutputPath);
        }

        return ctx;
    }

    [SuppressMessage("ReSharper", "GrammarMistakeInComment")]
    [SuppressMessage("ReSharper", "CommentTypo")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static IEnumerable<string> CompilerArguments(
        string assemblyPath,
        ICollection<string> sources,
        ICollection<string> references
    ) {
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
}
