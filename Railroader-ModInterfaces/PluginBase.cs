using JetBrains.Annotations;
using Serilog;

namespace Railroader.ModInterfaces;

/// <summary> The base class for .NET-based plugins. </summary>
/// <param name="moddingContext">Instance of shared <see cref="IModdingContext"/>.</param>
[PublicAPI]
public abstract class PluginBase(IModdingContext moddingContext)
{
    /// <summary> Logger instance. </summary>
    public ILogger Logger => Log.ForContext(GetType())!;

    /// <summary> Instance of shared <see cref="IModdingContext"/>. </summary>
    public IModdingContext ModdingContext { get; } = moddingContext;

    private bool _IsEnabled;

    /// <summary> Gets or sets whether the plugin has been enabled. </summary>
    public bool IsEnabled {
        get => _IsEnabled;
        set {
            if (_IsEnabled == value) {
                return;
            }

            _IsEnabled = value;

            if (_IsEnabled) {
                OnEnable();
            } else {
                OnDisable();
            }
        }
    }

    /// <summary> Called when the mod has been enabled. </summary>
    public virtual void OnEnable() {
    }

    /// <summary> Called when the mod has been disabled. </summary>
    public virtual void OnDisable() {
    }
}