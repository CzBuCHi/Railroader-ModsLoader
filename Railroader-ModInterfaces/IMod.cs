using JetBrains.Annotations;

namespace Railroader.ModInterfaces;

/// <summary> Represents a loaded mod instance. </summary>
[PublicAPI]
public interface IMod
{
    /// <summary> Gets the definition/metadata for this mod. </summary>
    IModDefinition Definition { get; }

    /// <summary> Gets a value indicating whether this mod is enabled. </summary>
    bool IsEnabled { get; }

    /// <summary> Gets a value indicating whether this mod is loaded. </summary>
    bool IsLoaded { get; }

    /// <summary> Gets the plugins provided by this mod, or <see langword="null"/> if no plugins are available. </summary>
    PluginBase[]? Plugins { get; }
}