using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Railroader.ModInterfaces;
using Serilog;
using ILogger = Serilog.ILogger;
using Logger = Serilog.Core.Logger;

namespace Railroader.ModInjector;

[PublicAPI]
public static class Injector
{
    internal static IModLoader    ModLoader    => new ModLoader(new FileSystem());
    internal static ICodeCompiler CodeCompiler => new CodeCompiler();
    internal static IPluginLoader PluginLoader => new PluginLoader();

    private static  ILogger          _Logger         = null!;
    private static  IModDefinition[] _ModDefinitions = null!;
    private static  Mod[]            _Mods           = null!;

    /// <summary> Injector 'main' function. </summary>
    public static void ModInjectorMain() {
        _Logger = ModLogger.ForContext(typeof(Injector))!;
        _Logger.Information("ModInjectorMain");

     

        _Mods = new Mod[_ModDefinitions.Length];

        for (var i = 0; i < _ModDefinitions.Length; i++) {
            var definition = _ModDefinitions[i];
            _Logger.Information("definition: {definition}", definition.Id);
            var outputDllPath = CodeCompiler.CompileMod(definition);

            _Logger.Information("DLL: {outputDllPath}", outputDllPath);
            _Mods[i] = new Mod(definition, outputDllPath);
        }

        var moddingContext = new ModdingContext(_Mods);

        _Logger.Information("moddingContext: {moddingContext}", moddingContext);

        foreach (var mod in _Mods.Where(o => o.OutputDllPath != null)) {

            _Logger.Information("mod: {id}, DLL: {outputDllPath}", mod.Definition.Id, mod.OutputDllPath);

            mod.Plugins = PluginLoader.LoadPlugins(mod.OutputDllPath!, moddingContext).ToArray();

            _Logger.Information("mod: {id}, plugins: {plugins} ", mod.Definition.Id, mod.Plugins?.Length);
            if (mod.Plugins != null) {
                foreach (var plugin in mod.Plugins) {
                    _Logger.Information("mod: {id}, plugin: {plugin} ", mod.Definition.Id, plugin.GetType().FullName);
                }
            }

            mod.IsLoaded = true;
        }

        foreach (var mod in _Mods) {
            _Logger.Information("enabling mod {id}", mod.Definition.Id);
            mod.IsEnabled = true;
        }

        _Logger.Information("ModInjectorMain done");
    }

    /// <summary> Serilog configuration. </summary>
    public static Logger CreateLogger(LoggerConfiguration configuration) {
        _ModDefinitions = ModLoader.LoadModDefinitions();

        foreach (var modDefinition in _ModDefinitions.Where(o => o.LogLevel != null)) {
            configuration.MinimumLevel!.Override(modDefinition.Id, modDefinition.LogLevel!.Value);
        }

        var logger = configuration.CreateLogger()!;
        ModLoader.ProcessLogMessages(logger);

        foreach (var modDefinition in _ModDefinitions.Where(o => o.LogLevel != null)) {
            logger.Information("Log level for {mod} set to {level}", modDefinition.Id, modDefinition.LogLevel);
        }

        return logger;
    }
}

