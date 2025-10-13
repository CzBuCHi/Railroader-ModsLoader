using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Railroader.ModInjector.Wrappers;
using Railroader.ModInterfaces;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Railroader.ModInjector.Services;

[PublicAPI]
public interface ICodeCompiler
{
    string[] ReferenceNames { get; set; }

    string? CompileMod(IModDefinition definition);
}

public sealed class CodeCompiler(IFileSystem fileSystem, IMonoCompiler monoCompiler, ILogger logger) : ICodeCompiler
{
    [ExcludeFromCodeCoverage]
    public CodeCompiler() : this(new FileSystemWrapper(), new MonoCompiler(), Log.ForContext("SourceContext", "Railroader.ModInjector")!) {
    }

    public string[] ReferenceNames { get; set; } = [
        "Assembly-CSharp",
        "0Harmony",
        "Railroader-ModInterfaces",
        "Serilog"
    ];

    public string? CompileMod(IModDefinition definition) {
        var csFiles = fileSystem.DirectoryInfo(definition.DefinitionPath).EnumerateFiles("*.cs", SearchOption.AllDirectories).ToArray();
        if (csFiles.Length == 0) {
            return null;
        }

        var outputDllPath = Path.Combine(definition.DefinitionPath, definition.Id + ".dll");
        if (fileSystem.File.Exists(outputDllPath)) {
            var newestFile = csFiles.OrderByDescending(o => o.LastWriteTime).First();

            if (fileSystem.File.GetLastWriteTime(outputDllPath) > newestFile.LastWriteTime) {
                logger.Information("Using existing mod {identifier} DLL ...", definition.Id);
                return outputDllPath;
            }

            fileSystem.File.Delete(outputDllPath);
        }

        logger.Information("Compiling mod {identifier} ...", definition.Id);

        var sources    = csFiles.Select(o => o.FullName).ToArray();
        var references = ReferenceNames.Select(o => Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "Railroader_Data", "Managed", o + ".dll")).ToArray();

        logger.Debug("outputDllPath: {outputDllPath}, Sources: {sources}, references: {references}", outputDllPath, sources, references);

        var args = CompilerArguments(sources, outputDllPath, references).ToArray();

        var sb = new StringBuilder();
        using (var error = new StringWriter(sb)) {
            monoCompiler.Compile(args, error);
        }

        var errors = sb.ToString();
        if (!string.IsNullOrEmpty(errors)) {
            logger.Error("Compilation failed with error(s):\r\n{errors}", errors);
            return null;
        }

        logger.Information("Compilation complete ...");
        return outputDllPath;
    }

    [SuppressMessage("ReSharper", "GrammarMistakeInComment")]
    [SuppressMessage("ReSharper", "CommentTypo")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static IEnumerable<string> CompilerArguments(string [] sources, string outputDllPath, string[] references) {
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
        yield return $"-out:{outputDllPath}";
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
        //   -helpinternal        Shows internal and advanced compiler options
    }
}
