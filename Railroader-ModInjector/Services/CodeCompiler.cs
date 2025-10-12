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
    public CodeCompiler() : this(new FileSystemWrapper(), new MonoCompiler(), Log.ForContext<CodeCompiler>()) {
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
}
