using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Railroader.ModInjector.JsonConverters;
using Railroader.ModInterfaces;
using Serilog.Events;

namespace Railroader.ModInjector;

/// <summary> Implementation of <see cref="IModDefinition"/> for mod metadata. </summary>
[DebuggerDisplay("{Identifier,nq} [{Version}] ")]
internal sealed class ModDefinition : IModDefinition
{
    /// <inheritdoc />
    [JsonProperty("id", Required = Required.DisallowNull)]
    public string Identifier { get; set; } = null!;

    /// <inheritdoc />
    [JsonProperty("name", Required = Required.DisallowNull)]
    public string Name { get; set; } = null!;

    /// <inheritdoc />
    [JsonProperty("version", Required = Required.DisallowNull)]
    [JsonConverter(typeof(VersionJsonConverter))]
    public Version Version { get; set; } = null!;
    
    /// <inheritdoc />
    [JsonProperty("logLevel")]
    [JsonConverter(typeof(LogEventLevelJsonConverter))]
    public LogEventLevel? LogLevel { get; set; }

    /// <inheritdoc />
    [JsonProperty("requires")]
    [JsonConverter(typeof(ModReferenceJsonConverter))]
    public Dictionary<string, FluentVersion?> Requires { get; set; } = null!;

    /// <inheritdoc />
    [JsonProperty("conflictsWith")]
    [JsonConverter(typeof(ModReferenceJsonConverter))]
    public Dictionary<string, FluentVersion?> ConflictsWith { get; set; } = null!;

    /// <summary> Gets or sets the base directory path for the mod. </summary>
    [JsonIgnore]
    public string BasePath { get; set; } = null!;

    /// <summary> Gets a value indicating whether this mod definition is valid. </summary>
    [JsonIgnore]
    public bool IsValid => !string.IsNullOrEmpty(Identifier) && !string.IsNullOrEmpty(Name);
}
