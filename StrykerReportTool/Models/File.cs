using JetBrains.Annotations;

namespace StrykerReportTool.Models;

[PublicAPI]
public class File
{
    public string       Language { get; set; } = null!;
    public string       Source   { get; set; } = null!;
    public List<Mutant> Mutants  { get; set; } = null!;
}
