namespace Railroader.ModInterfaces;

/// <summary> Internal interface for plugin instances. </summary>
internal interface IPlugin
{
    /// <summary> Gets or sets a value indicating whether this plugin is enabled. </summary>
    bool IsEnabled { get; set; }

    /// <summary> Gets the modding context for this plugin. </summary>
    IModdingContext ModdingContext { get; }

    /// <summary> Gets the mod definition for this plugin. </summary>
    IModDefinition ModDefinition { get; }
}