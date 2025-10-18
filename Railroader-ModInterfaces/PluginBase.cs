using System;
using JetBrains.Annotations;

namespace Railroader.ModInterfaces;

[PublicAPI]
public interface IPluginBase
{
    /// <summary> Gets the modding context for this plugin. </summary>
    IModdingContext ModdingContext { get; }

    /// <summary> Gets the mod for this plugin. </summary>
    IMod Mod { get; }

    /// <summary> Gets or sets a value indicating whether this plugin is enabled. </summary>
    bool IsEnabled { get; set; }
}

/// <summary> Base class for all plugins, providing common functionality and modding context access. </summary>
/// <remarks> This class is intended to be inherited by concrete plugin implementations. </remarks>
[PublicAPI]
public abstract class PluginBase<T> : IPluginBase where T : PluginBase<T>
{
    private static T? _Instance;

    /// <summary> Gets the singleton instance of this plugin type. </summary>
    /// <exception cref="InvalidOperationException"> Thrown if the instance has not been created yet. </exception>
    public static T Instance => _Instance ?? throw new InvalidOperationException($"{typeof(T).Name} was not created");

    /// <summary> Initializes a new instance of the <see cref="PluginBase{T}"/> class. </summary>
    /// <param name="moddingContext">The modding context.</param>
    /// <param name="mod">The mod definition.</param>
    /// <exception cref="InvalidOperationException"> Thrown if an instance of this type already exists. </exception>
    protected PluginBase(IModdingContext moddingContext, IMod mod) {
        if (_Instance != null) {
            throw new InvalidOperationException($"Cannot create plugin '{GetType()}' twice.");
        }

        _Instance = (T)this;

        ModdingContext = moddingContext;
        Mod = mod;
    }

    /// <inheritdoc />
    public IModdingContext ModdingContext { get; }

    /// <inheritdoc />
    public IMod Mod { get; }

    private bool _IsEnabled;

    /// <inheritdoc />
    public bool IsEnabled {
        get => _IsEnabled;
        set {
            if (_IsEnabled == value) {
                return;
            }

            _IsEnabled = value;
            OnIsEnabledChanged();
        }
    }

    /// <summary> Called when the <see cref="IsEnabled"/> property changes. </summary>
    /// <remarks>
    /// Override this method to handle enable/disable events.
    /// The base implementation does nothing.
    /// </remarks>
    protected virtual void OnIsEnabledChanged() {
    }
}
