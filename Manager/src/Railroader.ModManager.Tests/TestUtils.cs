using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.CSharp;
using NSubstitute;
using Railroader.ModManager.Interfaces;
using Railroader.ModManager.Services;
using Serilog;

namespace Railroader.ModManager.Tests;

[ExcludeFromCodeCoverage]
public static class TestUtils
{
    private const string GameDir = @"c:\Program Files (x86)\Steam\steamapps\common\Railroader\";

    public static (Mono.Cecil.AssemblyDefinition AssemblyDefinition, string OutputPath) BuildAssemblyDefinition(string source, string? suffix = null, [CallerFilePath] string callerFilePath = null!, [CallerMemberName] string callerMemberName = null!) {
        var prefix    = typeof(TestUtils).Namespace!;
        var index    = callerFilePath.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length;
        var rootPath = callerFilePath.Substring(0, index);

        var outputPath = Path.Combine(rootPath, "obj", "Temp", Path.GetFileNameWithoutExtension(callerFilePath), callerMemberName + suffix);

        var logger = Substitute.For<ILogger>();

        var compiler = CompileAssemblyCore.CompileAssembly(CompilerCallableEntryPoint.InvokeCompiler, logger);
        

        if (Directory.Exists(outputPath)) {
            foreach (var file in Directory.EnumerateFiles(outputPath, "*.*")) {
                File.Delete(file);
            }
        }

        Directory.CreateDirectory(outputPath);

        var sourcePath = Path.Combine(outputPath, "source.cs");
        var assemblyPath = Path.Combine(outputPath, "output.dll");

        File.WriteAllText(sourcePath, source);


        var sources = new[] { sourcePath };
        var references = new[] {
                             "Assembly-CSharp",
                             "0Harmony",
                             typeof(IPlugin).Assembly.GetName().Name,
                             "Serilog",
                             "UnityEngine.CoreModule"
                         }
                         .Select(o => Path.Combine(GameDir, "Railroader_Data", "Managed", o + ".dll"))
                         .ToList();

        references.Add(typeof(DateTime).Assembly.Location);
        references.Add(typeof(TestUtils).Assembly.Location);

        var result = compiler(assemblyPath, sources, references, out var messages);
        if (result == false) {
            throw new InvalidOperationException("Failed to compile source:\r\n" + messages);
        }

        return (Mono.Cecil.AssemblyDefinition.ReadAssembly(assemblyPath), outputPath);
    }

    public static Assembly BuildAssembly(string source, string []? references = null) {
        var settings = new CompilerSettings {
            Target = Target.Library,
            Optimize = true,
            Platform = Platform.AnyCPU,
            ShowFullPaths = true
        };
        settings.ReferencesLookupPaths.Add(Directory.GetCurrentDirectory());
        settings.AssemblyReferences.AddRange([
            "Assembly-CSharp",
            "0Harmony",
            typeof(IMod).Assembly.GetName().Name,
            "Serilog",
            "UnityEngine.CoreModule"
        ]);

        if (references != null) {
            settings.AssemblyReferences.AddRange(references);
        }


        var printer = new SimpleReportPrinter();
        var context = new CompilerContext(settings, printer);
        var eval    = new Evaluator(context);

        eval.Compile(source + " interface __AssemblyMarker { } ");
        if (context.Report.Errors > 0) {
            throw new Exception($"Compilation error: {printer.Messages}");
        }

        return (Assembly)eval.Evaluate(" typeof(__AssemblyMarker).Assembly ")!;
    }

    private sealed class SimpleReportPrinter : ReportPrinter
    {
        private readonly List<string> _Messages = new();

        public string Messages => string.Join("\r\n", _Messages);

        public override void Print(AbstractMessage msg, bool showFullPath) {
            base.Print(msg, showFullPath);

            var sb = new StringBuilder();
            using (var output = new StringWriter(sb)) {
                Print(msg, output, showFullPath);
            }

            _Messages.Add(sb.ToString());
        }
    }

    public static void Write(Mono.Cecil.AssemblyDefinition assemblyDefinition, string outputPath, string name) {
        if (Debugger.IsAttached) {
            assemblyDefinition.Name.Name = name;
            assemblyDefinition.Write(Path.Combine(outputPath, name + ".dll"));
        }
    }
}
