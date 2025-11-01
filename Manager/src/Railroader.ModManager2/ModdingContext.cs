using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Railroader.ModManager.Delegates.HarmonyLib;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Interfaces;
using Serilog;
using UI.Builder;
using UI.Common;

namespace Railroader.ModManager;

/// <summary> Implementation of <see cref="IModdingContext"/> providing basic modding services. </summary>
[method: EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ModdingContext(IReadOnlyCollection<IMod> mods, ILogger logger, HarmonyFactory harmonyFactory) : IModdingContext
{
    [ExcludeFromCodeCoverage]
    public ModdingContext(IReadOnlyCollection<IMod> mods) 
        : this(mods, Log.Logger.ForSourceContext(), Harmony.Factory) {
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IMod> Mods { get; } = mods;

    public ILogger Logger { get; } = logger;

    public HarmonyFactory HarmonyFactory { get; } = harmonyFactory;

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
