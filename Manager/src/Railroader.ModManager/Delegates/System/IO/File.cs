using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using _File = System.IO.File;

namespace Railroader.ModManager.Delegates.System.IO;

public interface IFileStatic
{
    /// <inheritdoc cref="File.Exists(string)" />
    bool Exists(string path);

    /// <inheritdoc cref="File.ReadAllText(string)" />
    string ReadAllText(string path);

    /// <inheritdoc cref="File.WriteAllText(string, string)" />
    void WriteAllText(string path, string contents);

    /// <inheritdoc cref="File.GetLastWriteTime(string)" />
    DateTime GetLastWriteTime(string path);

    /// <inheritdoc cref="File.Delete(string)" />
    void Delete(string path);

    /// <inheritdoc cref="File.Move(string, string)" />
    void Move(string sourceFileName, string destFileName);

    /// <inheritdoc cref="File.Create(string)" />
    Stream Create(string path);
}

[ExcludeFromCodeCoverage]
public sealed class FileStatic : IFileStatic
{
    /// <inheritdoc />
    public bool Exists(string path) => _File.Exists(path);

    /// <inheritdoc />
    public string ReadAllText(string path) => _File.ReadAllText(path);

    /// <inheritdoc />
    public void WriteAllText(string path, string contents) => _File.WriteAllText(path, contents);

    /// <inheritdoc />
    public DateTime GetLastWriteTime(string path) => _File.GetLastWriteTime(path);

    /// <inheritdoc />
    public void Delete(string path) => _File.Delete(path);

    /// <inheritdoc />
    public void Move(string sourceFileName, string destFileName) => _File.Move(sourceFileName, destFileName);

    /// <inheritdoc />
    public Stream Create(string path) => _File.Create(path);
}
