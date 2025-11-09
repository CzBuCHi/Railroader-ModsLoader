using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using _Directory = System.IO.Directory;

namespace Railroader.ModManager.Delegates.System.IO;

public interface IDirectoryStatic
{
    /// <inheritdoc cref="_Directory.Exists(string)" />
    bool Exists(string path);

    /// <inheritdoc cref="_Directory.EnumerateDirectories(string)" />
    IEnumerable<string> EnumerateDirectories(string path);

    /// <inheritdoc cref="_Directory.GetCurrentDirectory()" />
    string GetCurrentDirectory();
}

[ExcludeFromCodeCoverage]
public sealed class DirectoryStatic : IDirectoryStatic
{
    /// <inheritdoc />
    public bool Exists(string path) => _Directory.Exists(path);

    /// <inheritdoc />
    public IEnumerable<string> EnumerateDirectories(string path) => _Directory.EnumerateDirectories(path);

    /// <inheritdoc />
    public string GetCurrentDirectory() => _Directory.GetCurrentDirectory();
}
