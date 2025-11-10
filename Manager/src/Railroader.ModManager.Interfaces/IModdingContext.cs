using System.Collections.Generic;
using Railroader.ModManager.Interfaces.UI;

namespace Railroader.ModManager.Interfaces;

/// <summary> An injectable interface that allows access to other mods and some quality-of-life functionality. </summary>
public interface IModdingContext
{
    /// <summary> Gets the list of all mods. This includes loaded, enabled, disabled, and failed mods. </summary>
    IReadOnlyCollection<IMod> Mods { get; }

    void RegisterWindow<TWindow>() where TWindow : ProgrammaticWindowBase;

    void OpenWindow<TWindow>() where TWindow : ProgrammaticWindowBase;

    void CloseWindow<TWindow>() where TWindow : ProgrammaticWindowBase;
}
