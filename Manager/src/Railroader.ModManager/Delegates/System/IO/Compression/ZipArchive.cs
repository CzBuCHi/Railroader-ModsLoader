using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Linq;

namespace Railroader.ModManager.Delegates.System.IO.Compression;

public interface IZipArchive : IDisposable
{
    /// <inheritdoc cref="ZipArchive.Entries" />
    IReadOnlyCollection<IZipArchiveEntry> Entries { get; }

    /// <inheritdoc cref="ZipArchive.GetEntry(string)" />
    IZipArchiveEntry? GetEntry(string entryName);
}

[ExcludeFromCodeCoverage]
public sealed class ZipArchiveWrapper(ZipArchive archive) : IZipArchive
{
    public static IZipArchive? CreateWrapper(ZipArchive? archive) =>
        archive != null ? new ZipArchiveWrapper(archive) : null;

    /// <inheritdoc />
    public IReadOnlyCollection<IZipArchiveEntry> Entries =>
        archive.Entries.Select(ZipArchiveEntryWrapper.CreateWrapper).Cast<IZipArchiveEntry>().ToList().AsReadOnly();

    /// <inheritdoc />
    public IZipArchiveEntry? GetEntry(string entryName) => ZipArchiveEntryWrapper.CreateWrapper(archive.GetEntry(entryName));

    /// <inheritdoc />
    public void Dispose() => archive.Dispose();
}
