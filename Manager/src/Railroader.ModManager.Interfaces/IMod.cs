using JetBrains.Annotations;
using Serilog;

namespace Railroader.ModManager.Interfaces;

/// <summary> Represents a loaded mod instance. </summary>
[PublicAPI]
public interface IMod
{
    /// <summary> Gets the definition/metadata for this mod. </summary>
    IModDefinition Definition { get; }

    /// <summary> Gets a value indicating whether this mod is enabled. </summary>
    bool IsEnabled { get; }

    /// <summary> Gets a value indicating whether this mod is valid. </summary>
    bool IsValid { get; }

    /// <summary> Gets a value indicating whether this mod is loaded. </summary>
    bool IsLoaded { get; }

    /// <summary> Gets the plugins provided by this mod, or <see langword="null" /> if no plugins are available. </summary>
    IPlugin[]? Plugins { get; }

    /// <summary> Creates a scoped logger for this mod. </summary>
    /// <param name="scope">
    ///     The optional scope name to append to the logger context.
    ///     If <see langword="null" />, only the mod identifier is used.
    /// </param>
    /// <returns>A configured logger instance.</returns>
    ILogger CreateLogger(string? scope = null);


    /// <summary>
    ///     Loads settings from a JSON file in the mods directory using the specified identifier.
    /// </summary>
    /// <typeparam name="T">The type of the settings object to load. Must be a reference type.</typeparam>
    /// <param name="identifier">
    ///     A string used to construct the settings filename. Must consist only of letters,  digits, underscores, and hyphens.
    ///     The file will be loaded from  <c>{mod_directory}/{identifier}.json</c>.
    /// </param>
    /// <returns>
    ///     The deserialized settings object of type <typeparamref name="T" /> if the file exists and is valid;
    ///     otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     This method attempts to read and deserialize a JSON file located at
    ///     <c>{mod_directory}/{identifier}.json</c>. If the file does not exist, is empty,
    ///     contains invalid JSON, or cannot be deserialized into <typeparamref name="T" />,
    ///     the method returns <c>null</c> without throwing an exception.
    /// </remarks>
    T? LoadSettings<T>(string identifier) where T : class;

    /// <summary>
    ///     Saves settings to a JSON file in the mods directory using the specified identifier.
    /// </summary>
    /// <typeparam name="T">The type of the settings object to save. Must be a reference type.</typeparam>
    /// <param name="identifier">
    ///     A string used to construct the settings filename. Must consist only of letters,  digits, underscores, and hyphens.
    ///     The file will be saved as  <c>{mod_directory}/{identifier}.json</c>.
    /// </param>
    /// <param name="settings">The settings object to serialize and save.</param>
    /// <remarks>
    ///     This method serializes the <paramref name="settings" /> object to JSON and writes it to
    ///     <c>{mod_directory}/{identifier}.json</c>. The mods directory is created automatically
    ///     if it does not exist. Existing files with the same identifier will be overwritten.
    /// </remarks>
    void SaveSettings<T>(string identifier, T settings) where T : class;
}
