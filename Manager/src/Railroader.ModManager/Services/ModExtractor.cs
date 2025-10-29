using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Railroader.ModManager.Services.Wrappers.FileSystem;

namespace Railroader.ModManager.Services;

internal interface IModExtractor
{
    void ExtractMods();
}

/// <inheritdoc />
internal sealed class ModExtractor(IFileSystem fileSystem, IMemoryLogger logger) : IModExtractor
{
    /// <inheritdoc />
    public void ExtractMods() {
        var modsDirectory = Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "Mods");
        var zipFiles      = fileSystem.DirectoryInfo(modsDirectory).EnumerateFiles("*.zip").ToArray();

        foreach (var zipFile in zipFiles) {
            ExtractMod(zipFile, modsDirectory);
        }
    }

    private void ExtractMod(IFileInfo zipFile, string modsDirectory) {
        var zipPath = zipFile.FullName;
        // Rename zip to bak so future ExtractMods calls will no longer pick this file ... 
        zipFile.MoveTo(Path.ChangeExtension(zipFile.FullName, ".bak"));

        var success = false;
        try {
            logger.Information("Processing mod archive '{ZipPath}' for extraction.", zipPath);

            // Read Definition.json from the zip
            using var archive         = fileSystem.ZipFile.OpenRead(zipFile.FullName);
            var       definitionEntry = archive?.GetEntry("Definition.json");
            if (definitionEntry == null) {
                logger.Error("Skipping archive '{ZipPath}': Missing 'Definition.json'.", zipPath);
                return;
            }

            using var entryStream = definitionEntry.Open();
            using var reader      = new StreamReader(entryStream);
            var       json        = reader.ReadToEnd();

            ModDefinition? modDefinition;
            try {
                modDefinition = JsonConvert.DeserializeObject<ModDefinition>(json);
            } catch (JsonException ex) {
                logger.Error(ex, "Skipping archive '{ZipPath}': Failed to parse Definition.json.", zipPath);
                throw;
            }

            if (modDefinition is not { IsValid: true }) {
                logger.Error("Skipping archive '{ZipPath}': Invalid 'Definition.json'.", zipPath);
                return;
            }

            // Define extraction path using Identifier
            var extractPath = Path.Combine(modsDirectory, modDefinition.Identifier);
            fileSystem.ZipFile.ExtractToDirectory(zipFile.FullName, extractPath);

            logger.Information("Successfully extracted mod '{ModId}' from '{ZipPath}' to '{ExtractPath}'.", modDefinition.Identifier, zipPath, extractPath);
            success = true;
        } catch (Exception exc) {
            logger.Error("Failed to unzip archive '{ZipPath}': {exception}.", zipPath, exc);
        } finally {
            if (!success) {
                // Extraction failed - rename file back to zip to indicate it was not extracted
                zipFile.MoveTo(zipPath);
            }
        }
    }
}
