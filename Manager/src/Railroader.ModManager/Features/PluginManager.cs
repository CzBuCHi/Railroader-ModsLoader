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

/// <summary>
///     Creates a reusable plugin loader for a modding context.
/// </summary>
internal delegate PluginLoader PluginLoaderFactory(IModdingContext moddingContext);

/// <summary>
///     Loads all plugins from a <see cref="Mod" /> using the pre-configured context and logger.
/// </summary>
/// <param name="mod">The mod to load plugins from.</param>
/// <returns>An array of instantiated <see cref="IPlugin" /> instances.</returns>
internal delegate IPlugin[] PluginLoader(Mod mod);

/// <summary>
///     Manages discovery and instantiation of mod plugins via reflection.
/// </summary>
internal static class PluginManager {
    /// <summary>
    ///     Creates a high-performance, reusable plugin loader for the given modding context.
    /// </summary>
    /// <param name="moddingContext">The modding context to inject into plugin constructors.</param>
    /// <returns>
    ///     A <see cref="PluginLoader" /> delegate that can be reused to load plugins from multiple mods
    ///     without reallocating logger or context.
    /// </returns>
    [ExcludeFromCodeCoverage]
    public static PluginLoader CreateLoader(IModdingContext moddingContext) =>
        mod => LoadPlugins(moddingContext, Log.Logger.ForSourceContext(), Assembly.LoadFrom, mod);

    /// <summary>
    ///     Loads and instantiates all valid plugins from the given mod.
    /// </summary>
    /// <param name="moddingContext">The modding context to pass to plugin constructors.</param>
    /// <param name="logger">The logger instance (typically scoped to the mod).</param>
    /// <param name="loadFrom">Delegate used to load the assembly from path (e.g. <c>Assembly.LoadFrom</c>).</param>
    /// <param name="mod">The mod containing the plugin assembly.</param>
    /// <returns>An array of instantiated plugins implementing <see cref="IPlugin" />.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if <c>mod.AssemblyPath</c> is <c>null</c> — violates mod contract.
    /// </exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IPlugin[] LoadPlugins(IModdingContext moddingContext, ILogger logger, LoadFrom loadFrom, Mod mod) =>
        InstantiatePlugins(moddingContext, logger, loadFrom, mod).ToArray();

    /// <summary>
    ///     Scans the mod assembly and instantiates all valid plugin types.
    /// </summary>
    /// <param name="moddingContext">Context injected into plugin constructors.</param>
    /// <param name="logger">Logger for warnings (e.g. missing ctor, wrong base class).</param>
    /// <param name="loadFrom">Assembly loading strategy.</param>
    /// <param name="mod">Source mod.</param>
    /// <returns>
    ///     A sequence of instantiated <see cref="IPlugin" /> objects. Empty if assembly fails to load.
    /// </returns>
    private static IEnumerable<IPlugin> InstantiatePlugins(IModdingContext moddingContext, ILogger logger, LoadFrom loadFrom, Mod mod) {
        var assemblyPath = mod.AssemblyPath ?? throw new InvalidOperationException("Mod contract violation: AssemblyPath is null.");

        var assembly = loadFrom(assemblyPath);
        if (assembly == null) {
            logger.Warning("Failed to load assembly from path: {AssemblyPath} for mod {ModId}",
                           assemblyPath, mod.Definition.Identifier);
            yield break;
        }

        var pluginBaseType = typeof(PluginBase<>);
        var types          = assembly.GetTypes();
        foreach (var type in types) {
            if (type.IsAbstract) {
                continue;
            }

            if (type.BaseType is not { IsGenericType: true } baseType ||
                baseType.GetGenericTypeDefinition() != pluginBaseType) {
                if (typeof(IPlugin).IsAssignableFrom(type)) {
                    logger.Warning(
                        "Type {Type} implements IPlugin but does not inherit from PluginBase<> in mod {ModId}",
                        type.FullName, mod.Definition.Identifier);
                }

                continue;
            }

            var ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null!, [typeof(IModdingContext), typeof(IMod)], null!);
            if (ctor == null) {
                logger.Warning(
                    "Cannot find constructor (IModdingContext, IMod) on plugin {Plugin} in mod {ModId}",
                    type.FullName, mod.Definition.Identifier);
                continue;
            }

            yield return (IPlugin)ctor.Invoke([moddingContext, mod])!;
        }
    }
}