using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using Microsoft.CSharp;
using Railroader.ModInterfaces;
using Serilog;
using ILogger = Serilog.ILogger;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Mono.CSharp;
using Serilog.Events;
using System.Linq.Expressions;
using Expression = System.Linq.Expressions.Expression;

namespace Railroader.ModInjector;

public interface ICodeCompiler
{
    string? CompileMod(IModDefinition definition);
}

public class CodeCompiler : ICodeCompiler
{
    private readonly ILogger _Logger = ModLogger.ForContext(typeof(CodeCompiler));

    public string? CompileMod(IModDefinition definition) {
        
        var     csFiles = new DirectoryInfo(definition.DefinitionPath).GetFiles("*.cs", SearchOption.AllDirectories);
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

        Assembly[] references = [
            typeof(PluginBase).Assembly,
            typeof(ILogger).Assembly,
        ];

        Compile(csFiles.Select(o => File.ReadAllText(o.FullName)), references);
        if (File.Exists("eval-0.dll")) {
            File.Move("eval-0.dll", outputDllPath);
        }

        if (!File.Exists(outputDllPath)) {
            _Logger.Information("Mod compilation failed.");
            return null;
        }

        return outputDllPath;
    }

    static CodeCompiler() {
        var countField = typeof(Evaluator).GetField("count", BindingFlags.NonPublic | BindingFlags.Static)!;

        var lambda = Expression.Lambda<Action>(
            Expression.Assign(Expression.Field(null!, countField), Expression.Constant(0))
        );

        _ResetEvaluator = lambda.Compile();
    }

    private static readonly Action _ResetEvaluator;

    private void Compile(IEnumerable<string> sources, IEnumerable<Assembly> references) {
        var save = Environment.GetEnvironmentVariable("SAVE");
        try {
            Environment.SetEnvironmentVariable("SAVE", "SAVE");
            _ResetEvaluator();

            var settings = new CompilerSettings {
                Platform = Platform.AnyCPU,
                Optimize = false,
                GenerateDebugInfo = true,
                ShowFullPaths = true,
            };
            var printer   = new SerilogReportPrinter(_Logger);
            var context   = new CompilerContext(settings, printer);
            var evaluator = new Evaluator(context);

            foreach (var reference in references) {
                evaluator.ReferenceAssembly(reference);
            }
            
            foreach (var source in sources) {
                evaluator.Compile(source);
            }

        } finally {
            Environment.SetEnvironmentVariable("SAVE", save!);
        }
    }

    private class SerilogReportPrinter(ILogger logger) : ReportPrinter
    {
        public override void Print(AbstractMessage msg, bool showFullPath) {
            base.Print(msg, showFullPath);

            var logEventLevel = msg.IsWarning ? LogEventLevel.Warning: LogEventLevel.Error;
            if (!msg.Location.IsNull) {
                var path = showFullPath ? msg.Location.ToStringFullName() : msg.Location.ToString();
                logger.Write(logEventLevel, "{path} CS{code:0000}: {text}", path, msg.Code, msg.Text);
            } else {
                logger.Write(logEventLevel, "CS{code:0000}: {text}", msg.Code, msg.Text);
            }
            if (msg.RelatedSymbols == null) {
                return;
            }

            foreach (var relatedSymbol in msg.RelatedSymbols) {
                logger.Debug("  relatedSymbol: {relatedSymbol})", relatedSymbol);
            }
        }
    }
}
