using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Stryker;

public static class Program
{
    public static void Main() {
        try {
            var reportPath = new Solution()
                             .Projects
                             .AsTestTestedPairs()
                             .GetTestReports()
                             .MergeReports()
                             .Save();

            Process.Start(new ProcessStartInfo {
                FileName = reportPath,
                UseShellExecute = true,
                Verb = "open"
            });
        } catch (Exception exc) {
            Console.WriteLine(exc);

            Console.WriteLine();
            Console.WriteLine("Press any key to exit ...");
            Console.ReadKey();
        }
    }
}

internal sealed record SolutionTestReport(JsonDocument Report);

internal static class TestReportExtensions
{
    public static string Save(this SolutionTestReport report) {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        report.Report.WriteTo(writer);
        writer.Flush();
        stream.Seek(0, SeekOrigin.Begin);
        var bytes = stream.ToArray();

        WriteJsonReport(bytes);
        return WriteHtmlReport(bytes);
    }

    public static SolutionTestReport MergeReports(this SolutionTestReports reports) {
        if (reports.Reports.Length == 0) {
            throw new InvalidOperationException("No reports to merge.");
        }

        if (reports.Reports.Any(o => o == null)) {
            throw new InvalidOperationException("At least one report generation failed.");
        }

        var reportDocuments = reports.Reports.Cast<JsonDocument>().ToArray();

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

        writer.WriteString("projectRoot", reports.RootPath);
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
        var mergedDocument = JsonDocument.Parse(stream);

        return new SolutionTestReport(mergedDocument);
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
}

internal sealed record SolutionTestReports(string RootPath, JsonDocument?[] Reports);

internal static class TestTestedPairsExtensions
{
    public static SolutionTestReports GetTestReports(this SolutionTestTestedPairs pairs) =>
        new(pairs.RootPath, pairs.Pairs.Select(o => GetTestReport(pairs.RootPath, o)).ToArray());

    private static JsonDocument? GetTestReport(string solutionRoot, TestTestedPairs pair) {
        if (IsReportOutdated(pair)) {
            if (!RunStryker(solutionRoot, pair)) {
                return null;
            }
        }

        var path = GetReportPath(pair.Identifier);
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string GetReportPath(string identifier) =>
        Path.Combine(Directory.GetCurrentDirectory(), "StrykerOutput", identifier, "reports", "mutation-report.json");

    private static bool RunStryker(string solutionRoot, TestTestedPairs pair) {
        string[] args = [
            "stryker",
            "-f", @"..\stryker-config.json",
            "-s", @"..\Railroader-ModsLoader.sln",
            "-O", @"..\StrykerOutput\" + pair.Identifier,
            "-p", Path.GetFileName(pair.Project)
        ];

        var process = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = "dotnet",
                Arguments = string.Join(" ", args),
                WorkingDirectory = Path.GetDirectoryName(pair.TestProject)
            }
        };

        Console.WriteLine($"Running: dotnet {string.Join(" ", args)}");
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
        return reportDate == null ||
               reportDate < GetNewestFileInDirectory(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), pair.Project))) ||
               reportDate < GetNewestFileInDirectory(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), pair.TestProject)));
    }

    private static DateTime? GetReportDate(string identifier) {
        var file = new FileInfo(GetReportPath(identifier));
        return file.Exists ? file.LastWriteTime : null;
    }

    private static DateTime? GetNewestFileInDirectory(DirectoryInfo directory) =>
        directory.Exists
            ? directory.EnumerateFiles("*.*", SearchOption.AllDirectories)
                       .OrderByDescending(o => o.LastWriteTime)
                       .Select(o => o.LastWriteTime)
                       .FirstOrDefault()
            : null;
}

internal sealed record SolutionTestTestedPairs(string RootPath, TestTestedPairs[] Pairs);

internal sealed record TestTestedPairs(string Identifier, string Project, string TestProject);

internal static class ProjectInfoExtensions
{
    public static SolutionTestTestedPairs AsTestTestedPairs(this SolutionProjects solutionProjects) =>
        new(solutionProjects.RootPath, EnumerateTestTestedPairs(solutionProjects).ToArray());

    private static IEnumerable<TestTestedPairs> EnumerateTestTestedPairs(this SolutionProjects solutionProjects) {
        var dict = solutionProjects.Projects.ToDictionary(o => o.Name);

        foreach (var project in dict.Values.Where(o => o.Name.EndsWith(".Tests"))) {
            if (dict.TryGetValue(project.Name[..^6], out var testedProject)) {
                yield return new TestTestedPairs(testedProject.Name, testedProject.Path, project.Path);
            }
        }
    }
}

internal sealed record SolutionProjects(string RootPath, ProjectInfo[] Projects);

internal sealed record ProjectInfo(string Name, string Path);

internal sealed class Solution
{
    public Solution() {
        var strykerConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "stryker-config.json");

        var strykerConfigJson = File.Exists(strykerConfigPath)
            ? File.ReadAllText(strykerConfigPath)
            : throw new FileNotFoundException("Cannot find stryker-config.json in current directory");

        var strykerConfig = JsonDocument.Parse(strykerConfigJson);

        var solution = strykerConfig.RootElement.GetProperty("stryker-config").GetProperty("solution").GetString()
                       ?? throw new Exception("Unable to resolve solution from stryker-config.json");

        var projects = File.ReadLines(solution)
                           .Select(o => ProjectRegex.Match(o))
                           .Where(o => o.Success)
                           .Select(o => new ProjectInfo(o.Groups["name"].Value, o.Groups["path"].Value))
                           .ToArray();


        Projects = new SolutionProjects(Path.GetDirectoryName(solution)!, projects);
    }

    public SolutionProjects Projects { get; }

    private static readonly Regex ProjectRegex = new("^Project\\(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\"\\) = \"(?<name>[^\"]+)\", \"(?<path>[^\"]+)\", \"[^\"]+\"$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
}