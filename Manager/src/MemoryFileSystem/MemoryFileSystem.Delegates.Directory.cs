using System;
using System.Diagnostics;
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

    private EnumerateDirectories? _EnumerateDirectories;
    private GetCurrentDirectory?  _GetCurrentDirectory;

    EnumerateDirectories IDirectory.EnumerateDirectories {
        [DebuggerStepThrough]
        get { return _EnumerateDirectories ??= CreateEnumerateDirectories(); }
    }

    GetCurrentDirectory IDirectory.GetCurrentDirectory {
        [DebuggerStepThrough]
        get { return _GetCurrentDirectory ??= CreateGetCurrentDirectory(); }
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
