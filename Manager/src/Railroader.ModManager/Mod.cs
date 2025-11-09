using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Railroader.ModManager.Delegates.System.IO.File;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Interfaces;
using Serilog;

namespace Railroader.ModManager;

/// <summary> Implementation of <see cref="IMod"/> for a loaded mod instance. </summary>
public sealed class Mod(ILogger logger, ModDefinition modDefinition, ReadAllText readAllText, WriteAllText writeAllText) : IMod
{
    internal static Mod Create(ILogger logger, ModDefinition modDefinition) => 
        new(logger, modDefinition, File.ReadAllText, File.WriteAllText);

    /// <inheritdoc />
    public IModDefinition Definition => modDefinition;

    /// <summary> Gets or sets the output DLL path for this mod. </summary>
    public string? AssemblyPath { get; internal set; }

    private bool _IsEnabled;
    
    /// <inheritdoc />
    public bool IsEnabled {
        get => _IsEnabled;
        internal set {
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
    public bool IsValid { get; internal set; }

    /// <inheritdoc />
    public bool IsLoaded { get; internal set; }

    /// <inheritdoc />
    [JsonIgnore]
    public IPlugin[]? Plugins { get; internal set; }

    [JsonProperty("Plugins")]
    public string[]? PluginNames => Plugins?.Select(o => o.GetType().FullName).ToArray();

    /// <inheritdoc />
    public ILogger CreateLogger(string? scope = null) =>
        logger.ForSourceContext(scope == null ? Definition.Identifier : $"{Definition.Identifier}.{scope}");

    private string GetSettingsFilePath(string identifier) => Path.Combine(modDefinition.BasePath, identifier + ".json");
    
    private readonly JsonSerializerSettings _JsonSerializerSettings = new() {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        }
    };
    
    /// <inheritdoc />
    public T? LoadSettings<T>(string identifier) where T : class => 
        JsonConvert.DeserializeObject<T>(readAllText(GetSettingsFilePath(identifier)), _JsonSerializerSettings);

    /// <inheritdoc />
    public void SaveSettings<T>(string identifier, T settings) where T : class => 
        writeAllText(GetSettingsFilePath(identifier), JsonConvert.SerializeObject(settings, _JsonSerializerSettings));
}
