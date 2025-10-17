using Railroader.ModInterfaces;

namespace Railroader.ModInjector;

/// <summary> Implementation of <see cref="IMod"/> for a loaded mod instance. </summary>
internal sealed class Mod(IModDefinition modDefinition, string? outputDllPath) : IMod
{
    /// <inheritdoc />
    public IModDefinition Definition { get; } = modDefinition;

    /// <summary> Gets or sets the output DLL path for this mod. </summary>
    public string? OutputDllPath { get; } = outputDllPath;

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
    public PluginBase[]? Plugins { get; internal set; }
}