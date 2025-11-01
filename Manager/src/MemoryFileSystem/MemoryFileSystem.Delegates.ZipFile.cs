using System.Diagnostics;
using System.IO;
using System.Linq;
using MemoryFileSystem2.Internal;
using Newtonsoft.Json;
using NSubstitute;
using Railroader.ModManager.Delegates.System.IO.Compression.ZipFile;

namespace MemoryFileSystem2;

partial class MemoryFileSystem : MemoryFileSystem.IZipFile
{
    public interface IZipFile
    {
        ExtractToDirectory ExtractToDirectory { get; }
        OpenRead           OpenRead           { get; }
    }

    public IZipFile ZipFile => this;

    private ExtractToDirectory? _ExtractToDirectory;
    private OpenRead?           _OpenRead;

    ExtractToDirectory IZipFile.ExtractToDirectory {
        [DebuggerStepThrough]
        get { return _ExtractToDirectory ??= CreateExtractToDirectory(); }
    }

    OpenRead IZipFile.OpenRead {
        [DebuggerStepThrough]
        get { return _OpenRead ??= CreateOpenRead(); }
    }

    private ExtractToDirectory CreateExtractToDirectory() {
        var mock = Substitute.For<ExtractToDirectory>();
        mock.When(o => o.Invoke(Arg.Any<string>(), Arg.Any<string>())).Do(o => {
            var normalizedSource = NormalizePath(o.ArgAt<string>(0));
            var normalizedDest   = NormalizePath(o.ArgAt<string>(1));

            if (!Items.TryGetValue(normalizedSource, out var zipEntry) || zipEntry.IsDirectory) {
                throw new FileNotFoundException($"Zip file '{normalizedSource}' not found.");
            }

            // Deserialize zip contents
            try {
                var entries = new MemoryZip(zipEntry.ExistingContent);
                foreach (var entry in entries.OrderBy(p => p.Path.Length)) {
                    Add(entry with { Path = Path.Combine(normalizedDest, entry.Path) });
                }
            } catch (JsonException ex) {
                throw new InvalidDataException($"Failed to deserialize zip contents for '{normalizedSource}'.", ex);
            }
        });
        return mock;
    }

    private OpenRead CreateOpenRead() {
        var mock = Substitute.For<OpenRead>();
        mock.Invoke(Arg.Any<string>()).Returns(o => {
            var normalizedPath = NormalizePath(o.Arg<string>());
            if (!Items.TryGetValue(normalizedPath, out var zipEntry) || zipEntry.IsDirectory) {
                throw new FileNotFoundException($"Zip file '{normalizedPath}' not found.");
            }

            return new MemoryZipArchive(new MemoryZip(zipEntry.ExistingContent)).Mock();
        });
        return mock;
    }
}
