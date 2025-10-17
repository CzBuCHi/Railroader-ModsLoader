using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Railroader.ModInjector.PluginWrappers;
using Railroader.ModInterfaces;

namespace Railroader.ModInjector.Services;

/// <summary> Manages plugin creation and instantiation for mods. </summary>
internal interface IPluginManager
{
    IEnumerable<PluginBase> CreatePlugins(Mod mod);
}

/// <inheritdoc />
internal sealed class PluginManager(ModdingContext moddingContext) : IPluginManager
{
    /// <inheritdoc />
    public IEnumerable<PluginBase> CreatePlugins(Mod mod) {
        Assembly assembly;
        try {
            assembly = Assembly.LoadFrom(mod.OutputDllPath!);
        } catch (Exception) {
            yield break;
        }
        
        // create instances ... 
        foreach (var type in assembly.GetTypes()) {
            if (!typeof(PluginBase).IsAssignableFrom(type) || type.IsAbstract) {
                continue;
            }

            var constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null!, [typeof(IModdingContext), typeof(IModDefinition)], null!);
            if (constructor == null) {
                continue;
            }

            yield return (PluginBase)constructor.Invoke([moddingContext, mod.Definition])!;
        }
    }
}
