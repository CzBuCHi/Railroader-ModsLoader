using System.Linq;
using JetBrains.Annotations;
using Railroader.ModInjector.Services;
using Serilog;
using Serilog.Core;

namespace Railroader.ModInjector;

/// <summary> Entry point for mod injection and initialization. </summary>
[UsedImplicitly(ImplicitUseTargetFlags.Members)]
internal static class Injector
{
    /*
    Game code was patched like this to call method below:
    (preprocessor directives show patch changes against vanilla code)

    namespace Logging;

    public class LogConfigurator : MonoBehaviour
    {
        private void Awake()
        {
            Log.Logger =
    #if PATCHED
                Injector.CreateLogger(
    #endif
                    MakeConfiguration()
                        .MinimumLevel.Information()
                        .MinimumLevel.Override("Model.AI.AutoEngineer", LogEventLevel.Warning)
                        .MinimumLevel.Override("Model.AI.AutoEngineerPlanner", LogEventLevel.Warning)
                        .MinimumLevel.Override("Effects.Decals.CanvasDecalRenderer", LogEventLevel.Warning)
    #if !PATCHED
                        .CreateLogger();
    #else
                );
    #endif

            Log.Information("Railroader {appVersion} ({buildId})", Application.version, App.Client.BuildId); // Original game code

    #if PATCHED
            Injector.ModInjectorMain();
    #endif
        }
    }
     */

    /// <summary> Initializes components for testing purposes. </summary>
    /// <param name="modManager">The mod manager instance.</param>
    /// <param name="modDefinitionLoader">The mod definition loader instance.</param>
    /// <param name="logManager">The log manager instance.</param>
    internal static void InitForTesting(IModManager modManager, IModDefinitionLoader modDefinitionLoader, ILogConfigurator logManager) {
        _ModManager = modManager;
        _ModDefinitionLoader = modDefinitionLoader;
        _LogManager = logManager;
    }

    private static IModManager?          _ModManager;
    private static IModDefinitionLoader? _ModDefinitionLoader;
    private static ILogConfigurator?          _LogManager;
    private static ModDefinition[]?      _ModDefinitions;
    private static ILogger?              _Logger;

    /// <summary> Main entry point for mod system initialization. </summary>
    public static void ModInjectorMain() {
        if (_Logger != null) {
            _Logger.Warning("ModInjectorMain called more than once.");
            return;
        }

        _Logger = Log.ForContext(typeof(Injector));

        (_ModManager ?? new ModManager()).Bootstrap(_ModDefinitions!);
        _ModManager = null;

        _Logger.Information("ModInjectorMain initialized successfully.");
    }

    /// <summary> Creates and configures the mod-aware logger. </summary>
    /// <param name="configuration">The base logger configuration.</param>
    /// <returns>The fully configured logger with mod support.</returns>
    public static Logger CreateLogger(LoggerConfiguration configuration) {
        _ModDefinitionLoader ??= new ModDefinitionLoader();
        _ModDefinitions = _ModDefinitionLoader.LoadDefinitions();

        (_LogManager ?? new LogConfigurator()).ConfigureLogger(configuration, _ModDefinitions);
        _LogManager = null;

        var logger = configuration.CreateLogger()!;

        var injectorLogger = logger.ForContext(typeof(Injector))!;

        _ModDefinitionLoader.ProcessLogMessages(injectorLogger);
        _ModDefinitionLoader = null;

        foreach (var modDefinition in _ModDefinitions.Where(o => o.LogLevel != null)) {
            injectorLogger.Information("Log level for {mod} set to {level}", modDefinition.Identifier, modDefinition.LogLevel);
        }

        return logger;
    }
}
