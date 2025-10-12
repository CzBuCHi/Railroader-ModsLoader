using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Railroader.ModInterfaces;
using Serilog.Events;

namespace Railroader.ModInjector;

/// <summary> Serialized as Definition.json. </summary>
internal sealed class ModDefinition : IModDefinition
{
    [JsonProperty]
    public string Id { get; set; } = null!;

    [JsonProperty]
    public string Name { get; set; } = null!;

    [JsonIgnore]
    public Version Version { get; set; } = null!;

    [JsonIgnore]
    public bool VersionValid { get; private set; }

    [JsonProperty(nameof(Version))]
    private string VersionRaw {
        [ExcludeFromCodeCoverage]
        get => Version.ToString(3)!;
        set {
            if (Version.TryParse(value, out var version) && version != null) {
                Version = version;
                VersionValid = true;
            } else {
                Version = new Version(1, 0);
            }
        }
    }

    [JsonIgnore]
    public LogEventLevel? LogLevel { get; set; }

    [JsonIgnore]
    public bool LogLevelValid { get; private set; }

    [JsonProperty(nameof(LogLevel))]
    private string? LogLevelRaw {
        [ExcludeFromCodeCoverage]
        get => LogLevel != null ? LogLevel.ToString() : null;
        set {
            if (value == null) {
                LogLevel = LogEventLevel.Information;
                LogLevelValid = true;
            } else if (Enum.TryParse<LogEventLevel>(value, out var parsed)) {
                LogLevel = parsed;
                LogLevelValid = true;
            } else {
                LogLevel = LogEventLevel.Information;
                LogLevelValid = false;
            }
        }
    }

    [JsonIgnore]
    public string DefinitionPath { get; set; } = null!;

    public bool IsValid => !string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(Name) && VersionValid && LogLevelValid;
}
