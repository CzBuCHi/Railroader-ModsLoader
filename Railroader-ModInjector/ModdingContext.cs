using System;
using System.Collections.Generic;
using Railroader.ModInterfaces;
using UI.Builder;
using UI.Common;

namespace Railroader.ModInjector;

internal sealed class ModdingContext(Mod[] mods) : IModdingContext
{
    public IReadOnlyCollection<IMod> Mods => mods;

    public string GamePath { get; set; } = null!;

    public T? LoadSettings<T>(string identifier) where T : class => throw new NotImplementedException();

    public void SaveSettings<T>(string identifier, T settings) where T : class {
        throw new NotImplementedException();
    }

    public Window CreateWindow(string identifier, int width, int height, Window.Position position) => throw new NotImplementedException();

    public UIPanel PopulateWindow(Window window, Action<UIPanelBuilder> closure) => throw new NotImplementedException();
}
