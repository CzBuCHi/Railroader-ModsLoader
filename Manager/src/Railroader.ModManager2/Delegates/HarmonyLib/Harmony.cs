using System.Reflection;
using _Harmony = HarmonyLib.Harmony;

namespace Railroader.ModManager.Delegates.HarmonyLib;

internal delegate IHarmony HarmonyFactory(string id);

internal interface IHarmony
{
    void PatchAll(Assembly assembly);

    void PatchCategory(Assembly assembly, string category);

    void UnpatchCategory(Assembly assembly, string category);

    void PatchAllUncategorized(Assembly assembly);

    void UnpatchAll(string id);
}

internal sealed class Harmony(string id) : IHarmony
{
    internal static IHarmony Factory(string id) => new Harmony(id);

    private readonly _Harmony _Harmony = new(id);

    public void PatchAll(Assembly assembly) => _Harmony.PatchAll(assembly);

    public void PatchCategory(Assembly assembly, string category) => _Harmony.PatchCategory(assembly, category);

    public void UnpatchCategory(Assembly assembly, string category) => _Harmony.UnpatchCategory(assembly, category);

    public void PatchAllUncategorized(Assembly assembly) => _Harmony.PatchAllUncategorized(assembly);

    public void UnpatchAll(string id) => _Harmony.UnpatchAll(id);
}
