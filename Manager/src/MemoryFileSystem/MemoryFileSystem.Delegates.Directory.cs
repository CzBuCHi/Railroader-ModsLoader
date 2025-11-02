using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NSubstitute;
using Railroader.ModManager.Delegates.System.IO.Directory;

namespace MemoryFileSystem;

partial class MemoryFileSystem : MemoryFileSystem.IDirectory
{
    public interface IDirectory
    {
        EnumerateDirectories EnumerateDirectories { get; }
        GetCurrentDirectory  GetCurrentDirectory  { get; }
    }

    public IDirectory Directory => this;

    [MemberNotNull(nameof(_EnumerateDirectories))]
    [MemberNotNull(nameof(_GetCurrentDirectory))]
    private void Init_Directory() {
        _EnumerateDirectories = CreateEnumerateDirectories();
        _GetCurrentDirectory = CreateGetCurrentDirectory();
    }

    private EnumerateDirectories _EnumerateDirectories;
    private GetCurrentDirectory  _GetCurrentDirectory;

    EnumerateDirectories IDirectory.EnumerateDirectories => _EnumerateDirectories;

    GetCurrentDirectory IDirectory.GetCurrentDirectory => _GetCurrentDirectory;

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
