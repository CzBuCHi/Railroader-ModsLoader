using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.CSharp;
using Railroader.ModInterfaces;
using ILogger = Serilog.ILogger;

namespace Railroader.ModInjector;

public interface ICodeCompiler
{
    string? CompileMod(IModDefinition definition);
}

public class CodeCompiler : ICodeCompiler
{
    private readonly ILogger _Logger = ModLogger.ForContext(typeof(CodeCompiler));

    public string? CompileMod(IModDefinition definition) {
        var csFiles = new DirectoryInfo(definition.DefinitionPath).GetFiles("*.cs", SearchOption.AllDirectories);
        if (csFiles.Length == 0) {
            return null;
        }

        var outputDllPath = Path.Combine(definition.DefinitionPath, definition.Id + ".dll");
        if (File.Exists(outputDllPath)) {
            var newestFile = csFiles.OrderByDescending(o => o.LastWriteTime).First();

            if (File.GetLastWriteTime(outputDllPath) > newestFile.LastWriteTime) {
                return outputDllPath;
            }

            File.Delete(outputDllPath);
        }

        _Logger.Information("Compiling mod {identifier} ...", definition.Id);

        string[] references = [
            "Assembly-CSharp",
            "0Harmony",
            "Railroader-ModInterfaces",
            "Serilog"
        ];

        var success = Compile(outputDllPath, csFiles.Select(o => o.FullName), references.Select(o => Path.Combine(Environment.CurrentDirectory, "Railroader_Data", "Managed", o + ".dll")));
        return success ? outputDllPath : null;
    }

    private bool Compile(string outputDllPath, IEnumerable<string> sources, IEnumerable<string> references) {
        var args = sources.Concat([
            "-target:library",
            "-platform:anycpu",
            $"-out:{outputDllPath}",
            "-optimize",
            "-fullpaths",
            "-warn:4",
            $"-reference:{string.Join(",", references)}"
        ]).ToArray();

        var sb = new StringBuilder();
        using (TextWriter error = new StringWriter(sb)) {
            CompilerCallableEntryPoint.InvokeCompiler(args, error);
        }

        var err = sb.ToString();
        if (!string.IsNullOrEmpty(err)) {
            _Logger.Information("Compilation failed with error(s):\r\n" + err);
            return false;
        }

        _Logger.Information("Compilation complete ...");
        return true;
    }
}
