using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Logging;
using Railroader.ModManager.Features;
using Railroader.ModManager.Services;
using Serilog;
using Serilog.Events;

namespace Railroader.ModManager.HarmonyPatches;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
[HarmonyPatch]
[HarmonyPatchCategory("LogManager")]
[ExcludeFromCodeCoverage]
public static class LogManagerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(LogManager), "Awake")]
    public static void AwakePrefix(out MemoryLogger __state) {
        __state = (MemoryLogger)Log.Logger!;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LogManager), "Awake")]
    public static void AwakePostfix(ref MemoryLogger __state) {
        __state.Information("Finalizing logger configuration");
        var logger = Log.Logger!;
        __state.Flush(logger);
        logger.Information("Logger configured successfully");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LogManager), "MakeConfiguration")]
    public static void MakeConfigurationPostfix(ref LoggerConfiguration __result) {
        var logger = Log.Logger!;
        try {
            foreach (var pair in Bootstrapper.ModDefinitions.Where(o => o.LogLevel != null && o.LogLevel != LogEventLevel.Information)) {
                var identifier = pair.Identifier;
                if (identifier == "") {
                    identifier = "Railroader.ModManager";
                }

                logger.Information("Setting log level for {identifier} to {level}", identifier, pair.LogLevel!.Value);
                __result.MinimumLevel.Override(identifier, pair.LogLevel.Value);
            }

            RemoveUnitySinks(__result);

            // Configure modded sinks
            __result.WriteTo.Conditional(o => o.Properties.ContainsKey("SourceContext"), o => o.UnityConsole("[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"));
            __result.WriteTo.Conditional(o => !o.Properties.ContainsKey("SourceContext"), o => o.UnityConsole("[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

        } catch (Exception exc) {
            logger.Error(exc, "Failed to configure serilog");
        }
    }

    /// <summary> Removes vanilla Unity console sinks from the configuration. </summary>
    /// <param name="configuration">The logger configuration to modify.</param>
    private static void RemoveUnitySinks(LoggerConfiguration configuration) {
        var field = typeof(LoggerConfiguration).GetField("_logEventSinks", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) {
            throw new InvalidOperationException($"Unable to get field {typeof(LoggerConfiguration)}::_logEventSinks");
        }

        var sinks = (IList)field.GetValue(configuration)!;

        foreach (var sink in sinks.OfType<SerilogUnityConsoleEventSink>().ToList()) {
            sinks.Remove(sink);
        }
    }
}
