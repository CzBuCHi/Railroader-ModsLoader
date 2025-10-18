using System;
using System.IO;
using JetBrains.Annotations;
using Railroader.ModInterfaces;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;
using ILogger = Serilog.ILogger;
using Object = UnityEngine.Object;

namespace Railroader.ModInjector.PluginPatchers;

/// <summary>
/// Patches types implementing <see cref="ITopRightButtonPlugin"/> to add or configure a UI button in the top-right area
/// when <c>OnIsEnabledChanged</c> is called.
/// </summary>
public sealed class TopRightButtonPluginPatcher(ILogger logger) : PluginPatcherBase<ITopRightButtonPlugin, TopRightButtonPluginPatcher>(logger)
{
    /// <summary> Handles the <c>OnIsEnabledChanged</c> event for the plugin, performing patcher-specific logic when the plugin is enabled or disabled. </summary>
    /// <param name="plugin">The plugin instance. Must not be null.</param>
    [UsedImplicitly]
    public static void OnIsEnabledChanged(IPluginBase plugin) {
        var topRightButton = (ITopRightButtonPlugin)plugin;

        var topRightArea = Object.FindObjectOfType<UI.TopRightArea>();
        if (topRightArea == null) {
            return;
        }

        var pluginType = plugin.GetType();
        var path       = $"{pluginType.Namespace}.{topRightButton.IconName}";

        Texture2D texture;
        try {
            byte[] bytes;
            using (var stream = pluginType.Assembly.GetManifestResourceStream(path)!) {
                using (var ms = new MemoryStream()) {
                    stream.CopyTo(ms);
                    bytes = ms.ToArray();
                }
            }

            texture = new Texture2D(128, 128, TextureFormat.DXT5, false);
            texture.LoadImage(bytes);
        } catch (Exception exc) {
            DI.GetLogger(plugin.Mod.Definition.Identifier + ".HarmonyPluginPatcher").Error(exc, "Failed to load texture {0}", path);
            return;
        }

        var componentInChildren = topRightArea.transform.Find("Strip")!.gameObject.GetComponentInChildren<Button>()!;
        var gameObject          = Object.Instantiate(componentInChildren.gameObject, componentInChildren.transform.parent!)!;
        gameObject.transform.SetSiblingIndex(topRightButton.Index);

        gameObject.GetComponent<UITooltipProvider>()!.TooltipInfo = new TooltipInfo(topRightButton.Tooltip, string.Empty);

        var button = gameObject.GetComponent<Button>()!;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => topRightButton.OnClick());

        var image = gameObject.GetComponent<Image>()!;
        image.sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, 128, 128), new Vector2(0.5f, 0.5f))!;
    }
}
