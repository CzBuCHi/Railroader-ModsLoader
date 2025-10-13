using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Railroader.ModInjector.Wrappers;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.ModInjector.Services;

public interface IPluginLoader
{
    IEnumerable<PluginBase> LoadPlugins(string assemblyPath, IModdingContext moddingContext, IModDefinition modDefinition);
}

public class PluginLoader(IAssemblyLoader assemblyLoader, ILogger logger) : IPluginLoader
{
    [ExcludeFromCodeCoverage]
    public PluginLoader() : this(new AssemblyLoader(), Log.ForContext("SourceContext", "Railroader.ModInjector")) {
    }

    public IEnumerable<PluginBase> LoadPlugins(string assemblyPath, IModdingContext moddingContext, IModDefinition modDefinition) {
        logger.Debug("Loading assembly from {assemblyPath} ...", assemblyPath);

        Assembly assembly;
        try {
            assembly = assemblyLoader.Load(assemblyPath);
        } catch (Exception exc) {
            logger.Error("Failed to load mod assembly from {assemblyPath}, error: {error}", assemblyPath, exc);
            yield break;
        }
      
        foreach (var type in assembly.GetTypes()) {
            logger.Debug("Checking type: {type}", type);
            if (!typeof(PluginBase).IsAssignableFrom(type) || type.IsAbstract) {
                continue;
            }

            logger.Debug("Found PluginBase-derived type: {type}", type);

            var constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null!, [typeof(IModdingContext), typeof(IModDefinition)], null!);
            if (constructor == null) {
                logger.Error("No constructor found in {type} that accepts IModdingContext and IModDefinition", type);
                continue;
            }

            object? pluginInstance;
            try {
                pluginInstance = constructor.Invoke([moddingContext, modDefinition]);
            } catch (TargetInvocationException exc) {
                logger.Error("Failed to create {type}. error: {error}", type, exc.InnerException);
                continue;
            }
            
            logger.Debug("Successfully created instance of {type}", type);
            yield return (PluginBase)pluginInstance!;
        }
    }
}
