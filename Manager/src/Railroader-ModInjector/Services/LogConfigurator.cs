using System.Collections;
using System.Linq;
using System.Reflection;
using Logging;
using Serilog;
using Serilog.Events;

namespace Railroader.ModInjector.Services;

/// <summary> Configures Serilog for modding with per-mod log levels and custom sinks. </summary>
internal interface ILogConfigurator
{
    /// <summary> Configures the logger with mod-specific settings. </summary>
    /// <param name="configuration">The logger configuration to modify.</param>
    /// <param name="definitions">The loaded mod definitions.</param>
    void ConfigureLogger(LoggerConfiguration configuration, ModDefinition[] definitions);
}

/// <inheritdoc />
internal sealed class LogConfigurator : ILogConfigurator
{
    /// <inheritdoc />
    public void ConfigureLogger(LoggerConfiguration configuration, ModDefinition[] definitions) {
        // Configure log levels 
        configuration.MinimumLevel!.Override("Railroader.ModInjector", LogEventLevel.Debug);

        foreach (var modDefinition in definitions.Where(o => o.LogLevel != null && o.LogLevel != LogEventLevel.Information)) {
            configuration.MinimumLevel!.Override(modDefinition.Identifier, modDefinition.LogLevel!.Value);
        }

        RemoveUnitySinks(configuration);

        // Configure modded sinks
        configuration.WriteTo!.Conditional(o => o.Properties!.ContainsKey("SourceContext"), o => o.UnityConsole("[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"));
        configuration.WriteTo.Conditional(o => !o.Properties!.ContainsKey("SourceContext"), o => o.UnityConsole("[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));
    }

    /// <summary> Removes vanilla Unity console sinks from the configuration. </summary>
    /// <param name="configuration">The logger configuration to modify.</param>
    private static void RemoveUnitySinks(LoggerConfiguration configuration) {
        var field = typeof(LoggerConfiguration).GetField("_logEventSinks", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var sinks = (IList)field.GetValue(configuration)!;

        foreach (var sink in sinks.OfType<SerilogUnityConsoleEventSink>().ToList()) {
            sinks.Remove(sink);
        }
    }
}
