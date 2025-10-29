using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace Railroader.ModManager.Services.Wrappers;

/// <summary> Wrapper for <see cref="Harmony"/>. </summary>
[PublicAPI]
internal interface IHarmonyWrapper
{
    string Id { get; }
    void PatchAll();
    void PatchAll(Assembly assembly);
    PatchProcessor CreateProcessor(MethodBase original);
    PatchClassProcessor CreateClassProcessor(Type type);
    ReversePatcher CreateReversePatcher(MethodBase original, HarmonyMethod standin);
    void PatchAllUncategorized();
    void PatchAllUncategorized(Assembly assembly);
    void PatchCategory(string category);
    void PatchCategory(Assembly assembly, string category);
    MethodInfo Patch(MethodBase original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, HarmonyMethod? transpiler = null, HarmonyMethod? finalizer = null);
    void UnpatchAll(string? harmonyID = null);
    void Unpatch(MethodBase original, HarmonyPatchType type, string harmonyID = "*");
    void Unpatch(MethodBase original, MethodInfo patch);
    void UnpatchCategory(string category);
    void UnpatchCategory(Assembly assembly, string category);
    IEnumerable<MethodBase> GetPatchedMethods();
}

[ExcludeFromCodeCoverage]
internal sealed class HarmonyWrapper(Harmony harmony) : IHarmonyWrapper
{
    public string Id => harmony.Id;
    public void PatchAll() => harmony.PatchAll();
    public void PatchAll(Assembly assembly) => harmony.PatchAll(assembly);
    public PatchProcessor CreateProcessor(MethodBase original) => harmony.CreateProcessor(original);
    public PatchClassProcessor CreateClassProcessor(Type type) => harmony.CreateClassProcessor(type);
    public ReversePatcher CreateReversePatcher(MethodBase original, HarmonyMethod standin) => harmony.CreateReversePatcher(original, standin);
    public void PatchAllUncategorized() => harmony.PatchAllUncategorized();
    public void PatchAllUncategorized(Assembly assembly) => harmony.PatchAllUncategorized(assembly);
    public void PatchCategory(string category) => harmony.PatchCategory(category);
    public void PatchCategory(Assembly assembly, string category) => harmony.PatchCategory(assembly, category);
    public MethodInfo Patch(MethodBase original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, HarmonyMethod? transpiler = null, HarmonyMethod? finalizer = null) => harmony.Patch(original, prefix, postfix, transpiler, finalizer);
    public void UnpatchAll(string? harmonyID = null) => harmony.UnpatchAll(harmonyID);
    public void Unpatch(MethodBase original, HarmonyPatchType type, string harmonyID = "*") => harmony.Unpatch(original, type, harmonyID);
    public void Unpatch(MethodBase original, MethodInfo patch) => harmony.Unpatch(original, patch);
    public void UnpatchCategory(string category) => harmony.UnpatchCategory(category);
    public void UnpatchCategory(Assembly assembly, string category) => harmony.UnpatchCategory(assembly, category);
    public IEnumerable<MethodBase> GetPatchedMethods() => harmony.GetPatchedMethods();
}
