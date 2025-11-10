using Railroader.ModManager.HarmonyPatches;
using Railroader.ModManager.Interfaces.UI;

namespace Railroader.ModManager.Features;

public interface IWindowManager
{
    void RegisterWindow<TWindow>() where TWindow : ProgrammaticWindowBase;
    void OpenWindow<TWindow>() where TWindow : ProgrammaticWindowBase;
    void CloseWindow<TWindow>() where TWindow : ProgrammaticWindowBase;
}

public sealed class WindowManager : IWindowManager
{
    public static IWindowManager Instance = new WindowManager();

    public void RegisterWindow<TWindow>() where TWindow : ProgrammaticWindowBase => ProgrammaticWindowCreatorPatches.RegisterWindow<TWindow>();

    public void OpenWindow<TWindow>() where TWindow : ProgrammaticWindowBase => ProgrammaticWindowCreatorPatches.GetWindow<TWindow>().ShowWindow();

    public void CloseWindow<TWindow>() where TWindow : ProgrammaticWindowBase => ProgrammaticWindowCreatorPatches.GetWindow<TWindow>().CloseWindow();
}
