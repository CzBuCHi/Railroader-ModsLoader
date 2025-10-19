using System.Diagnostics;
using JetBrains.Annotations;

namespace StrykerReportTool.Models;

[PublicAPI]
[DebuggerDisplay("High: {High}, Low: {Low}")]
public sealed class Thresholds
{
    public int High { get; set; }
    public int Low  { get; set; }
}
