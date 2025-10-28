using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Railroader.ModManager.Wrappers.FileSystem;

namespace MemoryFileSystem.Internal;

public sealed class MemoryDirectory(IMemoryFileSystem fileSystem) : IDirectory
{
    public IEnumerable<string> EnumerateDirectories(string path) =>
        fileSystem
            .Enumerate(path, "*.*")
            .Where(o => o.IsDirectory)
            .Select(o => o.Path);

    public string GetCurrentDirectory() => fileSystem is MemoryFs memoryFs
        ? memoryFs.CurrentDirectory
        : throw new InvalidOperationException("Only MemoryFs support concept of 'CurrentDirectory'.");

    public IDirectory Mock() {
        var mock = Substitute.For<IDirectory>();
        mock.EnumerateDirectories(Arg.Any<string>()).Returns(o => EnumerateDirectories(o.Arg<string>()));
        mock.GetCurrentDirectory().Returns(o => GetCurrentDirectory());
        return mock;
    }
}
