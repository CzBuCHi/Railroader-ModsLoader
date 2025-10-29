using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Railroader.ModManager.Extensions;
using Railroader.ModManager.Interfaces;
using Railroader.ModManager.Services;
using Railroader.ModManager.Services.Factories;
using Serilog;
using UnityEngine;
using ILogger = Serilog.ILogger;

namespace Railroader.ModManager;

[ExcludeFromCodeCoverage]
public class ModManager : MonoBehaviour
{
    private static bool        _Injected;
    private static GameObject? _GameObject;

    /// <remarks> This method is called by game code <see cref="Logging.LogManager"/>.</remarks>
    [PublicAPI]
    public static void Bootstrap() {
        try {
            if (_GameObject != null || _Injected) {
                return;
            }

            _Injected = true;
            _ServiceManager = BuildServiceProvider();

            ServiceProvider.GetService<IModExtractor>().ExtractMods();

            var modDefinitionLoader = ServiceProvider.GetService<IModDefinitionLoader>();
            modDefinitionLoader.LoadDefinitions();
            var settings = _ServiceManager.GetService<LoggerSettings>();

            foreach (var definition in modDefinitionLoader.ModDefinitions) {
                if (definition.LogLevel != null) {
                    settings.ModsLogLevels[definition.Identifier] = definition.LogLevel.Value;
                }
            }

            ServiceProvider.GetService<IHarmonyFactory>().CreateHarmony("Railroader.ModManager").PatchCategory(typeof(ModManager).Assembly, "LogManager");

            _GameObject = new GameObject("ModManager");
            _GameObject.SetActive(false);
            _GameObject.AddComponent<ModManager>();
            _GameObject.SetActive(true);
        } catch (Exception exc) {
            Debug.LogError("Failed to load ModManager ModManager!");
            Debug.LogException(exc);
        }
    }

    private static ServiceManager    _ServiceManager = null!;
    private static IServiceProvider? _ServiceProvider;

    public static IServiceProvider ServiceProvider {
        get => _ServiceProvider ?? _ServiceManager;
        internal set => _ServiceProvider = value;
    }

    private static ServiceManager BuildServiceProvider() {
        var serviceManager = new ServiceManager();

        // initial logger
        serviceManager.AddSingleton<IMemoryLogger, MemoryLogger>();

        // data
        serviceManager.AddSingleton<LoggerSettings, LoggerSettings>(_ => new LoggerSettings());

        // factories
        serviceManager.AddSingleton<IHarmonyFactory, HarmonyFactory>();
        serviceManager.AddTransient<IPluginManagerFactory, PluginManagerFactory>(o => new PluginManagerFactory(o.GetService<ILoggerFactory>().GetLogger()));

        // services
        serviceManager.AddTransient<IModExtractor, ModExtractor>(o => new ModExtractor(o.GetService<IMemoryLogger>()));
        serviceManager.AddSingleton<IModDefinitionLoader, ModDefinitionLoader>(o => new ModDefinitionLoader(o.GetService<IMemoryLogger>()));
        serviceManager.AddTransient<IModDefinitionProcessor, ModDefinitionProcessor>(o => new ModDefinitionProcessor(o.GetService<ILoggerFactory>().GetLogger()));
        serviceManager.AddSingleton<CompileAssemblyDelegate, CompileAssemblyDelegate>(o => CompileAssemblyCore.CompileAssembly(o.GetService<ILoggerFactory>().GetLogger()));
        serviceManager.AddTransient<ICodeCompiler, CodeCompiler>(o => new CodeCompiler(o.GetService<ILoggerFactory>().GetLogger()            ));
        serviceManager.AddTransient<ICodePatcher, CodePatcher>(o => new CodePatcher(o.GetService<ILoggerFactory>().GetLogger()));

        return serviceManager;
    }

    internal static void ConfigureLogger() {
        var memoryLogger = (MemoryLogger)ServiceProvider.GetService<ILogger>();
        memoryLogger.Information("Finalizing logger configuration");

        var logger = Log.Logger!;
        ((ServiceManager)ServiceProvider).AddSingleton<ILoggerFactory, LoggerFactory>(_ => new LoggerFactory(logger));

        memoryLogger.Flush(logger);

        logger.Information("Logger configured successfully");

        LoadMods();
    }

    private static ModManager? _Instance;

    private void Awake() {
        if (_Instance != null) {
            return;
        }

        _Instance = this;
        DontDestroyOnLoad(transform.gameObject);
    }

    private void OnDestroy() {
        if (_Instance == this) {
            _Instance = null;
        }
    }

    private static void LoadMods() {
        var definitions = ServiceProvider.GetService<IModDefinitionLoader>().ModDefinitions;

        var logger = ServiceProvider.GetService<ILoggerFactory>().GetLogger();
        logger.Information("Validating mods ...");
        if (!ServiceProvider.GetService<IModDefinitionProcessor>().PreprocessModDefinitions(ref definitions)) {
            logger.Error("Validation error detected. Canceling mod loading.");
            return;
        }

        var codeCompiler = ServiceProvider.GetService<ICodeCompiler>();
        var codePatcher = ServiceProvider.GetService<ICodePatcher>();

        var mods = new Mod[definitions.Length];

        for (var i = 0; i < definitions.Length; i++) {
            var definition    = definitions[i];
            var outputDllPath = codeCompiler.CompileMod(definition);
            codePatcher.ApplyPatches(definition);
            mods[i] = new Mod(definition, outputDllPath);
        }

        var moddingContext = new ModdingContext(mods);
        logger.Information("Created modding context ...");
        logger.Debug("mods: {mods}", JsonConvert.SerializeObject(moddingContext.Mods));

        _ServiceManager.AddSingleton<IModdingContext, ModdingContext>(_ => moddingContext);

        var pluginManager = _ServiceManager.GetService<IPluginManagerFactory>().CreatePluginManager(moddingContext);

        logger.Information("Instantiating plugins ...");
        foreach (var mod in mods.Where(o => o.AssemblyPath != null)) {
            mod.Plugins = pluginManager.CreatePlugins(mod).ToArray();
            mod.IsLoaded = true;
        }

        foreach (var mod in mods) {
            logger.Debug("enabling mod {id}", mod.Definition.Identifier);
            mod.IsEnabled = true;
        }

        logger.Information("Applying harmony patches ...");
        ServiceProvider.GetService<IHarmonyFactory>().CreateHarmony("Railroader.ModManager").PatchAllUncategorized(typeof(ModManager).Assembly);

        logger.Information("Mod loader loaded ...");
    }
}
