using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Railroader.ModManager.Delegates.System.IO.Directory;
using Railroader.ModManager.Delegates.System.IO.File;
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
internal sealed class ModDefinitionLoader(
    IMemoryLogger logger,
    GetCurrentDirectory getCurrentDirectory,
    EnumerateDirectories enumerateDirectories,
    Exists exists,
    ReadAllText readAllText
) : IModDefinitionLoader
{
    public ModDefinitionLoader(IMemoryLogger logger)
        : this(logger, Directory.GetCurrentDirectory, Directory.EnumerateDirectories, File.Exists, File.ReadAllText) {
    }


    /// <inheritdoc />
    public ModDefinition[] ModDefinitions { get; private set; } = [];

    /// <inheritdoc />
    public void LoadDefinitions() {
        var modDefinitions = new Dictionary<string, ModDefinition>(StringComparer.OrdinalIgnoreCase);

        var baseDirectory = Path.Combine(getCurrentDirectory(), "Mods");
        foreach (var directory in enumerateDirectories(baseDirectory)) {
            var path = Path.Combine(directory, "Definition.json");
            if (!exists(path)) {
                logger.Warning("Not loading directory {directory}: Missing Definition.json.", directory);
                continue;
            }

            logger.Information("Loading definition from {directory} ...", directory);
            try {
                var jObject       = JObject.Parse(readAllText(path));
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
