using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

[assembly: ExcludeFromCodeCoverage]

// removes all mutants with Killed / Ignored / CompileError / Timeout status
// then removes all files with no mutants
// and finally removes all test files not testing remaining files

try {
    var strykerOutput = args.Length == 1
        ? Path.GetFullPath(args[0])
        : Path.Combine(Directory.GetCurrentDirectory(), "StrykerOutput", "mutation-report.json");

    if (!System.IO.File.Exists(strykerOutput)) {
        Console.WriteLine($"Cannot find report file at '{strykerOutput}'");
        return;
    }

    // 1. Load Stryker JSON report
    Console.WriteLine("Load Stryker JSON report ...");
    var reportJson = System.IO.File.ReadAllText(strykerOutput);

    var options = new JsonSerializerOptions {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    var report = JsonSerializer.Deserialize<Report>(reportJson, options);
    if (report == null) {
        Console.WriteLine("Failed to deserialize mutation report!");
        return;
    }

    // 2. Remove mutants that were killed or ignored
    Console.WriteLine("Remove mutants that were killed or ignored ...");
    string[] statuses = [
        "Killed",
        "Ignored",
        "CompileError",
        "Timeout"
    ];

    foreach (var pair in report.Files) {
        var mutants = pair.Value.Mutants
            .Where(m => !statuses.Contains(m.Status, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        report.Files[pair.Key] = pair.Value with { Mutants = mutants };
    }

    // 3. Remove files with no mutants
    Console.WriteLine("Remove files with no mutants ...");
    var filesToRemove = report.Files
        .Where(o => o.Value.Mutants.Length == 0)
        .Select(o => o.Key)
        .ToArray();

    foreach (var fileName in filesToRemove) {
        report.Files.Remove(fileName);
    }

    // 4. Remove tests not used by any remaining file
    Console.WriteLine("Remove tests not used by any remaining file ...");
    var usedTestIds = report.Files.Values
        .SelectMany(f => f.Mutants)
        .SelectMany(m => m.CoveredBy.Concat(m.KilledBy))
        .Distinct()
        .ToHashSet();

    foreach (var pair in report.TestFiles) {
        var tests = pair.Value.Tests
            .Where(t => usedTestIds.Contains(t.Id))
            .ToArray();

        report.TestFiles[pair.Key] = pair.Value with { Tests = tests };
    }

    // 5. Remove test files with no tests
    Console.WriteLine("Remove test files with no tests ...");
    var testFilesToRemove = report.TestFiles
        .Where(o => o.Value.Tests.Length == 0)
        .Select(o => o.Key);

    foreach (var testFileName in testFilesToRemove) {
        report.TestFiles.Remove(testFileName);
    }

    // 6. Save the modified report
    Console.WriteLine("Save the modified report ...");
    var outputPath = Path.ChangeExtension(strykerOutput, ".filtered.json");
    var outputJson = JsonSerializer.Serialize(report, options);
    System.IO.File.WriteAllText(outputPath, outputJson);

    var reportPath = Path.ChangeExtension(strykerOutput, ".filtered.html");
    using var fileStream = System.IO.File.Create(reportPath);
    using var prefixStream = typeof(Program).Assembly.GetManifestResourceStream("StrykerReportTool.mutation-report_prefix.txt")!;
    prefixStream.CopyTo(fileStream);

    fileStream.Write(Encoding.UTF8.GetBytes(outputJson));

    using var suffixStream = typeof(Program).Assembly.GetManifestResourceStream("StrykerReportTool.mutation-report_suffix.txt")!;
    suffixStream.CopyTo(fileStream);

    Console.WriteLine("DONE");
} catch (Exception exc) {
    Console.WriteLine("ERROR:" + exc);
}

#if DEBUG
Console.WriteLine("Press any key to exit ...");
Console.ReadKey();
#endif

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedPositionalProperty.Global
#pragma warning disable CA1050

public sealed record Report(
    Dictionary<string, File> Files,
    string ProjectRoot,
    string SchemaVersion,
    Dictionary<string, TestFile> TestFiles,
    Thresholds Thresholds);

public sealed record File(string Language, string Source, Mutant[] Mutants);

[DebuggerDisplay("{Status,nq} {MutatorName,nq} at {Location}")]
public sealed record Mutant(
    string Id,
    string MutatorName,
    string Replacement,
    Location Location,
    string Status,
    string StatusReason,
    bool Static,
    Guid[] CoveredBy,
    Guid[] KilledBy);

public sealed record TestFile(string Language, string Source, Test[] Tests);

public sealed record Test(Guid Id, string Name, Location Location);

[DebuggerDisplay("{Start} - {End}")]
public sealed record Location(Position Start, Position End);

[DebuggerDisplay("{Line}:{Column}")]
public sealed record Position(int Line, int Column);

public sealed record Thresholds(int High, int Low);
