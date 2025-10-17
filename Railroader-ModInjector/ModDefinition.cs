using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Railroader.ModInterfaces;
using Serilog.Events;

namespace Railroader.ModInjector;

/// <summary> Implementation of <see cref="IModDefinition"/> for mod metadata. </summary>
internal sealed class ModDefinition : IModDefinition
{
    /// <inheritdoc />
    [JsonProperty("id")]
    public string Identifier { get; set; } = null!;

    /// <inheritdoc />
    [JsonProperty]
    public string Name { get; set; } = null!;

    /// <inheritdoc />
    [JsonIgnore]
    public Version Version { get; set; } = null!;

    /// <summary> Gets a value indicating whether the version is valid. </summary>
    [JsonIgnore]
    public bool VersionValid { get; private set; }

    /// <summary> JSON serializer backing field for <see cref="Version"/>. </summary>
    [JsonProperty(nameof(Version))]
    private string VersionRaw
    {
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

    /// <inheritdoc />
    [JsonIgnore]
    public LogEventLevel? LogLevel { get; set; }

    /// <summary> Gets a value indicating whether the log level is valid. </summary>
    [JsonIgnore]
    public bool LogLevelValid { get; private set; }

    /// <summary> JSON serializer backing field for <see cref="LogLevel"/>. </summary>
    [JsonProperty(nameof(LogLevel))]
    private string? LogLevelRaw
    {
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

    /// <summary> Gets or sets the base directory path for the mod. </summary>
    [JsonIgnore]
    public string BasePath { get; set; } = null!;

    /// <summary> Gets a value indicating whether this mod definition is valid. </summary>
    public bool IsValid => !string.IsNullOrEmpty(Identifier) && !string.IsNullOrEmpty(Name) && VersionValid && LogLevelValid;
}
