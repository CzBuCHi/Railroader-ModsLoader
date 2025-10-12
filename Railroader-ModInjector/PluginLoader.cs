using System.Collections.Generic;
using System.Reflection;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.ModInjector;

public interface IPluginLoader
{
    IEnumerable<PluginBase> LoadPlugins(string outputDllPath, IModdingContext moddingContext);
}

public class PluginLoader : IPluginLoader
{
    private readonly ILogger _Logger = ModLogger.ForContext(typeof(PluginLoader));

    public IEnumerable<PluginBase> LoadPlugins(string outputDllPath, IModdingContext moddingContext) {
        _Logger.Information("Loading assembly from {outputDllPath} ...", outputDllPath);

        var assembly = Assembly.LoadFrom(outputDllPath);
        foreach (var type in assembly.GetTypes()) {
            _Logger.Debug("Checking type: {type}", type);
            if (!typeof(PluginBase).IsAssignableFrom(type) || type.IsAbstract) {
                continue;
            }

            _Logger.Debug("Found PluginBase-derived type: {type}", type);

            var constructor = type.GetConstructor([typeof(IModdingContext)]);
            if (constructor == null) {
                _Logger.Error("No constructor found in {type} that accepts IModdingContext", type);
                continue;
            }

            var pluginInstance = constructor.Invoke([moddingContext]);
            if (pluginInstance is not PluginBase plugin) {
                _Logger.Error("Failed to cast {type} to PluginBase", type);
                continue;
            }

            _Logger.Debug("Successfully created instance of {type}", type);
            yield return plugin;
        }
    }

}
