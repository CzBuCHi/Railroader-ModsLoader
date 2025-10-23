using System.Collections.Generic;
using System.Reflection;
using Railroader.ModInjector.Wrappers;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.ModInjector.Services;

/// <summary> Manages plugin instantiation for mods. </summary>
internal interface IPluginManager
{
    IEnumerable<IPluginBase> CreatePlugins(Mod mod);
}

/// <inheritdoc />
internal sealed class PluginManager : IPluginManager
{
    public required IAssemblyWrapper AssemblyWrapper { get; init; }
    public required IModdingContext  ModdingContext  { get; init; }
    public required ILogger          Logger          { get; init; }

    /// <inheritdoc />
    public IEnumerable<IPluginBase> CreatePlugins(Mod mod) {
        var assembly = AssemblyWrapper.LoadFrom(mod.AssemblyPath!);
        if (assembly == null) {
            yield break;
        }

        foreach (var type in assembly.GetTypes()) {
            if (type.IsAbstract) {
                continue;
            }

            if (type.BaseType is not { IsGenericType: true } || type.BaseType?.GetGenericTypeDefinition() != typeof(PluginBase<>)) {
                if (typeof(IPluginBase).IsAssignableFrom(type)) {
                    Logger.Warning("Type {type} inherits IPluginBase but not PluginBase<> in mod {ModId}", type, mod.Definition.Identifier);
                }

                continue;
            }

            var constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null!, [typeof(IModdingContext), typeof(IMod)], null!);
            if (constructor == null) {
                Logger.Warning("Cannot find constructor that accepts IModdingContext, IMod parameters on plugin {plugin} in mod {ModId}", type, mod.Definition.Identifier);
                continue;
            }

            yield return (IPluginBase)constructor.Invoke([ModdingContext, mod])!;
        }
    }
}
