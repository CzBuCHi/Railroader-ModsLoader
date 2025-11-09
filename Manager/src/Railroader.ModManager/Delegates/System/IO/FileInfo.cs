using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Railroader.ModManager.Delegates.System.IO;

public interface IFileInfo
{
    /// <inheritdoc cref="FileInfo.LastWriteTime" />
    DateTime LastWriteTime { get; }

    /// <inheritdoc cref="FileInfo.FullName" />
    string FullName { get; }

    /// <inheritdoc cref="FileInfo.MoveTo(string)" />
    void MoveTo(string destFileName);
}

[ExcludeFromCodeCoverage]
public sealed class FileInfoWrapper(FileInfo fileInfo) : IFileInfo
{
    public DateTime LastWriteTime => fileInfo.LastWriteTime;

    public string FullName => fileInfo.FullName;

    public void MoveTo(string destFileName) => fileInfo.MoveTo(destFileName);
}
