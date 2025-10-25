using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Railroader.ModInjector.Wrappers.FileSystem;

/// <summary> Wrapper for types in System.IO namespace. </summary>
public interface IFileSystem
{
    /// <inheritdoc cref="System.IO.Directory"/>
    IDirectory Directory { get; }

    /// <inheritdoc cref="System.IO.File"/>
    IFile File { get; }

    /// <inheritdoc cref="System.IO.DirectoryInfo(string)"/>
    IDirectoryInfo DirectoryInfo(string path);
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public sealed class FileSystemWrapper : IFileSystem
{
    /// <inheritdoc />
    public IDirectory Directory { get; } = new DirectoryWrapper();

    /// <inheritdoc />
    public IFile File { get; } = new FileWrapper();

    /// <inheritdoc />
    public IDirectoryInfo DirectoryInfo(string path) => new DirectoryInfoWrapper(new DirectoryInfo(path));
}
