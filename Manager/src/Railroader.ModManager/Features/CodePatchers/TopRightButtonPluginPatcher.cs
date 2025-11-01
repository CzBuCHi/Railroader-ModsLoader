using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using JetBrains.Annotations;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Interfaces;
using Serilog;
using UI;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;
using ILogger = Serilog.ILogger;
using Object = UnityEngine.Object;

namespace Railroader.ModManager.Features.CodePatchers;

/// <summary> Patches types implementing <see cref="ITopRightButtonPlugin"/> to add UI button in the top-right area. </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public static class TopRightButtonPluginPatcher
{
    // NOTE: This class is not tested, because code calls Unity engine methods -  if called from outside Unity they all throw this exception:
    // System.Security.SecurityException("ECall methods must be packaged into a system module.")

    [ExcludeFromCodeCoverage]
    public static TypePatcherDelegate Factory() => Factory(Log.Logger.ForSourceContext());

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TypePatcherDelegate Factory(ILogger logger) {
        var method = MethodPatcher.Factory<ITopRightButtonPlugin>(logger, typeof(TopRightButtonPluginPatcher), typeof(PluginBase<>), "OnIsEnabledChanged");
        return (assemblyDefinition, typeDefinition) => method(assemblyDefinition, typeDefinition);
    }

    private sealed record PatcherState(bool IsEnabled, GameObject? GameObject);

    private static readonly ConcurrentDictionary<IPlugin, PatcherState> _States = new();

    /// <summary> Handles the <c>OnIsEnabledChanged</c> event for the plugin, performing patcher-specific logic when the plugin is enabled or disabled. </summary>
    /// <param name="plugin">The plugin instance. Must not be null.</param>
    /// <remarks>Method called from plugin.</remarks>
    [UsedImplicitly]
    public static void OnIsEnabledChanged(ITopRightButtonPlugin plugin) {
        var topRightArea = Object.FindObjectOfType<TopRightArea>();
        if (topRightArea == null) {
            return;
        }

        var state = _States.GetOrAdd(plugin, o => new PatcherState(!o.IsEnabled, null))!;
        if (state.IsEnabled == plugin.IsEnabled) {
            return;
        }

        var logger = ((ModdingContext)plugin.ModdingContext).Logger;
        if (plugin.IsEnabled) {
            logger.Information("Applying TopRightButton patch for mod {ModId}", plugin.Mod.Definition.Identifier);

            try {
                var gameObject = AddButton(plugin, topRightArea);
                _States[plugin] = new PatcherState(true, gameObject);
            } catch (Exception exc) {
                logger.Error(exc, "Failed to add button to top right area.");
                throw;
            }
        } else {
            logger.Information("Removing TopRightButton patch for mod {ModId}", plugin.Mod.Definition.Identifier);

            if (state.GameObject != null) {
                Object.Destroy(state.GameObject);
                _States[plugin] = new PatcherState(false, null);
            }
        }
    }

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    private static GameObject AddButton(ITopRightButtonPlugin plugin, TopRightArea topRightArea) {
        var texture = LoadButtonTexture(plugin);

        var componentInChildren = topRightArea.transform.Find("Strip").gameObject.GetComponentInChildren<Button>();
        var gameObject          = Object.Instantiate(componentInChildren.gameObject, componentInChildren.transform.parent);
        gameObject.transform.SetSiblingIndex(plugin.Index);

        gameObject.GetComponent<UITooltipProvider>().TooltipInfo = new TooltipInfo(plugin.Tooltip, string.Empty);

        var button = gameObject.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => plugin.OnClick());

        var image = gameObject.GetComponent<Image>();
        image.sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, 128, 128), new Vector2(0.5f, 0.5f));

        return gameObject;
    }

    private static Texture2D LoadButtonTexture(ITopRightButtonPlugin plugin) {
        var pluginType = plugin.GetType();
        var path       = $"{pluginType.Namespace}.{plugin.IconName}";

        byte[] bytes;
        using (var stream = pluginType.Assembly.GetManifestResourceStream(path)) {
            using (var ms = new MemoryStream()) {
                stream!.CopyTo(ms);
                bytes = ms.ToArray();
            }
        }

        var texture = new Texture2D(128, 128, TextureFormat.DXT5, false);
        texture.LoadImage(bytes);
        return texture;
    }
}
