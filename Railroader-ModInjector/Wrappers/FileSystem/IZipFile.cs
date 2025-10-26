using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace Railroader.ModInjector.Wrappers.FileSystem;

/// <summary> Wrapper for <see cref="ZipFile"/>. </summary>
public interface IZipFile
{
    /// <inheritdoc cref="ZipFile.ExtractToDirectory(string, string)" />
    void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName);

    /// <inheritdoc cref="ZipFile.OpenRead(string)" />
    IZipArchive? OpenRead(string archiveFileName);
}

[ExcludeFromCodeCoverage]
public sealed class ZipFileWrapper : IZipFile
{
    /// <inheritdoc />
    public void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName) => ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName);

    /// <inheritdoc />
    public IZipArchive? OpenRead(string archiveFileName) => Wrap(ZipFile.OpenRead(archiveFileName));

    private static IZipArchive? Wrap(ZipArchive? zipArchive) => zipArchive != null ? new ZipArchiveWrapper(zipArchive) : null;
}
