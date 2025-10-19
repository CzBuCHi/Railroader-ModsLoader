using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using JetBrains.Annotations;
using Railroader.ModInterfaces;
using UI;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;
using ILogger = Serilog.ILogger;
using Object = UnityEngine.Object;

namespace Railroader.ModInjector.Patchers.Special;

/// <summary>
/// Patches types implementing <see cref="ITopRightButtonPlugin"/> to add UI button in the top-right area.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TopRightButtonPluginPatcher(ILogger logger) : TypePatcher(
    [new MethodPatcher<ITopRightButtonPlugin, TopRightButtonPluginPatcher>(logger, typeof(PluginBase<>), "OnIsEnabledChanged")]
)
{
    // NOTE: This class is not tested, because code calls Unity engine methods -  if called from outside Unity they all throw this exception:
    // System.Security.SecurityException("ECall methods must be packaged into a system module.")

    private sealed record PatcherState(bool IsEnabled, GameObject? GameObject);

    private static readonly ConcurrentDictionary<IPluginBase, PatcherState> _States = new();

    /// <summary> Handles the <c>OnIsEnabledChanged</c> event for the plugin, performing patcher-specific logic when the plugin is enabled or disabled. </summary>
    /// <param name="plugin">The plugin instance. Must not be null.</param>
    [UsedImplicitly]
  
    public static void OnIsEnabledChanged(IPluginBase plugin) {
        var topRightArea = Object.FindObjectOfType<TopRightArea>();
        if (topRightArea == null) {
            return;
        }

        var state = _States.GetOrAdd(plugin, o => new PatcherState(!o.IsEnabled, null))!;
        if (state.IsEnabled == plugin.IsEnabled) {
            return;
        }

        var logger = DI.GetLogger(plugin.Mod.Definition.Identifier + "." + nameof(TopRightButtonPluginPatcher));
        if (plugin.IsEnabled) {
            logger.Information("Applying TopRightButton patch for mod {ModId}", plugin.Mod.Definition.Identifier);

            var gameObject = AddButton(plugin, topRightArea);
            _States[plugin] = new PatcherState(true, gameObject);
        } else {
            logger.Information("Removing TopRightButton patch for mod {ModId}", plugin.Mod.Definition.Identifier);

            if (state.GameObject != null) {
                Object.Destroy(state.GameObject);
                _States[plugin] = state with { IsEnabled = false };
            }
        }
    }

    private static Texture2D? LoadButtonTexture(IPluginBase plugin) {
        var topRightButton = (ITopRightButtonPlugin)plugin;

        var pluginType = plugin.GetType();
        var path       = $"{pluginType.Namespace}.{topRightButton.IconName}";

        try {
            byte[] bytes;
            using (var stream = pluginType.Assembly.GetManifestResourceStream(path)!) {
                using (var ms = new MemoryStream()) {
                    stream.CopyTo(ms);
                    bytes = ms.ToArray();
                }
            }

            var texture = new Texture2D(128, 128, TextureFormat.DXT5, false);
            texture.LoadImage(bytes);
            return texture;
        } catch (Exception exc) {
            DI.GetLogger(plugin.Mod.Definition.Identifier + ".HarmonyPluginPatcher").Error(exc, "Failed to load texture {0}", path);
            return null;
        }
    }

    private static GameObject? AddButton(IPluginBase plugin, TopRightArea topRightArea) {
        var texture = LoadButtonTexture(plugin);
        if (texture == null) {
            return null;
        }

        var topRightButton = (ITopRightButtonPlugin)plugin;

        var componentInChildren = topRightArea.transform.Find("Strip")!.gameObject.GetComponentInChildren<Button>()!;
        var gameObject          = Object.Instantiate(componentInChildren.gameObject, componentInChildren.transform.parent!)!;
        gameObject.transform.SetSiblingIndex(topRightButton.Index);

        gameObject.GetComponent<UITooltipProvider>()!.TooltipInfo = new TooltipInfo(topRightButton.Tooltip, string.Empty);

        var button = gameObject.GetComponent<Button>()!;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => topRightButton.OnClick());

        var image = gameObject.GetComponent<Image>()!;
        image.sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, 128, 128), new Vector2(0.5f, 0.5f))!;

        return gameObject;
    }
}
