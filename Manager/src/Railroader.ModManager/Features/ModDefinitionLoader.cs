using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json.Linq;
using Railroader.ModManager.Delegates.System.IO;
using Railroader.ModManager.Services;
using Path = System.IO.Path;

namespace Railroader.ModManager.Features;

/// <summary>
///     Delegate that represents a method for loading all available <see cref="ModDefinition" /> instances.
/// </summary>
/// <returns>
///     An array of loaded <see cref="ModDefinition" /> objects.
///     If no definitions could be loaded, an empty array is returned.
/// </returns>
public delegate ModDefinition[] LoadDefinitionsDelegate();

/// <summary>
///     Provides functionality to locate, read, and parse mod definition files
///     from the <c>Mods</c> directory in the current working directory.
/// </summary>
public static class ModDefinitionLoader
{
    /// <summary>
    ///     Creates a default delegate instance that loads all mod definitions
    ///     using real file system and directory operations.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="IMemoryLogger" /> instance used for diagnostic output.
    /// </param>
    /// <returns>
    ///     A <see cref="LoadDefinitionsDelegate" /> that can be invoked to load mod definitions.
    /// </returns>
    [ExcludeFromCodeCoverage]
    public static LoadDefinitionsDelegate Create(IMemoryLogger logger) =>
        () => LoadDefinitions(logger, FileSystem.Instance);

    /// <summary>
    ///     Loads all valid <see cref="ModDefinition" /> instances from the <c>Mods</c> directory.
    /// </summary>
    /// <param name="logger"> The <see cref="IMemoryLogger" /> used for warnings, informational messages, and errors. </param>
    /// <param name="fileSystem"></param>
    /// <returns>
    ///     An array of valid, distinct <see cref="ModDefinition" /> objects.
    ///     If the <c>Mods</c> directory is missing or no valid definitions are found, an empty array is returned.
    /// </returns>
    public static ModDefinition[] LoadDefinitions(
        IMemoryLogger logger,
        IFileSystem fileSystem
    ) {
        var modDefinitions = new Dictionary<string, ModDefinition>(StringComparer.OrdinalIgnoreCase);

        var baseDirectory = Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "Mods");
        if (!fileSystem.Directory.Exists(baseDirectory)) {
            logger.Warning("Mods directory not found at {baseDirectory}", baseDirectory);
            return [];
        }

        foreach (var modDir in fileSystem.Directory.EnumerateDirectories(baseDirectory)) {
            var definitionPath = Path.Combine(modDir, "Definition.json");
            if (!fileSystem.File.Exists(definitionPath)) {
                logger.Warning("Not loading directory {directory}: Missing Definition.json.", modDir);
                continue;
            }

            logger.Information("Loading definition from {directory} ...", modDir);
            try {
                var jObject = JObject.Parse(fileSystem.File.ReadAllText(definitionPath));
                var modDef  = jObject.ToObject<ModDefinition>()!;
                if (!modDef.IsValid) {
                    logger.Error("Skipping mod at {definitionPath}: Invalid mod definition.", definitionPath);
                    continue;
                }

                if (modDefinitions.TryGetValue(modDef.Identifier, out var existing)) {
                    logger.Error("Duplicate mod identifier '{identifier}' found in '{newDirectory}'. Already defined in '{existingDirectory}'.",
                                 modDef.Identifier, modDir, existing.BasePath);
                    continue;
                }

                modDef.BasePath = modDir;
                modDefinitions.Add(modDef.Identifier, modDef);
            } catch (Exception exc) {
                logger.Error("Failed to parse definition JSON: {exception}", exc);
            }
        }

        return modDefinitions.Values.ToArray();
    }
}