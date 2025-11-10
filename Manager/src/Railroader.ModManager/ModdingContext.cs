using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Railroader.ModManager.Delegates.HarmonyLib;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Interfaces;
using Railroader.ModManager.Interfaces.UI;
using Serilog;

namespace Railroader.ModManager;

/// <summary> Implementation of <see cref="IModdingContext" /> providing basic modding services. </summary>
[method: EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ModdingContext(IReadOnlyCollection<IMod> mods, ILogger logger, HarmonyFactory harmonyFactory)
    : IModdingContext
{
    [ExcludeFromCodeCoverage]
    public ModdingContext(IReadOnlyCollection<IMod> mods)
        : this(mods, Log.Logger.ForSourceContext(), HarmonyWrapper.Factory) {
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IMod> Mods { get; } = mods;

    public ILogger Logger { get; } = logger;

    public HarmonyFactory HarmonyFactory { get; } = harmonyFactory;

    public void RegisterWindow<TWindow>() where TWindow : ProgrammaticWindowBase => Features.WindowManager.Instance.RegisterWindow<TWindow>();

    public void OpenWindow<TWindow>() where TWindow : ProgrammaticWindowBase => Features.WindowManager.Instance.OpenWindow<TWindow>();

    public void CloseWindow<TWindow>() where TWindow : ProgrammaticWindowBase => Features.WindowManager.Instance.CloseWindow<TWindow>();
}
