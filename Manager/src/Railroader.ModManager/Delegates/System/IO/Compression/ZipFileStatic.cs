using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace Railroader.ModManager.Delegates.System.IO.Compression;

public interface IZipFileStatic
{
    /// <inheritdoc cref="ZipFile.ExtractToDirectory(string, string)" />
    void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName);

    /// <inheritdoc cref="ZipFile.OpenRead(string)" />
    IZipArchive? OpenRead(string archiveFileName);
}

[ExcludeFromCodeCoverage]
public sealed class ZipFileStatic : IZipFileStatic
{
    public void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName) =>
        ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName);

    public IZipArchive? OpenRead(string archiveFileName) =>
        ZipArchiveWrapper.CreateWrapper(ZipFile.OpenRead(archiveFileName));
}
