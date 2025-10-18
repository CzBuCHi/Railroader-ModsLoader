using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.ModInjector;

/// <summary> Implementation of <see cref="IMod"/> for a loaded mod instance. </summary>
internal sealed class Mod(IModDefinition modDefinition, string? assemblyPath) : IMod
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
    public IPluginBase[]? Plugins { get; internal set; }

    /// <inheritdoc />
    public ILogger CreateLogger(string? scope = null)
        => DI.GetLogger(scope == null ? Definition.Identifier : $"{Definition.Identifier}.{scope}");
}