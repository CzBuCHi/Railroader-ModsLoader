using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Railroader.ModManager.Delegates.System.Reflection.Assembly;
using Railroader.ModManager.Interfaces;
using Serilog;

namespace Railroader.ModManager.Services;

/// <summary> Manages plugin instantiation for mods. </summary>
internal interface IPluginManager
{
    IPlugin[] CreatePlugins(Mod mod);
}

/// <inheritdoc />
internal sealed class PluginManager(IModdingContext moddingContext, ILogger logger, LoadFrom loadFrom)
    : IPluginManager
{
    public PluginManager(IModdingContext moddingContext, ILogger logger) : this(moddingContext, logger, Assembly.LoadFrom) {
        
    }

    /// <inheritdoc />
    public IPlugin[] CreatePlugins(Mod mod) => PluginFactory(mod).ToArray();

    private IEnumerable<IPlugin> PluginFactory(Mod mod) {
        var assembly = loadFrom(mod.AssemblyPath!);
        if (assembly == null) {
            yield break;
        }

        foreach (var type in assembly.GetTypes()) {
            if (type.IsAbstract) {
                continue;
            }

            if (type.BaseType is not { IsGenericType: true } || type.BaseType?.GetGenericTypeDefinition() != typeof(PluginBase<>)) {
                if (typeof(IPlugin).IsAssignableFrom(type)) {
                    logger.Warning("Type {type} inherits IPluginBase but not PluginBase<> in mod {ModId}", type, mod.Definition.Identifier);
                }

                continue;
            }

            var constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null!, [typeof(IModdingContext), typeof(IMod)], null!);
            if (constructor == null) {
                logger.Warning("Cannot find constructor that accepts IModdingContext, IMod parameters on plugin {plugin} in mod {ModId}", type, mod.Definition.Identifier);
                continue;
            }

            yield return (IPlugin)constructor.Invoke([moddingContext, mod])!;
        }
    }
}
