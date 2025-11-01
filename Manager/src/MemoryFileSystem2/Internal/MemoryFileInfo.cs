using System;
using System.Diagnostics;
using System.IO;
using NSubstitute;
using Railroader.ModManager.Delegates.System.IO;

namespace MemoryFileSystem2.Internal;

[DebuggerStepThrough]
public sealed class MemoryFileInfo(MemoryFileSystem fileSystem, string path) : IFileInfo
{
    public DateTime LastWriteTime {
        get {
            if (fileSystem.Items.TryGetValue(FullName, out var entry) && entry is { IsDirectory: false }) {
                return entry.LastWriteTime;
            }

            throw new FileNotFoundException($"File not found: {FullName}");
        }
    }

    public string FullName { get; private set; } = fileSystem.NormalizePath(path);

    public void MoveTo(string destFileName) {
        destFileName = fileSystem.NormalizePath(destFileName);
        fileSystem.File.Move(FullName, destFileName);
        FullName = destFileName;
    }

    public IFileInfo Mock() {
        var mock = Substitute.For<IFileInfo>();
        mock.FullName.Returns(_ => FullName);
        mock.LastWriteTime.Returns(_ => LastWriteTime);
        mock.When(o => o.MoveTo(Arg.Any<string>())).Do(o => MoveTo(o.Arg<string>()));
        return mock;
    }
}
