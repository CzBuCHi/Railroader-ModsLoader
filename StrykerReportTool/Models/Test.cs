using System.Diagnostics;
using JetBrains.Annotations;

namespace StrykerReportTool.Models;

[PublicAPI]
[DebuggerDisplay("{Name,nq}")]
public class Test
{
    public Guid     Id       { get; set; }
    public string   Name     { get; set; } = null!;
    public Location Location { get; set; } = null!;
}
