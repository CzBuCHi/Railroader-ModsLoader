using JetBrains.Annotations;

namespace Railroader.ModManager.Interfaces;

/// <summary> Marker interface for plugins that want to use harmony to patch game code. </summary>
[PublicAPI]
public interface IHarmonyPlugin : IPlugin;