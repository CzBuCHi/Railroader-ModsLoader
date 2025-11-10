using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Interfaces.UI;
using Serilog;
using UI;

namespace Railroader.ModManager.HarmonyPatches;

[PublicAPI]
[HarmonyPatch]
public static class ProgrammaticWindowCreatorPatches
{
    private static MethodInfo               _CreateWindow;
    private static Dictionary<Type, object> _RegisteredWindows = new();
    private static bool                     _Started;

    static ProgrammaticWindowCreatorPatches() {
        var methodInfo =
            typeof(ProgrammaticWindowCreator)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(o => o.IsGenericMethod && o.Name == "CreateWindow" && o.GetParameters().Length == 1);

        if (methodInfo == null) {
            throw new InvalidOperationException("Cannot find method UI.ProgrammaticWindowCreator:CreateWindow<TWindow>(Action<>).");
        }

        _CreateWindow = methodInfo;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ProgrammaticWindowCreator), "Start")]
    [ExcludeFromCodeCoverage]
    public static void Start(ProgrammaticWindowCreator __instance) {
        _Started = true;
        foreach (var pair in _RegisteredWindows) {
            _CreateWindow.MakeGenericMethod(pair.Key).Invoke(__instance, [pair.Value]);
        }
    }

    public static void RegisterWindow<TWindow>() where TWindow : ProgrammaticWindowBase {
        if (_Started) {
            throw new InvalidOperationException("Cannot register window: Game already started.");
        }

        var type = typeof(TWindow);
        Log.Logger.ForSourceContext().Information("RegisterWindow: {windowType}", type);

        _RegisteredWindows[type] = (Action<TWindow>)Handler;
        return;

        void Handler(TWindow window) => _RegisteredWindows[type] = window;
    }

    public static TWindow GetWindow<TWindow>() where TWindow : ProgrammaticWindowBase {
        var type = typeof(TWindow);
        _RegisteredWindows.TryGetValue(type, out var instance);
        return instance as TWindow ?? throw new InvalidOperationException($"Cannot find window {type}.");
    }
}
