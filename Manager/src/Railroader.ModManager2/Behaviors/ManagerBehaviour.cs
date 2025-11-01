using System.Diagnostics.CodeAnalysis;
using Railroader.ModManager.Features;
using UnityEngine;

namespace Railroader.ModManager.Behaviors;

[ExcludeFromCodeCoverage]
public class ManagerBehaviour : MonoBehaviour
{
    private static ManagerBehaviour? _Instance;

    private void Awake() {
        if (_Instance != null) {
            return;
        }

        _Instance = this;
        DontDestroyOnLoad(transform.gameObject);
        Bootstrapper.LoadMods();
    }

    private void OnDestroy() {
        if (_Instance == this) {
            _Instance = null;
        }
    }
}
