using System;
using System.Diagnostics;
using System.Linq;
using NSubstitute;
using Railroader.ModManager.Delegates.System.IO;

namespace MemoryFileSystem;

partial class MemoryFileSystem
{
    public IDirectoryStatic Directory { get; }

    private IDirectoryStatic CreateDirectoryStatic() {
        var directory = Substitute.For<IDirectoryStatic>();
        
        directory.Exists(Arg.Any<string>())
            .Returns([DebuggerStepThrough](o) => Items.TryGetValue(NormalizePath(o.Arg<string>()), out var entry) && entry is { IsDirectory: true });
        
        directory.EnumerateDirectories(Arg.Any<string>())
            .Returns(o => Enumerate(o.Arg<string>(), "*.*").Where(p => p.IsDirectory).Select(p => p.Path));
        
        directory.GetCurrentDirectory().Returns(_ => this is MemoryFs memoryFs
            ? memoryFs.CurrentDirectory
            : throw new InvalidOperationException($"Only {typeof(MemoryFs)} supports concept of '{nameof(MemoryFs.CurrentDirectory)}'."));
        
        return directory;
    }
}
