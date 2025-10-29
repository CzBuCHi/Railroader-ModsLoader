using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Railroader.ModManager.Services.Wrappers.FileSystem;

/// <summary> Wrapper for <see cref="Directory"/>. </summary>
public interface IDirectory
{
    /// <inheritdoc cref="Directory.EnumerateDirectories(string)"/>
    IEnumerable<string> EnumerateDirectories(string path);

    /// <inheritdoc cref="Directory.GetCurrentDirectory()"/>
    string GetCurrentDirectory();
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
internal sealed class DirectoryWrapper : IDirectory
{
    /// <inheritdoc />
    public IEnumerable<string> EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);

    /// <inheritdoc />
    public string GetCurrentDirectory() => Directory.GetCurrentDirectory();
}
