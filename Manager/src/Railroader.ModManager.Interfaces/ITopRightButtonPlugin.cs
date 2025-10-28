using System;
using JetBrains.Annotations;

namespace Railroader.ModManager.Interfaces;

/// <summary> Defines a plugin that adds a custom button to the top-right UI area. </summary>
[PublicAPI]
public interface ITopRightButtonPlugin : IPlugin
{
    /// <summary> Gets the name of the icon to display for this button. </summary>
    string IconName { get; }

    /// <summary> Gets the tooltip text to display when hovering over this button. </summary>
    string Tooltip { get; }

    /// <summary> Gets the display index for this button (lower values appear first). </summary>
    int Index { get; }

    /// <summary> Gets the action to execute when this button is clicked. </summary>
    Action OnClick { get; }
}