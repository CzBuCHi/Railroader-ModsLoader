using Railroader.ModInterfaces;

namespace Railroader.ModInjector;

internal sealed class Mod(IModDefinition modDefinition, string? outputDllPath) : IMod
{
    private bool           _IsEnabled;
    public  string?        OutputDllPath { get; } = outputDllPath;
    public  IModDefinition Definition    { get; } = modDefinition;

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

    public bool          IsLoaded  { get; internal set; }
    public PluginBase[]? Plugins   { get; internal set; }
}
