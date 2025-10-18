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

    private static ModDefinition[]  _ModDefinitions = [];

    /// <summary> Main entry point for mod system initialization. </summary>
    public static void ModInjectorMain() {
        DI.ModManager().Bootstrap(_ModDefinitions);
    }

    /// <summary> Creates and configures the mod-aware logger. </summary>
    /// <param name="configuration">The base logger configuration.</param>
    /// <returns>The fully configured logger with mod support.</returns>
    public static Logger? CreateLogger(LoggerConfiguration configuration) {
        var initLogger = (IInitLogger)DI.Logger;

        _ModDefinitions = DI.ModDefinitionLoader().LoadDefinitions();

        DI.LogConfigurator().ConfigureLogger(configuration, _ModDefinitions);
        DI.CreateLogger(configuration);

        var injectorLogger = DI.GetLogger();
        initLogger.Flush(injectorLogger);

        foreach (var modDefinition in _ModDefinitions.Where(o => o.LogLevel != null)) {
            injectorLogger.Information("Log level for {mod} set to {level}", modDefinition.Identifier, modDefinition.LogLevel);
        }

        return DI.Logger as Logger;
    }
}
