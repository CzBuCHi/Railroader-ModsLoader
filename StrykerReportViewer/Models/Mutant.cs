using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace StrykerReportViewer.Models;

[PublicAPI]
[DebuggerDisplay("Mutant: {Status}")]
public class Mutant
{
    public string   Id           { get; set; } = null!;
    public string   MutatorName  { get; set; } = null!;
    public string   Replacement  { get; set; } = null!;
    public Location Location     { get; set; } = null!;
    public string   Status       { get; set; } = null!;
    public string   StatusReason { get; set; } = null!;
    public bool     Static       { get; set; }
    public Guid[]   CoveredBy    { get; set; } = null!;
    public Guid[]   KilledBy     { get; set; } = null!;
}
