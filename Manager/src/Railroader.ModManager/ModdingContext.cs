using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Railroader.ModManager.Interfaces;
using UI.Builder;
using UI.Common;

namespace Railroader.ModManager;

/// <summary> Implementation of <see cref="IModdingContext"/> providing basic modding services. </summary>
internal sealed class ModdingContext(IReadOnlyCollection<IMod> mods) : IModdingContext
{
    /// <inheritdoc />
    public IReadOnlyCollection<IMod> Mods { get; } = mods;

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public T? LoadSettings<T>(string identifier) where T : class => throw new NotImplementedException();

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
