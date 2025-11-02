using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Railroader.ModManager.Delegates.System.IO;
using Railroader.ModManager.Delegates.System.IO.Compression.ZipFile;
using Railroader.ModManager.Delegates.System.IO.Directory;
using Railroader.ModManager.Services;
using ZipFile = System.IO.Compression.ZipFile;

namespace Railroader.ModManager.Features;

public delegate void ExtractModsDelegate();

[PublicAPI]
public static class ModExtractor
{
    [ExcludeFromCodeCoverage]
    public static ExtractModsDelegate ExtractMods(IMemoryLogger logger) =>
        () => ExtractMods(logger, DirectoryInfoWrapper.Create, Directory.GetCurrentDirectory, ZipFileDefaults.OpenRead,
            ZipFile.ExtractToDirectory);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void ExtractMods(
        IMemoryLogger logger,
        DirectoryInfoFactory directoryInfo,
        GetCurrentDirectory getCurrentDirectory,
        OpenRead openRead,
        ExtractToDirectory extractToDirectory
    ) {
        var modsDirectory = Path.Combine(getCurrentDirectory(), "Mods");
        foreach (var zipFile in directoryInfo(modsDirectory).EnumerateFiles("*.zip")) {
            ExtractMod(logger, modsDirectory, openRead, extractToDirectory, zipFile);
        }
    }

    private static void ExtractMod(
        IMemoryLogger logger,
        string modsDirectory,
        OpenRead openRead,
        ExtractToDirectory extractToDirectory,
        IFileInfo zipFile
    ) {
        var zipPath = zipFile.FullName;
        logger.Information("Processing mod archive '{ZipPath}' for extraction.", zipPath);

        var archive = openRead(zipPath);
        if (archive is null) {
            logger.Error("Failed to open archive '{ZipPath}'.", zipPath);
            return;
        }

        string? json;
        try {
            var entry = archive.GetEntry("Definition.json");
            if (entry is null) {
                logger.Error("Skipping archive '{ZipPath}': Missing 'Definition.json'.", zipPath);
                return;
            }

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            json = reader.ReadToEnd();
        } catch (Exception ex) when (!(ex is JsonException)) {
            logger.Error(ex, "Failed to read archive '{ZipPath}'.", zipPath);
            return;
        } finally {
            archive.Dispose();
        }

        ModDefinition? modDefinition;
        try {
            modDefinition = JsonConvert.DeserializeObject<ModDefinition>(json)!;
        } catch (JsonException ex) {
            logger.Error(ex, "Skipping archive '{ZipPath}': Failed to parse Definition.json.", zipPath);
            return;
        }

        if (modDefinition is not { IsValid: true }) {
            logger.Error("Skipping archive '{ZipPath}': Invalid 'Definition.json'.", zipPath);
            return;
        }

        var extractPath = Path.Combine(modsDirectory, modDefinition.Identifier);

        try {
            extractToDirectory(zipPath, extractPath);
            logger.Information("Successfully extracted mod '{ModId}' from '{ZipPath}' to '{ExtractPath}'.",
                modDefinition.Identifier, zipPath, extractPath);

            // Rename to .bak only on success
            var bakPath = Path.ChangeExtension(zipPath, ".bak");
            zipFile.MoveTo(bakPath);
        } catch (Exception ex) {
            logger.Error(ex, "Failed to unzip archive '{ZipPath}'.", zipPath);
        }
    }
}
