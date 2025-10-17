using JetBrains.Annotations;
using Serilog;

namespace Railroader.ModInterfaces;

/// <summary> Base class for all plugins, providing common functionality and modding context access. </summary>
/// <remarks> This class is intended to be inherited by concrete plugin implementations. </remarks>
[PublicAPI]
public abstract class PluginBase(IModdingContext moddingContext, IModDefinition modDefinition) : IPlugin
{
    /// <summary> Gets the modding context for this plugin. </summary>
    public IModdingContext ModdingContext { get; } = moddingContext;

    /// <summary> Gets the mod definition for this plugin. </summary>
    public IModDefinition ModDefinition { get; } = modDefinition;

    /// <summary> Creates a scoped logger for this plugin. </summary>
    /// <param name="scope">
    /// The optional scope name to append to the logger context.
    /// If <see langword="null"/>, only the mod identifier is used.
    /// </param>
    /// <returns>A configured logger instance.</returns>
    public ILogger CreateLogger(string? scope = null) => 
        Log.ForContext("SourceContext", $"{ModDefinition.Identifier}{(scope != null ? $".{scope}" : "")}");

    private bool _IsEnabled;

    /// <summary> Gets or sets a value indicating whether this plugin is enabled. </summary>
    public bool IsEnabled
    {
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
    protected virtual void OnIsEnabledChanged()
    {
    }
}