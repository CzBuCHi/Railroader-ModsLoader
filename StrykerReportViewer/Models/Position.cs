using System.Diagnostics;
using JetBrains.Annotations;

namespace StrykerReportViewer.Models;

[PublicAPI]
[DebuggerDisplay("{Line}:{Column}")]
public class Position
{
    public int Line   { get; set; }
    public int Column { get; set; }
}
