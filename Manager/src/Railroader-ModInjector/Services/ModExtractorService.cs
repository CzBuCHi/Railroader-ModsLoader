using System.IO;
using Newtonsoft.Json;
using Railroader.ModManager.Wrappers.FileSystem;
using Serilog;

namespace Railroader.ModManager.Services;

public interface IModExtractorService
{
    void ExtractMods();
}

public sealed class ModExtractorService : IModExtractorService
{
    public required IFileSystem FileSystem { get; init; }
    public required ILogger     Logger     { get; init; }

    public void ExtractMods() {
        var modsDirectory = Path.Combine(FileSystem.Directory.GetCurrentDirectory(), "Mods");

        // Find all .zip files in Mods directory
        var modsDirInfo = FileSystem.DirectoryInfo(modsDirectory);
        var zipFiles    = modsDirInfo.EnumerateFiles("*.zip");
        foreach (var zipFile in zipFiles) {
            var zipPath = zipFile.FullName;
            Logger.Information("Processing mod archive '{ZipPath}' for extraction.", zipPath);

            // Read Definition.json from the zip
            var modDefinition = GetModDefinitionFromZip(zipPath);
            if (modDefinition is not { IsValid: true }) {
                Logger.Error("Skipping archive '{ZipPath}': Invalid or missing 'Definition.json'.", zipPath);
                continue;
            }

            // Define extraction path using Identifier
            var extractPath = Path.Combine(modsDirectory, modDefinition.Identifier);
            FileSystem.ZipFile.ExtractToDirectory(zipPath, extractPath);
            FileSystem.File.Move(zipPath, Path.ChangeExtension(zipPath, ".bak"));
            Logger.Information("Successfully extracted mod '{ModId}' from '{ZipPath}' to '{ExtractPath}'.", modDefinition.Identifier, zipPath, extractPath);
        }
    }

    private ModDefinition? GetModDefinitionFromZip(string zipPath) {
        try {
            using var archive         = FileSystem.ZipFile.OpenRead(zipPath);
            var       definitionEntry = archive?.GetEntry("Definition.json");
            if (definitionEntry == null) {
                return null;
            }

            using var entryStream = definitionEntry.Open();
            using var reader      = new StreamReader(entryStream);
            var       json        = reader.ReadToEnd();

            return JsonConvert.DeserializeObject<ModDefinition>(json);
        } catch (JsonException ex) {
            Logger.Error(ex, "Failed to parse Definition.json in {ZipPath}.", zipPath);
            return null;
        }
    }
}
