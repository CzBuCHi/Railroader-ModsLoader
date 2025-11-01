using System.Linq;
using Newtonsoft.Json;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Interfaces;
using Railroader.ModManager.Services;
using Serilog;

namespace Railroader.ModManager;

/// <summary> Implementation of <see cref="IMod"/> for a loaded mod instance. </summary>
public sealed class Mod(IModDefinition modDefinition, string? assemblyPath) : IMod
{
    /// <inheritdoc />
    public IModDefinition Definition { get; } = modDefinition;

    /// <summary> Gets or sets the output DLL path for this mod. </summary>
    public string? AssemblyPath { get; } = assemblyPath;

    private bool _IsEnabled;

    /// <inheritdoc />
    public bool IsEnabled
    {
        get => _IsEnabled;
        internal set
        {
            if (_IsEnabled == value) {
                return;
            }

            _IsEnabled = value;

            if (Plugins != null) {
                foreach (var plugin in Plugins) {
                    plugin.IsEnabled = value;
                }
            }
        }
    }

    /// <inheritdoc />
    public bool IsLoaded { get; internal set; }

    /// <inheritdoc />
    [JsonIgnore]
    public IPlugin[]? Plugins { get; internal set; }

    [JsonProperty("Plugins")]
    public string[]? PluginNames => Plugins?.Select(o => o.GetType().FullName).ToArray();

    /// <inheritdoc />
    public ILogger CreateLogger(string? scope = null)
        => ModManager.ServiceProvider.GetService<ILoggerFactory>().GetLogger(scope == null ? Definition.Identifier : $"{Definition.Identifier}.{scope}");
}