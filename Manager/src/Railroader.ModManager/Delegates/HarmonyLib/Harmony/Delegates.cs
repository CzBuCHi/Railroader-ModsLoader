using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;
using _Harmony = HarmonyLib.Harmony;

namespace Railroader.ModManager.Delegates.HarmonyLib.Harmony;

/// <inheritdoc cref="_Harmony(string)"/>
/// <remarks> Wraps <see cref="_Harmony(string)"/> for testability. </remarks>
internal delegate IHarmony HarmonyFactory(string path);

/// <summary> Wrapper for <see cref="_Harmony"/>. </summary>
[PublicAPI]
internal interface IHarmony
{
    void PatchAll(Assembly assembly);
    void PatchAllUncategorized(Assembly assembly);
    void PatchCategory(Assembly assembly, string category);
    void UnpatchAll(string? harmonyID = null);
}

[ExcludeFromCodeCoverage]
internal sealed class Harmony(_Harmony harmony) : IHarmony
{
    // TODO: not mocked
    public static HarmonyFactory Create => o => new Harmony(new _Harmony(o));

    public void PatchAll(Assembly assembly) => harmony.PatchAll(assembly);
    public void PatchAllUncategorized(Assembly assembly) => harmony.PatchAllUncategorized(assembly);
    public void PatchCategory(Assembly assembly, string category) => harmony.PatchCategory(assembly, category);
    public void UnpatchAll(string? harmonyID = null) => harmony.UnpatchAll(harmonyID);
}
