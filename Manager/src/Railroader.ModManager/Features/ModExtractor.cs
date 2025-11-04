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

/// <summary>
/// Represents an action that extracts all mod archives from the <c>Mods</c> directory.
/// </summary>
public delegate void ModExtractionAction();

/// <summary>
/// Provides functionality to extract mod archives (.zip) containing a valid <c>Definition.json</c>.
/// </summary>
[PublicAPI]
public static class ModExtractor {
    
    /// <summary>
    /// Creates a <see cref="ModExtractionAction"/> that extracts all mod archives using the specified logger.
    /// </summary>
    /// <param name="logger">The logger used to record extraction progress and errors.</param>
    /// <returns>A delegate that, when invoked, performs the full extraction process.</returns>
    [ExcludeFromCodeCoverage]
    public static ModExtractionAction GetExtractor(IMemoryLogger logger) =>
        () => ExtractAll(logger, DirectoryInfoWrapper.Create, Directory.GetCurrentDirectory, ZipFileDefaults.OpenRead, ZipFile.ExtractToDirectory);

    /// <summary>
    /// Extracts all <c>*.zip</c> files from the <c>Mods</c> directory that contain a valid <c>Definition.json</c>.
    /// </summary>
    /// <param name="logger">The logger for reporting progress and errors.</param>
    /// <param name="directoryInfo">Factory to create <see cref="IDirectoryInfo"/> wrappers.</param>
    /// <param name="getCurrentDirectory">Delegate to retrieve the current working directory.</param>
    /// <param name="openRead">Delegate to open a ZIP archive for reading.</param>
    /// <param name="extractToDirectory">Delegate to extract a ZIP archive to a directory.</param>
    /// <remarks>
    /// Each archive is processed independently. Invalid or duplicate mods are skipped with appropriate logging.
    /// Successfully extracted archives are moved to a <c>.bak</c> backup with a unique name if needed.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void ExtractAll(IMemoryLogger logger, DirectoryInfoFactory directoryInfo, GetCurrentDirectory getCurrentDirectory, OpenRead openRead, ExtractToDirectory extractToDirectory) {
        var modsDirectory = Path.Combine(getCurrentDirectory(), "Mods");
        var zipFiles      = directoryInfo(modsDirectory).EnumerateFiles("*.zip");

        foreach (var zipFile in zipFiles) {
            try {
                TryExtractOne(logger, openRead, extractToDirectory, zipFile, modsDirectory);
            } catch (Exception exc) {
                logger.Error(exc, "Failed to unzip archive {ZipPath}.", zipFile.FullName);
            }
        }
    }

    /// <summary>
    /// Attempts to extract a single mod archive.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="openRead">Delegate to open the ZIP archive.</param>
    /// <param name="extractToDirectory">Delegate to extract the archive contents.</param>
    /// <param name="zipFile">The ZIP file to process.</param>
    /// <param name="modsDirectory">The root directory containing mods.</param>
    /// <remarks>
    /// Early returns occur if:
    /// <list type="bullet">
    /// <item><description><c>Definition.json</c> is missing</description></item>
    /// <item><description>JSON parsing fails</description></item>
    /// <item><description><see cref="ModDefinition.IsValid"/> is <c>false</c></description></item>
    /// <item><description>Target extraction directory already exists</description></item>
    /// </list>
    /// On success, the archive is moved to a <c>.bak</c> backup.
    /// </remarks>
    private static void TryExtractOne(IMemoryLogger logger, OpenRead openRead, ExtractToDirectory extractToDirectory, IFileInfo zipFile, string modsDirectory) {
        logger.Information("Processing mod archive {ZipPath} for extraction.", zipFile.FullName);

        using var archive = openRead(zipFile.FullName) ?? throw new InvalidOperationException($"Failed to open archive: {zipFile.FullName}");

        var definitionEntry = archive.GetEntry("Definition.json");
        if (definitionEntry == null) {
            logger.Error("Skipping archive {ZipPath}: Missing 'Definition.json'.", zipFile.FullName);
            return;
        }

        string json;
        using (var entryStream = definitionEntry.Open())
        using (var reader = new StreamReader(entryStream)) {
            json = reader.ReadToEnd();
        }

        var modDefinition = TryDeserialize(logger, json, zipFile.FullName);
        if (modDefinition == null) {
            return;
        }

        if (!modDefinition.IsValid) {
            logger.Error("Skipping archive {ZipPath}: Invalid mod definition.", zipFile.FullName);
            return;
        }

        var extractPath = Path.Combine(modsDirectory, modDefinition.Identifier);

        if (Directory.Exists(extractPath)) {
            logger.Warning("Extraction path {ExtractPath} already exists – skipping mod {ModId}.", extractPath, modDefinition.Identifier);
            MoveToBackup(zipFile, logger, ".dup");
            return;
        }

        extractToDirectory(zipFile.FullName, extractPath);

        logger.Information("Successfully extracted mod {ModId} from {ZipPath} to {ExtractPath}.", modDefinition.Identifier, zipFile.FullName, extractPath);

        MoveToBackup(zipFile, logger, ".bak");
    }

    /// <summary>
    /// Attempts to deserialize the <c>Definition.json</c> content into a <see cref="ModDefinition"/>.
    /// </summary>
    /// <param name="logger">The logger for reporting JSON parsing errors.</param>
    /// <param name="json">The JSON string from <c>Definition.json</c>.</param>
    /// <param name="zipPath">The path of the ZIP file, used for logging context.</param>
    /// <returns>The deserialized <see cref="ModDefinition"/>, or <c>null</c> if parsing fails.</returns>
    private static ModDefinition? TryDeserialize(IMemoryLogger logger, string json, string zipPath) {
        try {
            return JsonConvert.DeserializeObject<ModDefinition>(json);
        } catch (JsonException ex) {
            logger.Error(ex, "Skipping archive {ZipPath}: Failed to parse Definition.json.", zipPath);
            return null;
        }
    }

    /// <summary>
    /// Moves the processed ZIP file to a backup location with the specified extension.
    /// </summary>
    /// <param name="zipFile">The ZIP file to back up.</param>
    /// <param name="logger">The logger for reporting backup failures.</param>
    /// <param name="extension">
    /// The backup file extension (e.g., <c>".bak"</c> or <c>".dup"</c>).
    /// </param>
    /// <remarks>
    /// If a file with the target name already exists, a numeric suffix is appended
    /// (e.g., <c>mod.zip.bak</c> → <c>mod.zip.bak1</c>) until a unique name is found.
    /// </remarks>
    private static void MoveToBackup(IFileInfo zipFile, IMemoryLogger logger, string extension) {
        var basePath   = Path.ChangeExtension(zipFile.FullName, extension);
        var backupPath = basePath;

        var i = 0;
        while (File.Exists(backupPath)) {
            backupPath = $"{basePath}{i++}";
        }

        try {
            zipFile.MoveTo(backupPath);
        } catch (Exception ex) {
            logger.Warning(ex, "Failed to create backup for {ZipPath} at {BackupPath}.", zipFile.FullName, backupPath);
        }
    }
}