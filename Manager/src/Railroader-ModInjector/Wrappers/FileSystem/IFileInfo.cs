using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Railroader.ModInjector.Wrappers.FileSystem;

/// <summary> Wrapper for <see cref="FileInfo"/>. </summary>
public interface IFileInfo
{
    /// <inheritdoc cref="FileSystemInfo.LastWriteTime"/>
    DateTime LastWriteTime { get; }

    /// <inheritdoc cref="FileSystemInfo.FullName"/>
    string FullName { get; }
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
internal sealed class FileInfoWrapper(FileInfo fileInfo) : IFileInfo
{
    /// <inheritdoc />
    public DateTime LastWriteTime => fileInfo.LastWriteTime;

    /// <inheritdoc />
    public string FullName => fileInfo.FullName;
}
