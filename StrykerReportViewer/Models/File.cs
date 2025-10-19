using JetBrains.Annotations;

namespace StrykerReportViewer.Models;

[PublicAPI]
public class File
{
    public string   Language { get; set; } = null!;
    public string   Source   { get; set; } = null!;
    public Mutant[] Mutants  { get; set; } = null!;
}
