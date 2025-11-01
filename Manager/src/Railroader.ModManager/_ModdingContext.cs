using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Railroader.ModManager.Interfaces;
using Serilog;
using UI.Builder;
using UI.Common;

namespace Railroader.ModManager;

/// <summary> Implementation of <see cref="IModdingContext"/> providing basic modding services. </summary>
public sealed class ModdingContext(IReadOnlyCollection<IMod> mods, ILogger logger) : IModdingContext
{
    /// <inheritdoc />
    public IReadOnlyCollection<IMod> Mods { get; } = mods;

    public ILogger Logger { get; } = logger;

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public T LoadSettings<T>(string identifier) where T : class => throw new NotImplementedException();

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public void SaveSettings<T>(string identifier, T settings) where T : class => throw new NotImplementedException();

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public Window CreateWindow(string identifier, int width, int height, Window.Position position) => throw new NotImplementedException();

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public UIPanel PopulateWindow(Window window, Action<UIPanelBuilder> closure) => throw new NotImplementedException();
}
