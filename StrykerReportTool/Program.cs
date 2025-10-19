using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StrykerReportTool.Models;
using File = System.IO.File;

if (args.Length == 0) {
    Console.WriteLine("USAGE: StrykerReportTool path-to-StrykerOutput [filter]" );
}

var strykerOutput =  args[0];

var filter = args.Length > 1 ? args[1] : "";

var newest = new DirectoryInfo(strykerOutput).EnumerateDirectories("*.*").OrderByDescending(o => o.LastWriteTime).FirstOrDefault();
if (newest == null) {
    return;
}

var reportJsonPath = Path.Combine(newest.FullName, "reports", "mutation-report.json");
var reportHtmlPath = Path.Combine(newest.FullName, "reports", "mutation-report.html");

var reportJson = File.ReadAllText(reportJsonPath);

var serializerSettings = new JsonSerializerSettings {
    ContractResolver = new CamelCasePropertyNamesContractResolver()
};

var report = JsonConvert.DeserializeObject<Report>(reportJson, serializerSettings)!;

if (filter != "") {
    foreach (var path in report.Files.Keys.ToArray()) {
        if (!path.EndsWith(filter)) {
            report.Files.Remove(path);
        }
    }

    var relatedRests = new HashSet<Guid>(report.Files.Values.SelectMany(o => o.Mutants).SelectMany(o => o.CoveredBy.Concat(o.KilledBy)));

    foreach (var path in report.TestFiles.Keys.ToArray()) {
        var testFile = report.TestFiles[path];
        if (!testFile.Tests.Any(o => relatedRests.Contains(o.Id))) {
            report.TestFiles.Remove(path);
        }
    }
}

foreach (var file in report.Files.Values) {
    foreach (var mutant in file.Mutants.ToArray()) {
        if (mutant.Status is "Killed" or "Ignored") {
            file.Mutants.Remove(mutant);
        }
    }
}

var clearedReport = JsonConvert.SerializeObject(report, serializerSettings);

File.WriteAllText(Path.ChangeExtension(reportJsonPath, ".clean.json"), clearedReport);

var reportHtml = File.ReadAllLines(reportHtmlPath);

var index = Array.FindLastIndex(reportHtml, o => o.TrimStart().StartsWith("app.report ="));

reportHtml[index] = "app.report = " + clearedReport + ";";

File.WriteAllLines(Path.ChangeExtension(reportHtmlPath, ".clean.html"), reportHtml);
