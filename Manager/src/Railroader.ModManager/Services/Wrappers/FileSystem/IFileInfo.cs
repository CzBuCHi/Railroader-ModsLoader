using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Railroader.ModManager.Services.Wrappers.FileSystem;

/// <summary> Wrapper for <see cref="FileInfo"/>. </summary>
public interface IFileInfo
{
    /// <inheritdoc cref="FileInfo.LastWriteTime"/>
    DateTime LastWriteTime { get; }

    /// <inheritdoc cref="FileInfo.FullName"/>
    string FullName { get; }

    /// <inheritdoc cref="FileInfo.MoveTo(string)"/>
    void MoveTo(string destFileName);
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
internal sealed class FileInfoWrapper(FileInfo fileInfo) : IFileInfo
{
    /// <inheritdoc />
    public DateTime LastWriteTime => fileInfo.LastWriteTime;

    /// <inheritdoc />
    public string FullName => fileInfo.FullName;

    /// <inheritdoc />
    public void MoveTo(string destFileName) => fileInfo.MoveTo(destFileName);
}
