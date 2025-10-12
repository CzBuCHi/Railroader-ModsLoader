using System.Collections;
using System.Linq;
using System.Reflection;
using Logging;
using Railroader.ModInterfaces;
using Serilog;
using Serilog.Events;

namespace Railroader.ModInjector.Services;

internal interface ILogManager
{
    void ConfigureLogger(LoggerConfiguration configuration, IModDefinition[] definitions);
}

internal sealed class LogManager : ILogManager
{
    public void ConfigureLogger(LoggerConfiguration configuration, IModDefinition[] definitions) {
        // configure log levels 
        configuration.MinimumLevel!.Override("Railroader.ModInjector", LogEventLevel.Debug);

        foreach (var modDefinition in definitions.Where(o => o.LogLevel != null && o.LogLevel != LogEventLevel.Information)) {
            configuration.MinimumLevel!.Override(modDefinition.Id, modDefinition.LogLevel!.Value);
        }

        // remove vanilla sink
        var logEventSinksField = typeof(LoggerConfiguration).GetField("_logEventSinks", BindingFlags.NonPublic | BindingFlags.Instance);
        if (logEventSinksField != null) {
            var logEventSinks = (IList)logEventSinksField.GetValue(configuration)!;
            foreach (var sink in logEventSinks.OfType<SerilogUnityConsoleEventSink>().ToArray()) {
                logEventSinks.Remove(sink);
            }
        }

        // configure modded sinks
        configuration.WriteTo!.Conditional(o => o.Properties!.ContainsKey("SourceContext"), o => o.UnityConsole("[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"));
        configuration.WriteTo.Conditional(o => !o.Properties!.ContainsKey("SourceContext"), o => o.UnityConsole("[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));
    }
}
