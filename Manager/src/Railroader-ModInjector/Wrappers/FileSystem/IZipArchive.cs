using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Linq;

namespace Railroader.ModManager.Wrappers.FileSystem;

/// <summary> Wrapper for <see cref="ZipArchive"/>. </summary>
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
    /// <inheritdoc />
    public IReadOnlyCollection<IZipArchiveEntry> Entries => archive.Entries.Select(e => new ZipArchiveEntryWrapper(e)).ToList().AsReadOnly();

    /// <inheritdoc />
    public IZipArchiveEntry? GetEntry(string entryName) => archive.GetEntry(entryName) is { } entry ? new ZipArchiveEntryWrapper(entry) : null;

    /// <inheritdoc />
    public void Dispose() => archive.Dispose();
}
