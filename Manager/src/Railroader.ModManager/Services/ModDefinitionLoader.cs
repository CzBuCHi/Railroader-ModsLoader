using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Railroader.ModManager.Services.Wrappers.FileSystem;
using Path = System.IO.Path;

namespace Railroader.ModManager.Services;

/// <summary> Loads mod definitions from JSON files and handles early logging. </summary>
internal interface IModDefinitionLoader
{
    /// <summary>An array of loaded mod definitions.</summary>
    ModDefinition[] ModDefinitions { get; }

    /// <summary> Loads all mod definitions from the mod directory. </summary>
    void LoadDefinitions();
}

/// <inheritdoc />
internal sealed class ModDefinitionLoader(IFileSystem fileSystem, IMemoryLogger logger) : IModDefinitionLoader
{
    /// <inheritdoc />
    public ModDefinition[] ModDefinitions { get; private set; } = [];

    /// <inheritdoc />
    public void LoadDefinitions() {
        var modDefinitions = new Dictionary<string, ModDefinition>(StringComparer.OrdinalIgnoreCase);

        var baseDirectory = Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "Mods");
        foreach (var directory in fileSystem.Directory.EnumerateDirectories(baseDirectory)) {
            var path = Path.Combine(directory, "Definition.json");
            if (!fileSystem.File.Exists(path)) {
                logger.Warning("Not loading directory {directory}: Missing Definition.json.", directory);
                continue;
            }

            logger.Information("Loading definition from {directory} ...", directory);
            try {
                var jObject       = JObject.Parse(fileSystem.File.ReadAllText(path));
                var modDefinition = jObject.ToObject<ModDefinition>()!;

                if (modDefinitions.TryGetValue(modDefinition.Identifier, out var conflict)) {
                    logger.Error("Another mod with the same Identifier has been found in '{directory}'", conflict.BasePath);
                } else {
                    modDefinition.BasePath = directory;
                    modDefinitions.Add(modDefinition.Identifier, modDefinition);
                }
            } catch (JsonException exc) {
                logger.Error("Failed to parse definition JSON, json error: {exception}", exc);
            } catch (Exception exc) {
                logger.Error("Failed to parse definition JSON, generic error: {exception}", exc);
            }
        }

        ModDefinitions = modDefinitions.Values.ToArray();
    }
}
