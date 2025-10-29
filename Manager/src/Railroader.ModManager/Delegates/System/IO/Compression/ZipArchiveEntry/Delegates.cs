using System.Diagnostics.CodeAnalysis;
using System.IO;
using _ZipArchiveEntry = System.IO.Compression.ZipArchiveEntry;

namespace Railroader.ModManager.Delegates.System.IO.Compression.ZipArchiveEntry;

/// <summary> Wrapper for <see cref="_ZipArchiveEntry"/>. </summary>
public interface IZipArchiveEntry
{
    /// <inheritdoc cref="_ZipArchiveEntry.FullName"/>
    string FullName { get; }

    /// <inheritdoc cref="_ZipArchiveEntry.Name"/>
    string Name { get; }

    /// <inheritdoc cref="_ZipArchiveEntry.Open()"/>
    Stream Open();
}

[ExcludeFromCodeCoverage]
public sealed class ZipArchiveEntryWrapper(_ZipArchiveEntry entry) : IZipArchiveEntry
{
    /// <inheritdoc />
    public string FullName => entry.FullName;

    /// <inheritdoc />
    public string Name => entry.Name;

    /// <inheritdoc />
    public Stream Open() => entry.Open();
}
