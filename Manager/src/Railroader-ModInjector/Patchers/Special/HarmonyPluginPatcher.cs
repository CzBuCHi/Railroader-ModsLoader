using System.Collections.Concurrent;
using JetBrains.Annotations;
using Railroader.ModInjector.Wrappers;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.ModInjector.Patchers.Special;

/// <summary> Patches types implementing <see cref="IHarmonyPlugin"/> to apply or remove Harmony patches when <c>OnIsEnabledChanged</c> is called. </summary>
public sealed class HarmonyPluginPatcher(ILogger logger) : TypePatcher(
    [new MethodPatcher<IHarmonyPlugin, HarmonyPluginPatcher>(logger, typeof(PluginBase<>), "OnIsEnabledChanged")]
)
{
    private sealed record PatcherState(bool IsEnabled, IHarmonyWrapper Harmony);

    private static readonly ConcurrentDictionary<IPluginBase, PatcherState> _States = new();

    /// <summary> Handles the <c>OnIsEnabledChanged</c> event for the plugin, performing patcher-specific logic when the plugin is enabled or disabled. </summary>
    /// <param name="plugin">The plugin instance. Must not be null.</param>
    [UsedImplicitly]
    public static void OnIsEnabledChanged(IPluginBase plugin) {
        var state = _States.GetOrAdd(plugin, o => new PatcherState(!plugin.IsEnabled, DI.HarmonyWrapper(o.Mod.Definition.Identifier)))!;
        if (state.IsEnabled == plugin.IsEnabled) {
            return;
        }

        _States[plugin] = state with { IsEnabled = plugin.IsEnabled };

        var logger = DI.GetLogger();
        if (plugin.IsEnabled) {
            logger.Information("Applying Harmony patch for mod {ModId}", plugin.Mod.Definition.Identifier);
            state.Harmony.PatchAll(plugin.GetType().Assembly);
        } else {
            logger.Information("Removing Harmony patch for mod {ModId}", plugin.Mod.Definition.Identifier);
            state.Harmony.UnpatchAll(plugin.Mod.Definition.Identifier);
        }
    }
}

