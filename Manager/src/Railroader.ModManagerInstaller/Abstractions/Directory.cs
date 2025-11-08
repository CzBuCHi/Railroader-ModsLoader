using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Railroader.ModManagerInstaller.Abstractions;

public interface IDirectoryStatic
{
    DirectoryInfo CreateDirectory(string path);
    string GetCurrentDirectory();
    void SetCurrentDirectory(string path);
    bool Exists(string path);
}

[ExcludeFromCodeCoverage]
public sealed class DirectoryStatic : IDirectoryStatic
{
    public DirectoryInfo CreateDirectory(string path) => Directory.CreateDirectory(path);

    public string GetCurrentDirectory() => Directory.GetCurrentDirectory();

    public void SetCurrentDirectory(string path) => Directory.SetCurrentDirectory(path);

    public bool Exists(string path) => Directory.Exists(path);
}
