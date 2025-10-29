using System;
using System.Diagnostics.CodeAnalysis;
using _FileInfo = System.IO.FileInfo;

namespace Railroader.ModManager.Delegates.System.IO.FileInfo;

/// <inheritdoc cref="_FileInfo(string)"/>
/// <remarks> Wraps <see cref="_FileInfo(string)"/> for testability. </remarks>
internal delegate IFileInfo FileInfoInfoFactory(string path);

public interface IFileInfo
{
    /// <inheritdoc cref="_FileInfo.LastWriteTime"/>
    DateTime LastWriteTime { get; }

    /// <inheritdoc cref="_FileInfo.FullName"/>
    string FullName { get; }

    /// <inheritdoc cref="_FileInfo.MoveTo(string)"/>
    void MoveTo(string destFileName);
}

[ExcludeFromCodeCoverage]
internal sealed class FileInfoWrapper(_FileInfo fileInfo) : IFileInfo
{
    public static FileInfoInfoFactory Create => o => new FileInfoWrapper(new _FileInfo(o));

    public DateTime LastWriteTime => fileInfo.LastWriteTime;

    public string FullName => fileInfo.FullName;

    public void MoveTo(string destFileName) => fileInfo.MoveTo(destFileName);
}
