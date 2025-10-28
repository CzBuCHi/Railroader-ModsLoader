using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NSubstitute;
using Railroader.ModManager.Wrappers.FileSystem;

namespace MemoryFileSystem.Internal;

public sealed class MemoryZipFile(IMemoryFileSystem fileSystem) : IZipFile
{
    public void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName) {
        var normalizedSource = fileSystem.NormalizePath(sourceArchiveFileName);
        var normalizedDest   = fileSystem.NormalizePath(destinationDirectoryName);

        if (!fileSystem.Items.TryGetValue(normalizedSource, out var zipEntry) || zipEntry.IsDirectory) {
            throw new FileNotFoundException($"Zip file '{normalizedSource}' not found.");
        }

        // Deserialize zip contents
        try {
            var entries = new MemoryZip(zipEntry.ExistingContent);
            foreach (var entry in entries.OrderBy(o => o.Path.Length)) {
                fileSystem.Add(entry with { Path = Path.Combine(normalizedDest, entry.Path) });
            }
        } catch (JsonException ex) {
            throw new InvalidDataException($"Failed to deserialize zip contents for '{normalizedSource}'.", ex);
        }
    }

    public IZipArchive OpenRead(string archiveFileName) {
        var normalizedPath = fileSystem.NormalizePath(archiveFileName);
        if (!fileSystem.Items.TryGetValue(normalizedPath, out var zipEntry) || zipEntry.IsDirectory) {
            throw new FileNotFoundException($"Zip file '{normalizedPath}' not found.");
        }

        return new MemoryZipArchive(new MemoryZip(zipEntry.ExistingContent));
    }

    public IZipFile Mock() {
        var mock = Substitute.For<IZipFile>();
        mock.When(o => o.ExtractToDirectory(Arg.Any<string>(), Arg.Any<string>())).Do(o => ExtractToDirectory(o.ArgAt<string>(0), o.ArgAt<string>(1)));
        mock.OpenRead(Arg.Any<string>()).Returns(o => OpenRead(o.Arg<string>()));
        return mock;
    }
}
