using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Railroader.ModManager;

public class ModManager : MonoBehaviour
{
    /// <remarks> This method is called by static constructor of <see cref="Logging.LogManager"/>.</remarks>
    [PublicAPI]
    public static void Bootstrap() {
        if (_Injected) {
            return;
        }

        _Injected = true;

        try {

        } catch (Exception exc) {
            Debug.LogError("Failed to load ModManager!");
            Debug.LogException(exc);
        }
    }

    private static bool        _Injected;
    private static GameObject? _GameObject;
}
