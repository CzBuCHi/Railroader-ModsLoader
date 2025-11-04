using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NSubstitute;
using Railroader.ModManager.Delegates.System.IO.Directory;

namespace MemoryFileSystem;

partial class MemoryFileSystem : MemoryFileSystem.IDirectory
{
    public interface IDirectory {
        DirectoryExists      Exists               { get; }
        EnumerateDirectories EnumerateDirectories { get; }
        GetCurrentDirectory  GetCurrentDirectory  { get; }
    }

    public IDirectory Directory => this;

    [MemberNotNull(nameof(_DirectoryExists))]
    [MemberNotNull(nameof(_EnumerateDirectories))]
    [MemberNotNull(nameof(_GetCurrentDirectory))]
    private void Init_Directory() {
        _DirectoryExists      = CreateDirectoryExists();
        _EnumerateDirectories = CreateEnumerateDirectories();
        _GetCurrentDirectory  = CreateGetCurrentDirectory();
    }

    private DirectoryExists      _DirectoryExists;
    private EnumerateDirectories _EnumerateDirectories;
    private GetCurrentDirectory  _GetCurrentDirectory;

    DirectoryExists IDirectory.Exists => _DirectoryExists;
    
    EnumerateDirectories IDirectory.EnumerateDirectories => _EnumerateDirectories;

    GetCurrentDirectory IDirectory.GetCurrentDirectory => _GetCurrentDirectory;

    private DirectoryExists CreateDirectoryExists() {
        var mock = Substitute.For<DirectoryExists>();
        mock.Invoke(Arg.Any<string>())
            .Returns([DebuggerStepThrough](o) => Items.TryGetValue(NormalizePath(o.Arg<string>()), out var entry) && entry is { IsDirectory: true });
        return mock;
    }
    
    private EnumerateDirectories CreateEnumerateDirectories() {
        var mock = Substitute.For<EnumerateDirectories>();
        mock.Invoke(Arg.Any<string>()).Returns(o => Enumerate(o.Arg<string>(), "*.*").Where(p => p.IsDirectory).Select(p => p.Path));
        return mock;
    }

    private GetCurrentDirectory CreateGetCurrentDirectory() {
        var mock = Substitute.For<GetCurrentDirectory>();
        mock.Invoke().Returns(_ => this is MemoryFs memoryFs
            ? memoryFs.CurrentDirectory
            : throw new InvalidOperationException($"Only {typeof(MemoryFs)} supports concept of '{nameof(MemoryFs.CurrentDirectory)}'."));
        return mock;
    }
}
