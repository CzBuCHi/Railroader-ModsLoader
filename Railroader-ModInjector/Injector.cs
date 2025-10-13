using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Railroader.ModInjector.Services;
using Railroader.ModInjector.Wrappers;
using Railroader.ModInterfaces;
using Serilog;
using ILogger = Serilog.ILogger;
using Logger = Serilog.Core.Logger;

namespace Railroader.ModInjector;

[PublicAPI]
public static class Injector
{
    internal static ILogManager      LogManager      => new LogManager();
    internal static IModLoader       ModLoader       => new ModLoader();
    internal static ICodeCompiler    CodeCompiler    => new CodeCompiler();
    internal static IPluginLoader    PluginLoader    => new PluginLoader();
    internal static IHarmonyExporter HarmonyExporter => new HarmonyExporter();

    private static  ILogger          _Logger         = null!;
    private static  IModDefinition[] _ModDefinitions = null!;
    private static  Mod[]            _Mods           = null!;

    /// <summary> Injector 'main' function. </summary>
    public static void ModInjectorMain() {
        _Logger = Log.ForContext("SourceContext", "Railroader.ModInjector");
        _Logger.Debug("ModInjectorMain start");

        _Mods = new Mod[_ModDefinitions.Length];

        for (var i = 0; i < _ModDefinitions.Length; i++) {
            var definition = _ModDefinitions[i];
            var outputDllPath = CodeCompiler.CompileMod(definition);
            _Mods[i] = new Mod(definition, outputDllPath);
        }

        var moddingContext = new ModdingContext(_Mods);

        _Logger.Debug("moddingContext: {moddingContext}", JsonConvert.SerializeObject(moddingContext));

        foreach (var mod in _Mods.Where(o => o.OutputDllPath != null)) {
            mod.Plugins = PluginLoader.LoadPlugins(mod.OutputDllPath!, moddingContext, mod.Definition).ToArray();
            mod.IsLoaded = true;
        }

        foreach (var mod in _Mods) {
            _Logger.Debug("enabling mod {id}", mod.Definition.Id);
            mod.IsEnabled = true;
        }

        _Logger.Information("Applying harmony patches ...");
        var harmony = new HarmonyLib.Harmony("Railroader.ModInjector");
        harmony.PatchAll(typeof(Injector).Assembly);

        // todo: config: ExportPatched = true

        //_Logger.Information("Exporting patched Assembly-CSharp ...");
        //var assemblyCSharp = Path.Combine(Environment.CurrentDirectory, "Railroader_Data", "Managed", "Assembly-CSharp");

        //HarmonyExporter.ExportPatchedAssembly(assemblyCSharp + ".dll", harmony, assemblyCSharp + "_Patched.dll");
        
        _Logger.Debug("ModInjectorMain end");
    }

    /// <summary> Serilog configuration. </summary>
    public static Logger CreateLogger(LoggerConfiguration configuration) {
        _ModDefinitions = ModLoader.LoadModDefinitions();

        LogManager.ConfigureLogger(configuration, _ModDefinitions);

        var logger = configuration.CreateLogger()!;

        //report messages from ModLoader
        ModLoader.ProcessLogMessages(logger);

        foreach (var modDefinition in _ModDefinitions.Where(o => o.LogLevel != null)) {
            logger.ForContext(typeof(Injector))!.Information("Log level for {mod} set to {level}", modDefinition.Id, modDefinition.LogLevel);
        }

        return logger;
    }
}