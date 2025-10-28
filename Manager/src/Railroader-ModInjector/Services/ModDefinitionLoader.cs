using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Railroader.ModInjector.Wrappers.FileSystem;
using Serilog;
using Path = System.IO.Path;

namespace Railroader.ModInjector.Services;

/// <summary> Loads mod definitions from JSON files and handles early logging. </summary>
internal interface IModDefinitionLoader
{
    /// <summary> Loads all mod definitions from the mod directory. </summary>
    /// <returns>An array of loaded mod definitions.</returns>
    ModDefinition[] LoadDefinitions();
}

internal sealed class ModDefinitionLoader : IModDefinitionLoader
{
    public required IFileSystem FileSystem { get; init; }
    public required ILogger     Logger     { get; init; }

    public ModDefinition[] LoadDefinitions() {
        var modDefinitions = new Dictionary<string, ModDefinition>(StringComparer.OrdinalIgnoreCase);

        var baseDirectory = Path.Combine(FileSystem.Directory.GetCurrentDirectory(), "Mods");
        foreach (var directory in FileSystem.Directory.EnumerateDirectories(baseDirectory)) {
            var path = Path.Combine(directory, "Definition.json");
            if (!FileSystem.File.Exists(path)) {
                Logger.Warning("Not loading directory {directory}: Missing Definition.json.", directory);
                continue;
            }

            Logger.Information("Loading definition from {directory}...", directory);
            try {
                var jObject       = JObject.Parse(FileSystem.File.ReadAllText(path));
                var modDefinition = jObject.ToObject<ModDefinition>()!;

                if (modDefinitions.TryGetValue(modDefinition.Identifier, out var conflict)) {
                    Logger.Error("Another mod with the same Identifier has been found in '{directory}'", conflict!.BasePath);
                } else {
                    modDefinition.BasePath = directory;
                    modDefinitions.Add(modDefinition.Identifier, modDefinition);
                }
            } catch (JsonException exc) {
                Logger.Error("Failed to parse definition JSON, json error: {exception}", exc);
            } catch (Exception exc) {
                Logger.Error("Failed to parse definition JSON, generic error: {exception}", exc);
            }
        }

        return modDefinitions.Values.ToArray();
    }
}
