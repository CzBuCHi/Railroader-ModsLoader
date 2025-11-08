using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Railroader.ModManagerInstaller.Abstractions;

public interface IFileStatic
{
    bool Exists(string path);
    string[] ReadAllLines(string path);
    void Copy(string sourceFileName, string destFileName);
    void SetLastWriteTime(string path, DateTime lastWriteTime);
    DateTime GetLastWriteTime(string path);
    Stream Open(string path, FileMode mode, FileAccess access);
}

[ExcludeFromCodeCoverage]
public sealed class FileStatic : IFileStatic
{
    public bool Exists(string path) => File.Exists(path);

    public string[] ReadAllLines(string path) => File.ReadAllLines(path);

    public void Copy(string sourceFileName, string destFileName) => File.Copy(sourceFileName, destFileName);

    public void SetLastWriteTime(string path, DateTime lastWriteTime) => File.SetLastWriteTime(path, lastWriteTime);

    public DateTime GetLastWriteTime(string path) => File.GetLastWriteTime(path);

    public Stream Open(string path, FileMode mode, FileAccess access) => File.Open(path, mode, access);
}
