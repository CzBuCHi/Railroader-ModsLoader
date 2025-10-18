using System.Collections.Concurrent;
using HarmonyLib;
using JetBrains.Annotations;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.ModInjector.PluginPatchers;

/// <summary> Patches types implementing <see cref="IHarmonyPlugin"/> to apply or remove Harmony patches when <c>OnIsEnabledChanged</c> is called. </summary>
public sealed class HarmonyPluginPatcher(ILogger logger) : PluginPatcherBase<IHarmonyPlugin, HarmonyPluginPatcher>(logger)
{
    private static readonly ConcurrentDictionary<IPluginBase, Harmony> _Harmony = new();

    /// <summary> Handles the <c>OnIsEnabledChanged</c> event for the plugin, performing patcher-specific logic when the plugin is enabled or disabled. </summary>
    /// <param name="plugin">The plugin instance. Must not be null.</param>
    [UsedImplicitly]
    public static void OnIsEnabledChanged(IPluginBase plugin) {
        var harmony = _Harmony.GetOrAdd(plugin, o => new Harmony(o.Mod.Definition.Identifier))!;

        var logger = DI.GetLogger(plugin.Mod.Definition.Identifier + ".HarmonyPluginPatcher");
        if (plugin.IsEnabled) {
            logger.Information("Applying Harmony patches for mod {ModId}", plugin.Mod.Definition.Identifier);
            harmony.PatchAll(plugin.GetType().Assembly);
        } else {
            logger.Information("Removing Harmony patches for mod {ModId}", plugin.Mod.Definition.Identifier);
            harmony.UnpatchAll(plugin.Mod.Definition.Identifier);
        }
    }
}
