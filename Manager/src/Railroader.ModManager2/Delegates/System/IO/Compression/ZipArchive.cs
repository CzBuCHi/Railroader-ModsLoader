using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using _ZipArchive = System.IO.Compression.ZipArchive;
namespace Railroader.ModManager.Delegates.System.IO.Compression;


/// <summary> Wrapper for <see cref="_ZipArchive"/>. </summary>
public interface IZipArchive : IDisposable
{
    /// <inheritdoc cref="_ZipArchive.Entries" />
    IReadOnlyCollection<IZipArchiveEntry> Entries { get; }

    /// <inheritdoc cref="_ZipArchive.GetEntry(string)" />
    IZipArchiveEntry? GetEntry(string entryName);
}

[ExcludeFromCodeCoverage]
public sealed class ZipArchiveWrapper(_ZipArchive archive) : IZipArchive
{
    /// <inheritdoc />
    public IReadOnlyCollection<IZipArchiveEntry> Entries => archive.Entries.Select(e => new ZipArchiveEntryWrapper(e)).ToList().AsReadOnly();

    /// <inheritdoc />
    public IZipArchiveEntry? GetEntry(string entryName) => archive.GetEntry(entryName) is { } entry ? new ZipArchiveEntryWrapper(entry) : null;

    /// <inheritdoc />
    public void Dispose() => archive.Dispose();
}
