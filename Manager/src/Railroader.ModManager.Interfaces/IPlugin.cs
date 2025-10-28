using JetBrains.Annotations;

namespace Railroader.ModManager.Interfaces;

/// <summary> Defines the contract for plugins in the modding system, providing access to modding context and mod metadata. </summary>
/// <remarks>
/// This interface is intended to be implemented only by classes derived from <see cref="PluginBase{T}"/>.
/// Direct implementation by other classes is not supported, as the modding system relies on the singleton
/// and initialization logic provided by <see cref="PluginBase{T}"/>.
/// </remarks>
[PublicAPI]
public interface IPlugin
{
    /// <summary> Gets the modding context for this plugin. </summary>
    IModdingContext ModdingContext { get; }

    /// <summary> Gets the mod for this plugin. </summary>
    IMod Mod { get; }

    /// <summary> Gets or sets a value indicating whether this plugin is enabled. </summary>
    bool IsEnabled { get; set; }
}
