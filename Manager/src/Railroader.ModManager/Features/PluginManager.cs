using System;
using System.Collections.Generic;
using System.ComponentModel;
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

public static class PluginManager
{
    [ExcludeFromCodeCoverage]
    public static CreatePluginsDelegate Factory(IModdingContext moddingContext) =>
        mod => CreatePlugins(moddingContext, mod, Log.Logger.ForSourceContext(), Assembly.LoadFrom);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IPlugin[] CreatePlugins(IModdingContext moddingContext, Mod mod, ILogger logger, LoadFrom loadFrom) =>
        new CreatePluginsContext(moddingContext, logger, loadFrom, mod).GetModAssemblyTypes()
                                                                       .PluginBaseDerivedOnly()
                                                                       .WithCorrectConstructor()
                                                                       .CreatePlugins()
                                                                       .ToArray();

    private record CreatePluginsContext(IModdingContext ModdingContext, ILogger Logger, LoadFrom LoadFrom, Mod Mod)
    {
        public Type            Type        { get; init; } = null!;
        public ConstructorInfo Constructor { get; init; } = null!;
    }

    private static IEnumerable<CreatePluginsContext> GetModAssemblyTypes(this CreatePluginsContext context) {
        var assembly = context.LoadFrom(context.Mod.AssemblyPath!);
        return assembly is null
            ? []
            : assembly.GetTypes().Where(o => !o.IsAbstract).Select(o => context with { Type = o });
    }

    private static IEnumerable<CreatePluginsContext> PluginBaseDerivedOnly(
        this IEnumerable<CreatePluginsContext> contexts
    ) =>
        contexts.Where(context => {
            if (context.Type.BaseType is { IsGenericType: true } baseGeneric &&
                baseGeneric.GetGenericTypeDefinition() == typeof(PluginBase<>)) {
                return true;
            }

            if (!typeof(IPlugin).IsAssignableFrom(context.Type)) {
                return false;
            }

            context.Logger.Warning("Type {type} inherits IPluginBase but not PluginBase<> in mod {ModId}", context.Type,
                context.Mod.Definition.Identifier);
            return false;
        });

    private static IEnumerable<CreatePluginsContext> WithCorrectConstructor(
        this IEnumerable<CreatePluginsContext> contexts
    ) =>
        contexts.Select(context => {
                    var ctor = context.Type.GetConstructor(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                        null!, [typeof(IModdingContext), typeof(IMod)], null!);

                    if (ctor is null) {
                        context.Logger.Warning(
                            "Cannot find constructor that accepts IModdingContext, IMod parameters on plugin {plugin} in mod {ModId}",
                            context.Type, context.Mod.Definition.Identifier);
                    }

                    return context with { Constructor = ctor! };
                })
                .Where(o => o.Constructor != null!);

    private static IEnumerable<IPlugin> CreatePlugins(this IEnumerable<CreatePluginsContext> contexts) =>
        contexts.Select(context => {
                    try {
                        return (IPlugin)context.Constructor.Invoke([context.ModdingContext, context.Mod])!;
                    } catch (Exception ex) {
                        context.Logger.Warning(ex, "Failed to instantiate plugin {Plugin} in mod {ModId}",
                            context.Constructor.DeclaringType?.FullName, context.Mod.Definition.Identifier);
                        return null;
                    }
                })
                .Where(o => o != null)
                .Cast<IPlugin>();
}
