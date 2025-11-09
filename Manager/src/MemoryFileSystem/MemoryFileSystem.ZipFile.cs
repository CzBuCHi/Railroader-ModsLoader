using System.IO;
using System.Linq;
using MemoryFileSystem.Internal;
using Newtonsoft.Json;
using NSubstitute;
using Railroader.ModManager.Delegates.System.IO.Compression;

namespace MemoryFileSystem;

partial class MemoryFileSystem
{
    public IZipFileStatic ZipFile { get; }

    private IZipFileStatic CreateZipFileStatic() {
        var mock = Substitute.For<IZipFileStatic>();

        mock.When(o => o.ExtractToDirectory(Arg.Any<string>(), Arg.Any<string>())).Do(o => {
            var normalizedSource = NormalizePath(o.ArgAt<string>(0));
            var normalizedDest   = NormalizePath(o.ArgAt<string>(1));

            if (!Items.TryGetValue(normalizedSource, out var zipEntry) || zipEntry.IsDirectory) {
                throw new FileNotFoundException($"Zip file '{normalizedSource}' not found.");
            }

            try {
                var entries = new MemoryZip(zipEntry.Content!);
                foreach (var entry in entries.OrderBy(p => p.Path.Length)) {
                    Add(entry with { Path = Path.Combine(normalizedDest, entry.Path) });
                }
            } catch (JsonException ex) {
                throw new InvalidDataException($"Failed to deserialize zip contents for '{normalizedSource}'.", ex);
            }
        });

        mock.OpenRead(Arg.Any<string>()).Returns(o => {
            var normalizedPath = NormalizePath(o.Arg<string>());
            if (!Items.TryGetValue(normalizedPath, out var zipEntry) || zipEntry.IsDirectory) {
                throw new FileNotFoundException($"Zip file '{normalizedPath}' not found.");
            }

            return new MemoryZipArchive(new MemoryZip(zipEntry.Content!)).Mock();
        });

        return mock;
    }
}
