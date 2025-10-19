using JetBrains.Annotations;

namespace StrykerReportViewer.Models;

[PublicAPI]
public class Report
{
    public Files      Files         { get; set; } = null!;
    public string     ProjectRoot   { get; set; } = null!;
    public string     SchemaVersion { get; set; } = null!;
    public TestFiles  TestFiles     { get; set; } = null!;
    public Thresholds Thresholds    { get; set; } = null!;
}
