using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;

namespace Railroader.ModManager.Wrappers.FileSystem;

/// <summary> Wrapper for <see cref="ZipArchiveEntry"/>. </summary>
public interface IZipArchiveEntry
{
    /// <inheritdoc cref="ZipArchiveEntry.FullName"/>
    string FullName { get; }

    /// <inheritdoc cref="ZipArchiveEntry.Name"/>
    string Name { get; }

    /// <inheritdoc cref="ZipArchiveEntry.Open()"/>
    Stream Open();
}

[ExcludeFromCodeCoverage]
public sealed class ZipArchiveEntryWrapper(ZipArchiveEntry entry) : IZipArchiveEntry
{
    /// <inheritdoc />
    public string FullName => entry.FullName;

    /// <inheritdoc />
    public string Name => entry.Name;

    /// <inheritdoc />
    public Stream Open() => entry.Open();
}
