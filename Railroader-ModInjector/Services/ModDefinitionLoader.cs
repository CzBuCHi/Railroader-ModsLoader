using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;

namespace Railroader.ModInjector.Services;

/// <summary> Loads mod definitions from JSON files and handles early logging. </summary>
internal interface IModDefinitionLoader
{
    /// <summary> Loads all mod definitions from the mod directory. </summary>
    /// <returns>An array of loaded mod definitions.</returns>
    ModDefinition[] LoadDefinitions();

    /// <summary> Processes and emits stored log messages to the specified logger. </summary>
    /// <param name="logger">The logger to emit messages to.</param>
    void ProcessLogMessages(ILogger logger);
}

/// <inheritdoc />
internal class ModDefinitionLoader : IModDefinitionLoader
{
    /// <summary> Stores log messages created before Serilog is configured. </summary>
    private readonly List<(LogEventLevel Level, string Format, object[] Args)> _LogMessages = new();

    /// <inheritdoc />
    public ModDefinition[] LoadDefinitions() {
        var modDefinitions = new Dictionary<string, ModDefinition>(StringComparer.OrdinalIgnoreCase);

        var baseDirectory = Path.Combine(Environment.CurrentDirectory, "Mods");
        foreach (var item in Directory.EnumerateDirectories(baseDirectory)) {
            var path = Path.Combine(item, "Definition.json");
            if (!File.Exists(path)) {
                _LogMessages.Add((LogEventLevel.Warning, "Not loading directory {directory}: Missing Definition.json.", [item]));
                continue;
            }

            _LogMessages.Add((LogEventLevel.Information, "Loading definition from {directory}...", [item]));
            try {
                var jObject       = JObject.Parse(File.ReadAllText(path));
                var modDefinition = jObject.ToObject<ModDefinition>()!;

                if (modDefinitions.TryGetValue(modDefinition.Identifier, out var conflict)) {
                    _LogMessages.Add((LogEventLevel.Error, "Another mod with the same Identifier has been found in '{directory}'", [conflict!.BasePath]));
                } else {
                    modDefinition.BasePath = item;
                    modDefinitions.Add(modDefinition.Identifier, modDefinition);
                }
            } catch (JsonException exc) {
                _LogMessages.Add((LogEventLevel.Error, "Failed to parse definition JSON from {directory}', json error: {exception}", [item, exc]));
            } catch (Exception exc) {
                _LogMessages.Add((LogEventLevel.Error, "Failed to parse definition JSON from {directory}', generic error: {exception}", [item, exc]));
            }
        }

        return modDefinitions.Values.ToArray();
    }

    /// <inheritdoc />
    public void ProcessLogMessages(ILogger logger) {
        foreach (var (level, format, args) in _LogMessages) {
            switch (level) {
#pragma warning disable CA2254
                case LogEventLevel.Verbose:     logger.Verbose(format, args); break;
                case LogEventLevel.Debug:       logger.Debug(format, args); break;
                case LogEventLevel.Information: logger.Information(format, args); break;
                case LogEventLevel.Warning:     logger.Warning(format, args); break;
                case LogEventLevel.Error:       logger.Error(format, args); break;
                case LogEventLevel.Fatal:       logger.Fatal(format, args); break;
#pragma warning restore CA2254
                default: throw new Exception($"Invalid log level ({level}).");
            }
        }
    }
}
