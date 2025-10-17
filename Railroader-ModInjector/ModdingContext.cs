using System;
using System.Collections.Generic;
using Railroader.ModInterfaces;
using UI.Builder;
using UI.Common;

namespace Railroader.ModInjector;

/// <summary> Implementation of <see cref="IModdingContext"/> providing basic modding services. </summary>
internal sealed class ModdingContext(IReadOnlyCollection<IMod> mods) : IModdingContext
{
    /// <inheritdoc />
    public IReadOnlyCollection<IMod> Mods { get; } = mods;

    /// <inheritdoc />
    public T? LoadSettings<T>(string identifier) where T : class => throw new NotImplementedException();

    /// <inheritdoc />
    public void SaveSettings<T>(string identifier, T settings) where T : class => throw new NotImplementedException();

    /// <inheritdoc />
    public Window CreateWindow(string identifier, int width, int height, Window.Position position) => throw new NotImplementedException();

    /// <inheritdoc />
    public UIPanel PopulateWindow(Window window, Action<UIPanelBuilder> closure) => throw new NotImplementedException();
}

