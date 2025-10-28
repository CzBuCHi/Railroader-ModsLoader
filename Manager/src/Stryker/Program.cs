using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Stryker;

internal sealed record ProjectInfo(string Name, string Path);

internal sealed record TestTestedPairs(string Identifier, string Project, string TestProject);

public static class Program
{
    public static void Main(string[] args) {
        try {
            Execute(args);
        } catch (Exception exc) {
            Console.WriteLine(exc);

            Console.WriteLine();
            Console.WriteLine("Press any key to exit ...");
            Console.ReadKey();
        }
    }

    private static readonly Regex _ProjectRegex = new("^Project\\(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\"\\) = \"(?<name>[^\"]+)\", \"(?<path>[^\"]+)\", \"[^\"]+\"$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static void Execute(string[] args) {
        var strykerConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "stryker-config.json");

        var strykerConfigJson = File.Exists(strykerConfigPath)
            ? File.ReadAllText(strykerConfigPath)
            : throw new FileNotFoundException("Cannot find stryker-config.json in current directory");

        var strykerConfig = JsonDocument.Parse(strykerConfigJson);

        var solution = strykerConfig.RootElement.GetProperty("stryker-config").GetProperty("solution").GetString()
                       ?? throw new Exception("Unable to resolve solution from stryker-config.json");

        var projects = File.ReadLines(solution)
                           .Select(o => _ProjectRegex.Match(o))
                           .Where(o => o.Success)
                           .Select(o => new ProjectInfo(o.Groups["name"].Value, o.Groups["path"].Value))
                           .ToArray();

        var solutionFullPath   = Path.GetFullPath(solution);
        var projectPairs       = EnumerateTestTestedPairs(projects).ToArray();
        var testReports        = projectPairs.Select(o => GetTestReport(solutionFullPath, o, args)).ToArray();
        var solutionTestReport = MergeReports(solutionFullPath, testReports);
        var reportPath         = Save(solutionTestReport);

        Process.Start(new ProcessStartInfo {
            FileName = reportPath,
            UseShellExecute = true,
            Verb = "open"
        });
    }

    private static string Save(JsonDocument report) {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        report.WriteTo(writer);
        writer.Flush();
        stream.Seek(0, SeekOrigin.Begin);
        var bytes = stream.ToArray();

        WriteJsonReport(bytes);
        return WriteHtmlReport(bytes);
    }

    private static JsonDocument MergeReports(string solutionFullPath, JsonDocument?[] reports) {
        if (reports.Length == 0) {
            throw new InvalidOperationException("No reports to merge.");
        }

        if (reports.Any(o => o == null)) {
            throw new InvalidOperationException("At least one report generation failed.");
        }

        var reportDocuments = reports.Cast<JsonDocument>().ToArray();

        // Use JsonSerializer to create a new JsonDocument by merging
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();

        writer.WritePropertyName("files");
        writer.WriteStartObject();

        // Aggregate files from all reports
        foreach (var report in reportDocuments) {
            var files = report.RootElement.GetProperty("files");
            foreach (var file in files.EnumerateObject()) {
                writer.WritePropertyName(file.Name);
                file.Value.WriteTo(writer);
            }
        }

        writer.WriteEndObject(); // files

        writer.WriteString("projectRoot", Path.GetDirectoryName(solutionFullPath));
        writer.WriteString("schemaVersion", reportDocuments[0].RootElement.GetProperty("schemaVersion").GetString());

        writer.WritePropertyName("testFiles");
        writer.WriteStartObject();

        // Aggregate testFiles from all reports
        foreach (var report in reportDocuments) {
            var files = report.RootElement.GetProperty("testFiles");
            foreach (var file in files.EnumerateObject()) {
                writer.WritePropertyName(file.Name);
                file.Value.WriteTo(writer);
            }
        }

        writer.WriteEndObject(); // testFiles

        writer.WritePropertyName("thresholds");
        reportDocuments[0].RootElement.GetProperty("thresholds").WriteTo(writer);

        writer.WriteEndObject(); // root
        writer.Flush();

        // Create merged JsonDocument
        stream.Position = 0;
        return JsonDocument.Parse(stream);
    }

    private static void WriteJsonReport(byte[] bytes) {
        using var fileStream = File.Create(Path.Combine(Directory.GetCurrentDirectory(), "StrykerOutput", "mutation-report.json"));
        fileStream.Write(bytes, 0, bytes.Length);
    }

    private static string WriteHtmlReport(byte[] bytes) {
        var       reportPath = Path.Combine(Directory.GetCurrentDirectory(), "StrykerOutput", "mutation-report.html");
        using var fileStream = File.Create(reportPath);

        using var prefixStream = GetResource("Stryker.mutation-report_prefix.txt");
        prefixStream.CopyTo(fileStream);

        fileStream.Write(bytes, 0, bytes.Length);

        using var suffixStream = GetResource("Stryker.mutation-report_suffix.txt");
        suffixStream.CopyTo(fileStream);

        return reportPath;
    }

    private static Stream GetResource(string name) =>
        typeof(Program).Assembly.GetManifestResourceStream(name)
        ?? throw new InvalidOperationException($"Cannot find manifest resource '{name}'");

    private static IEnumerable<TestTestedPairs> EnumerateTestTestedPairs(ProjectInfo[] projects) {
        var dict = projects.ToDictionary(o => o.Name);

        foreach (var project in dict.Values.Where(o => o.Name.EndsWith(".Tests"))) {
            if (dict.TryGetValue(project.Name[..^6], out var testedProject)) {
                yield return new TestTestedPairs(testedProject.Name, testedProject.Path, project.Path);
            }
        }
    }

    private static JsonDocument? GetTestReport(string solutionFullPath, TestTestedPairs pair, string[] args) {
        if (IsReportOutdated(pair)) {
            if (!RunStryker(solutionFullPath, pair, args)) {
                return null;
            }
        }

        var path = GetReportPath(pair.Identifier);
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string GetReportPath(string identifier) =>
        Path.Combine(Directory.GetCurrentDirectory(), "StrykerOutput", identifier, "reports", "mutation-report.json");

    private static bool RunStryker(string solutionFullPath, TestTestedPairs pair, string[] args) {
        var solutionRoot = Path.GetDirectoryName(solutionFullPath)!;
        List<string> processArgs = [
            "stryker",
            "-p", Path.GetFileName(pair.Project),
            "-s", Path.GetFullPath(solutionFullPath),
            "-O", Path.Combine(Path.GetFullPath(solutionRoot), "StrykerOutput", pair.Identifier)
        ];

        var configPath = Path.Combine(solutionRoot, "stryker-config.json");
        if (File.Exists(configPath)) {
            processArgs.AddRange(["-f", configPath]);
        }

        processArgs.AddRange(args);

        var process = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = "dotnet",
                Arguments = string.Join(" ", processArgs),
                WorkingDirectory = Path.GetDirectoryName(pair.TestProject)
            }
        };

        Console.WriteLine($"cd {Path.GetFullPath(Path.GetDirectoryName(pair.TestProject)!)}");
        Console.WriteLine($"dotnet {string.Join(" ", processArgs)}");

        process.Start();

        process.WaitForExit();

        if (process.ExitCode != 0) {
            Console.WriteLine($"Stryker failed for {pair.Project}");
            return false;
        }

        var outputPath = Path.Combine(solutionRoot, "StrykerOutput", pair.Identifier);
        Console.WriteLine($"Stryker completed for {pair.Project}. Output: {outputPath}");
        return true;
    }

    private static bool IsReportOutdated(TestTestedPairs pair) {
        var reportDate = GetReportDate(pair.Identifier);
        string[] excludes = [
            Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar,
            Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar
        ];
        var newestProjectFile     = GetNewestFileInDirectory(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), Path.GetDirectoryName(pair.Project)!)), excludes);
        var newestTestProjectFile = GetNewestFileInDirectory(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), Path.GetDirectoryName(pair.TestProject)!)), excludes);
        return reportDate == null ||
               (newestProjectFile != null && reportDate < newestProjectFile.LastWriteTime) ||
               (newestTestProjectFile != null && reportDate < newestTestProjectFile.LastWriteTime);
    }

    private static DateTime? GetReportDate(string identifier) {
        var file = new FileInfo(GetReportPath(identifier));
        return file.Exists ? file.LastWriteTime : null;
    }

    private static FileInfo? GetNewestFileInDirectory(DirectoryInfo directory, string[] excludes) =>
        directory.Exists
            ? directory.EnumerateFiles("*.*", SearchOption.AllDirectories)
                       .Where(o => !excludes.Any(p => o.FullName.Contains(p, StringComparison.OrdinalIgnoreCase)))
                       .OrderByDescending(o => o.LastWriteTime)
                       .FirstOrDefault()
            : null;
}
