using System;
using System.IO;
using NSubstitute;
using Railroader.ModManager.Wrappers.FileSystem;

namespace MemoryFileSystem.Internal;

public sealed class MemoryFileInfo(IMemoryFileSystem fileSystem, string path) : IFileInfo
{
    public DateTime LastWriteTime {
        get { 
            if (fileSystem.Items.TryGetValue(FullName, out var entry) && entry is { IsDirectory: false }) {
                return entry.LastWriteTime;
            }

            throw new FileNotFoundException($"File not found: {FullName}");
        }
    }

    public string FullName { get; } = fileSystem.NormalizePath(path);

    public IFileInfo Mock() {
        var mock = Substitute.For<IFileInfo>();
        mock.FullName.Returns(_ => FullName);
        mock.LastWriteTime.Returns(_ => LastWriteTime);
        return mock;
    }
}
