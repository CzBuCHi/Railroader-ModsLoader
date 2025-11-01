using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Railroader.ModManager.Features;
using UnityEngine;

namespace Railroader.ModManager;

[ExcludeFromCodeCoverage]
public static class ModManager
{
    private static bool _Injected;

    /// <remarks> This method is called by game code <see cref="Logging.LogManager"/>.</remarks>
    [PublicAPI]
    public static void Bootstrap() {
        try {
            if (_Injected) {
                return;
            }

            _Injected = true;
            Bootstrapper.Execute();
        } catch (Exception exc) {
            Debug.LogError("Failed to load ModManager ModManager!");
            Debug.LogException(exc);
        }
    }
}
