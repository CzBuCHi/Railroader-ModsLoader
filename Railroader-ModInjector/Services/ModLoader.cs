using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Railroader.ModInjector.Wrappers;
using Railroader.ModInterfaces;
using Serilog;
using Serilog.Events;
using Path = System.IO.Path;

namespace Railroader.ModInjector.Services;

internal interface IModLoader
{
    IModDefinition[] LoadModDefinitions();
    void ProcessLogMessages(ILogger logger);
}

internal sealed class ModLoader(IFileSystem fileSystem) : IModLoader
{
    [ExcludeFromCodeCoverage]
    public ModLoader() : this(new FileSystemWrapper()) {
    }

    // This code is called before serilog configuration, so I cannot simply pass ILogger in ctor
    private readonly List<(LogEventLevel Level, string Format, object[] Args)> _LogMessages = new();

    public IModDefinition[] LoadModDefinitions() {
        var modDefinitions = new Dictionary<string, ModDefinition>(StringComparer.OrdinalIgnoreCase);

        var baseDirectory = Path.Combine(Environment.CurrentDirectory, "Mods");
        foreach (var item in fileSystem.Directory.EnumerateDirectories(baseDirectory)) {
            var path = Path.Combine(item, "Definition.json");
            if (!fileSystem.File.Exists(path)) {
                _LogMessages.Add((LogEventLevel.Warning, "Not loading directory {directory}: Missing Definition.json.", [item]));
                continue;
            }

            _LogMessages.Add((LogEventLevel.Debug, "Loaded definition from {directory}...", [item]));
            try {
                var jObject       = JObject.Parse(fileSystem.File.ReadAllText(path));
                var modDefinition = jObject.ToObject<ModDefinition>()!;

                if (modDefinitions.TryGetValue(modDefinition.Id, out var conflict)) {
                    _LogMessages.Add((LogEventLevel.Error, "Another mod with the same ID has been found in {directory}'", [conflict!.DefinitionPath]));
                } else {
                    modDefinition.DefinitionPath = item;
                    modDefinitions.Add(modDefinition.Id, modDefinition);
                }
            } catch (JsonException exc) {
                _LogMessages.Add((LogEventLevel.Error, "Failed to parse definition JSON from {directory}', json error: {exception}", [item, exc]));
            } catch (Exception exc) {
                _LogMessages.Add((LogEventLevel.Error, "Failed to parse definition JSON from {directory}', generic error: {exception}", [item, exc]));
            }
        }

        return modDefinitions.Values.Cast<IModDefinition>().ToArray();
    }

    public void ProcessLogMessages(ILogger logger) {
        foreach (var (level, format, args) in _LogMessages) {
            switch (level) {
                case LogEventLevel.Verbose:     logger.Verbose(format, args); break;
                case LogEventLevel.Debug:       logger.Debug(format, args); break;
                case LogEventLevel.Information: logger.Information(format, args); break;
                case LogEventLevel.Warning:     logger.Warning(format, args); break;
                case LogEventLevel.Error:       logger.Error(format, args); break;
                case LogEventLevel.Fatal:       logger.Fatal(format, args); break;
                default:                        throw new Exception($"Invalid log level ({level}).");
            }
        }
    }
}
