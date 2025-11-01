using System.Collections.Concurrent;
using JetBrains.Annotations;
using Railroader.ModManager.Delegates.HarmonyLib.Harmony;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Interfaces;
using Railroader.ModManager.Services;

namespace Railroader.ModManager.CodePatchers.Special;

/// <summary> Patches types implementing <see cref="IHarmonyPlugin"/> to apply or remove Harmony patches when <c>OnIsEnabledChanged</c> is called. </summary>
public sealed class HarmonyPluginPatcher : TypePatcher
{
    public HarmonyPluginPatcher(ILoggerFactory loggerFactory)
        : base([new MethodPatcher<IHarmonyPlugin, HarmonyPluginPatcher>(loggerFactory, typeof(PluginBase<>), "OnIsEnabledChanged")]) {
    }

    private sealed record PatcherState(bool IsEnabled, IHarmony Harmony);

    private static readonly ConcurrentDictionary<IPlugin, PatcherState> _States = new();

    // todo: not mocked
    private static IHarmony CreateHarmonyWrapper(string id) => Harmony.Create(id);

    /// <summary> Handles the <c>OnIsEnabledChanged</c> event for the plugin, performing patcher-specific logic when the plugin is enabled or disabled. </summary>
    /// <param name="plugin">The plugin instance. Must not be null.</param>
    [UsedImplicitly]
    public static void OnIsEnabledChanged(IPlugin plugin) {
        var state = _States.GetOrAdd(plugin, _ => new PatcherState(!plugin.IsEnabled, CreateHarmonyWrapper(plugin.Mod.Definition.Identifier)))!;
        if (state.IsEnabled == plugin.IsEnabled) {
            return;
        }

        _States[plugin] = state with { IsEnabled = plugin.IsEnabled };

        var logger = ModManager.ServiceProvider.GetService<ILoggerFactory>().GetLogger();
        if (plugin.IsEnabled) {
            logger.Information("Applying Harmony patch for mod {ModId}", plugin.Mod.Definition.Identifier);
            state.Harmony.PatchAll(plugin.GetType().Assembly);
        } else {
            logger.Information("Removing Harmony patch for mod {ModId}", plugin.Mod.Definition.Identifier);
            state.Harmony.UnpatchAll(plugin.Mod.Definition.Identifier);
        }
    }
}
