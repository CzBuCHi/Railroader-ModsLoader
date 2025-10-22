using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace NSubstitute.FileSystem;

/// <summary> Wrapper for <see cref="File"/>. </summary>
public interface IFile
{
    /// <inheritdoc cref="File.Exists(string)"/>
    bool Exists(string path);

    /// <inheritdoc cref="File.ReadAllText(string)"/>
    string ReadAllText(string path);

    /// <inheritdoc cref="File.GetLastWriteTime(string)"/>
    DateTime GetLastWriteTime(string path);

    /// <inheritdoc cref="File.Delete(string)"/>
    void Delete(string path);

    /// <inheritdoc cref="File.Move(string, string)"/>
    void Move(string sourceFileName, string destFileName);

    /// <inheritdoc cref="File.Create(string)"/>
    Stream Create(string path);
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
internal sealed class FileWrapper : IFile
{
    /// <inheritdoc />
    public bool Exists(string path) => File.Exists(path);

    /// <inheritdoc />
    public string ReadAllText(string path) => File.ReadAllText(path);

    /// <inheritdoc />
    public DateTime GetLastWriteTime(string path) => File.GetLastWriteTime(path);

    /// <inheritdoc />
    public void Delete(string path) => File.Delete(path);

    /// <inheritdoc />
    public void Move(string tempFilePath, string assemblyPath) => File.Move(tempFilePath, assemblyPath);

    /// <inheritdoc />
    public Stream Create(string path) => File.Create(path);
}
