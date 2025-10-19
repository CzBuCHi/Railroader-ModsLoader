using JetBrains.Annotations;

namespace StrykerReportTool.Models;

[PublicAPI]
public class TestFile
{
    public string Language { get; set; } = null!;
    public string Source   { get; set; } = null!;
    public Test[] Tests    { get; set; } = null!;
}
