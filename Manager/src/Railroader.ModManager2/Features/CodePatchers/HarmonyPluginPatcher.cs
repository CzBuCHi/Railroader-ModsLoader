using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Railroader.ModManager.Delegates.HarmonyLib;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Interfaces;
using Serilog;

namespace Railroader.ModManager.Features.CodePatchers;

/// <summary> Patches types implementing <see cref="IHarmonyPlugin"/> to apply or remove Harmony patches when <c>OnIsEnabledChanged</c> is called. </summary>
[PublicAPI]
public sealed class HarmonyPluginPatcher
{
    [ExcludeFromCodeCoverage]
    public static TypePatcherDelegate Factory() => Factory(Log.Logger.ForSourceContext());

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TypePatcherDelegate Factory(ILogger logger) {
        var method = MethodPatcher.Factory<IHarmonyPlugin>(logger, typeof(HarmonyPluginPatcher), typeof(PluginBase<>), "OnIsEnabledChanged");
        return (assemblyDefinition, typeDefinition) => method(assemblyDefinition, typeDefinition);
    }

    private sealed record PatcherState(bool IsEnabled, IHarmony Harmony);

    private static readonly ConcurrentDictionary<IPlugin, PatcherState> _States = new();

    /// <summary> Handles the <c>OnIsEnabledChanged</c> event for the plugin, performing patcher-specific logic when the plugin is enabled or disabled. </summary>
    /// <param name="plugin">The plugin instance. Must not be null.</param>
    /// <remarks>Method called from plugin.</remarks>
    [UsedImplicitly]
    public static void OnIsEnabledChanged(IHarmonyPlugin plugin) {
        var context = (ModdingContext)plugin.ModdingContext;

        var state = _States.GetOrAdd(plugin, _ => new PatcherState(!plugin.IsEnabled, context.HarmonyFactory(plugin.Mod.Definition.Identifier)))!;
        if (state.IsEnabled == plugin.IsEnabled) {
            return;
        }

        _States[plugin] = state with { IsEnabled = plugin.IsEnabled };

        if (plugin.IsEnabled) {
            context.Logger.Information("Applying Harmony patch for mod {ModId}", plugin.Mod.Definition.Identifier);
            state.Harmony.PatchAll(plugin.GetType().Assembly);
        } else {
            context.Logger.Information("Removing Harmony patch for mod {ModId}", plugin.Mod.Definition.Identifier);
            state.Harmony.UnpatchAll(plugin.Mod.Definition.Identifier);
        }
    }
}
