using System.Diagnostics;
using System.IO;
using MemoryFileSystem2.Types;
using NSubstitute;
using Railroader.ModManager.Delegates.System.IO.Compression;

namespace MemoryFileSystem2.Internal;

[DebuggerStepThrough]
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
