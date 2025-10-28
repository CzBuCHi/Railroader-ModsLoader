using System.IO;
using NSubstitute;
using Railroader.ModManager.Wrappers.FileSystem;

namespace MemoryFileSystem.Internal;

public sealed class MemoryZipArchiveEntry(MemoryEntry entry) : IZipArchiveEntry
{
    public string FullName => entry.Path;
    public string Name     => Path.GetFileName(entry.Path);
    public Stream Open() => new MemoryStream(entry.ExistingContent);

    public IZipArchiveEntry Mock() {
        var mock = Substitute.For<IZipArchiveEntry>();
        mock.FullName.Returns(_ => FullName);
        mock.Name.Returns(_ => Name);
        mock.Open().Returns(_ => Open());
        return mock;
    }
}
