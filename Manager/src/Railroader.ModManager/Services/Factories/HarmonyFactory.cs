using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Railroader.ModManager.Services.Wrappers;

namespace Railroader.ModManager.Services.Factories;

internal interface IHarmonyFactory
{
    IHarmonyWrapper CreateHarmony(string id);
}

[ExcludeFromCodeCoverage]
internal sealed class HarmonyFactory : IHarmonyFactory
{
    public IHarmonyWrapper CreateHarmony(string id) => new HarmonyWrapper(new Harmony(id));
}
