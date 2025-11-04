using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HeathenEngineering.SteamworksIntegration.API;
using Railroader.ModManager.Features;
using UnityEngine;

namespace Railroader.ModManager.Behaviors;

/// <summary>
///     Ensures mod manager core initialization runs exactly once after Steamworks becomes available,
///     surviving Unity's double-startup crash caused by premature Steam API access.
/// </summary>
/// <remarks>
///     <para>
///         This MonoBehaviour is created twice due to Unity's startup behavior:
///         <list type="bullet">
///             <item>
///                 <description>
///                     <b>First thread</b>: Game starts, <c>Compile()</c> creates the bootstrapper.
///                     <c>HeathenEngineering.SteamworksIntegration.API.Utilities.Client.IpCountry</c> throws
///                     "Steamworks is not initialized." Unity crashes and restarts in a new thread.
///                     The GameObject is destroyed before <c>_Success</c> is set.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <b>Second thread</b>: Game restarts, <c>Compile()</c> is called again → new GameObject.
///                     The coroutine begins polling <c>IpCountry</c> every second. It fails initially, waits,
///                     then eventually succeeds when Steamworks initializes. <c>OnDestroy</c> is triggered,
///                     and <c>Bootstrapper.ExecuteCore()</c> runs exactly once.
///                 </description>
///             </item>
///         </list>
///     </para>
///     <para>
///         This pattern guarantees:
///         <list type="bullet">
///             <item>Core mod logic executes <b>only once</b> (on the successful thread)</item>
///             <item>No duplicate execution (first instance dies early)</item>
///             <item>Crash-resilient via <c>DontDestroyOnLoad</c> and polling</item>
///             <item>Non-blocking via <c>WaitForSecondsRealtime</c> in coroutine</item>
///         </list>
///     </para>
///     <para>
///         The polling loop is essential: Steamworks may take several seconds to initialize
///         after the second thread starts. This bootstrapper acts as a reliable gatekeeper
///         for all Steam-dependent mod functionality.
///     </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ManagerBootstrapperBehaviour : MonoBehaviour {
    private bool _Success;

    private void Awake() {
        DontDestroyOnLoad(transform.gameObject);
        StartCoroutine(Coroutine());
    }

    private void OnDestroy() {
        if (_Success) {
            Bootstrapper.ExecuteCore();
        }
    }

    private IEnumerator Coroutine() {
        do {
            try {
                _        = Utilities.Client.IpCountry;
                _Success = true;
            } catch {
                _Success = false;
            }

            if (!_Success) {
                yield return new WaitForSecondsRealtime(1);
            }
        } while (!_Success);

        Destroy(this);
    }
}