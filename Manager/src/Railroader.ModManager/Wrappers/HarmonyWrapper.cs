using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;

namespace Railroader.ModManager.Wrappers;

/// <summary> Wrapper for <see cref="Harmony"/>. </summary>
internal interface IHarmonyWrapper
{
    /// <inheritdoc cref="Harmony.PatchAll(Assembly)"/>
    void PatchAll(Assembly assembly);

    /// <inheritdoc cref="Harmony.UnpatchAll(string)"/>
    void UnpatchAll(string id);
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
internal sealed class HarmonyWrapper(string id) : IHarmonyWrapper
{
    /// <summary> Wrapped <see cref="Harmony"/> instance. </summary>
    private readonly Harmony _Harmony = new(id);

    /// <inheritdoc />
    public void PatchAll(Assembly assembly) => _Harmony.PatchAll(assembly);

    /// <inheritdoc />
    public void UnpatchAll(string id) => _Harmony.UnpatchAll(id);
}
