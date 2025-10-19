using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace StrykerReportViewer.Models;

[PublicAPI]
[DebuggerDisplay("{Name,nq}")]
public class Test
{
    public Guid     Id       { get; set; }
    public string   Name     { get; set; } = null!;
    public Location Location { get; set; } = null!;
}
