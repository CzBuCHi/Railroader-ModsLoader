using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Railroader.ModInjector;

public interface IFileSystem
{
    IDirectory Directory { get; }
    IFile      File      { get; }
}

public interface IDirectory
{
    IEnumerable<string> EnumerateDirectories(string baseDirectory);
}

public interface IFile
{
    bool Exists(string path);
    string ReadAllText(string path);
}

[ExcludeFromCodeCoverage]
internal sealed class FileSystem : IFileSystem
{
    public IDirectory Directory { get; } = new DirectoryWrapper();
    public IFile      File      { get; } = new FileWrapper();
}

[ExcludeFromCodeCoverage]
internal sealed class DirectoryWrapper : IDirectory
{
    public IEnumerable<string> EnumerateDirectories(string baseDirectory) => System.IO.Directory.EnumerateDirectories(baseDirectory);
}

[ExcludeFromCodeCoverage]
internal sealed class FileWrapper : IFile
{
    public bool Exists(string path) => System.IO.File.Exists(path);

    public string ReadAllText(string path) => System.IO.File.ReadAllText(path);
}
