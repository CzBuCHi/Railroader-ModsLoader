using System.Diagnostics;
using JetBrains.Annotations;

namespace StrykerReportViewer.Models;

[PublicAPI]
[DebuggerDisplay("Start: {Start}, End: {End}")]
public class Location
{
    public Position Start { get; set; } = null!;
    public Position End   { get; set; } = null!;
}
