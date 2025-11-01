using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Railroader.ModManager.Delegates.System.IO.Directory;
using Railroader.ModManager.Delegates.System.IO.File;
using Railroader.ModManager.Services;
using Path = System.IO.Path;

namespace Railroader.ModManager.Features;

public delegate ModDefinition[] ModDefinitionLoaderDelegate();

public static class ModDefinitionLoader
{
    [ExcludeFromCodeCoverage]
    public static ModDefinitionLoaderDelegate Factory(IMemoryLogger logger) =>
        () => LoadDefinitions(logger, Directory.GetCurrentDirectory, Directory.EnumerateDirectories, File.Exists, File.ReadAllText);

    public static ModDefinition[] LoadDefinitions(IMemoryLogger logger, GetCurrentDirectory getCurrentDirectory, EnumerateDirectories enumerateDirectories, Exists exists, ReadAllText readAllText) {
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

        return modDefinitions.Values.ToArray();
    }
}
