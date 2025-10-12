using System;
using System.Collections.Generic;
using UI.Builder;
using UI.Common;

namespace Railroader.ModInterfaces;

/// <summary> An injectable interface that allows access to other mods and some quality-of-life functionality. </summary>
public interface IModdingContext
{
    /// <summary> Gets the list of all mods. This includes loaded, enabled, disabled, and failed mods. </summary>
    IReadOnlyCollection<IMod> Mods { get; }

    /// <summary> Loads the settings with the identifier, which is ideally the mod's id. </summary>
    /// <typeparam name="T">The type that the settings should be deserialized as.</typeparam>
    /// <param name="identifier">The identifier that identifies which settings should be loaded. Only letters (a-z) and digits (0-9) are allowed here.</param>
    /// <returns>The settings if an instance could be found; <see langword="null" /> otherwise.</returns>
    T? LoadSettings<T>(string identifier) where T : class;

    /// <summary> Saves the settings with the specified identifier. </summary>
    /// <param name="identifier">The identifier that identifies which settings should be loaded. Only letters (a-z) and digits (0-9) are allowed here.</param>
    /// <param name="settings">The settings that should be serialized. This class must be JSON-serializable.</param>
    void SaveSettings<T>(string identifier, T settings) where T : class;

    /// <summary> Creates a new window. </summary>
    /// <param name="identifier">The identifier for this window</param>
    /// <param name="width">Width of the window to create</param>
    /// <param name="height">Height of the window to create</param>
    /// <param name="position">Position that the window should be created at</param>
    /// <returns>The created window instance.</returns>
    Window CreateWindow(string identifier, int width, int height, Window.Position position);

    /// <summary> Populates a window with a builder. </summary>
    /// <param name="window">Window to populate.</param>
    /// <param name="closure">Callback for the builder to build the window's contents.</param>
    /// <returns>The created UI panel.</returns>
    UIPanel PopulateWindow(Window window, Action<UIPanelBuilder> closure);
}