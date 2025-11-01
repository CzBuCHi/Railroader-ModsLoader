using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Railroader.ModManager.Delegates.System.Reflection.Assembly;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Interfaces;
using Serilog;

namespace Railroader.ModManager.Features;

public delegate CreatePluginsDelegate CreatePluginsDelegateFactory(IModdingContext moddingContext);

public delegate IPlugin[] CreatePluginsDelegate(Mod mod);

public sealed class PluginManager
{
    [ExcludeFromCodeCoverage]
    public static CreatePluginsDelegate Factory(IModdingContext moddingContext) =>
        mod => CreatePlugins(moddingContext, Log.Logger.ForSourceContext(), Assembly.LoadFrom, mod);

    public static IPlugin[] CreatePlugins(IModdingContext moddingContext, ILogger logger, LoadFrom loadFrom, Mod mod) =>
        PluginFactory(moddingContext, logger, loadFrom, mod).ToArray();

    private static IEnumerable<IPlugin> PluginFactory(IModdingContext moddingContext, ILogger logger, LoadFrom loadFrom, Mod mod) {
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
