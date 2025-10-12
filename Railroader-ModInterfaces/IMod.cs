using JetBrains.Annotations;

namespace Railroader.ModInterfaces;

/// <summary> An interface containing information about a mod.  </summary>
[PublicAPI]
public interface IMod
{
    /// <summary> Basic information about a mod loaded from Definition.json. </summary>
    IModDefinition Definition { get; }

    /// <summary> Gets whether the mod has been enabled. Only enabled mods will be loaded. </summary>
    bool IsEnabled { get; }

    /// <summary> Gets whether the mod has been loaded.  </summary>
    bool IsLoaded { get; }

    /// <summary> Gets the plugins that have been registered with this mod, if any. </summary>
    PluginBase[]? Plugins { get; }
}