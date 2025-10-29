using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Railroader.ModManager.Delegates;
using Serilog;

namespace Railroader.ModManager.Services;

internal delegate bool CompileAssemblyDelegate(string outputPath, ICollection<string> sources, ICollection<string> references, out string messages);

internal static class CompileAssemblyCore
{
    public static CompileAssemblyDelegate CompileAssembly(InvokeCompilerDelegate invokeCompiler, ILogger logger) =>
        (string outputPath, ICollection<string> sources, ICollection<string> references, out string messages) =>
            CompileAssembly(invokeCompiler, logger, outputPath, sources, references, out messages);

    private static bool CompileAssembly(InvokeCompilerDelegate invokeCompiler, ILogger logger, string outputPath, ICollection<string> sources, ICollection<string> references, out string messages) {
        var args = CompilerArguments(outputPath, sources, references).ToArray();

        logger.Information("Compiling assembly {outputPath} ...", outputPath);
        foreach (var reference in references) {
            logger.Debug("reference: {source}", reference);
        }

        foreach (var source in sources) {
            logger.Debug("source: {source}", source);
        }

        bool result;
        var  sb = new StringBuilder();
        using (var error = new StringWriter(sb)) {
            result = invokeCompiler(args, error);
        }

        messages = sb.ToString();
        if (!string.IsNullOrEmpty(messages)) {
            logger.Information("Compilation messages:\r\n{messages}", messages);
        }

        if (result) {
            logger.Information("Assembly {outputPath} compiled successfully", outputPath);
            return true;
        }

        logger.Error("Compilation of assembly {outputPath} failed", outputPath);
        return false;
    }

    /// <summary> Generates the command-line arguments for the Mono C# compiler. </summary>
    /// <param name="assemblyPath">The output path for the compiled assembly.</param>
    /// <param name="sources">The source file paths to compile.</param>
    /// <param name="references">The paths to reference assemblies.</param>
    /// <returns>An enumeration of compiler arguments.</returns>
    [SuppressMessage("ReSharper", "GrammarMistakeInComment")]
    [SuppressMessage("ReSharper", "CommentTypo")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static IEnumerable<string> CompilerArguments(string assemblyPath, ICollection<string> sources, ICollection<string> references) {
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
}
