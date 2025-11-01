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

    /// <summary> Gets the plugins provided by this mod, or <see langword="null"/> if no plugins are available. </summary>
    IPlugin[]? Plugins { get; }

    /// <summary> Creates a scoped logger for this mod. </summary>
    /// <param name="scope">
    /// The optional scope name to append to the logger context.
    /// If <see langword="null"/>, only the mod identifier is used.
    /// </param>
    /// <returns>A configured logger instance.</returns>
    ILogger CreateLogger(string? scope = null);
}