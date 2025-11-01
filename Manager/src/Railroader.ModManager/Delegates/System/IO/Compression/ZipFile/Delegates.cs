using System.Diagnostics.CodeAnalysis;
using Railroader.ModManager.Delegates.System.IO.Compression.ZipArchive;
using _ZipFile = System.IO.Compression.ZipFile;

namespace Railroader.ModManager.Delegates.System.IO.Compression.ZipFile;

/// <inheritdoc cref="_ZipFile.ExtractToDirectory(string, string)" />
/// <remarks> Wraps <see cref="_ZipFile.ExtractToDirectory(string, string)"/> for testability. </remarks>
public delegate void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName);

/// <inheritdoc cref="_ZipFile.OpenRead(string)" />
/// <remarks> Wraps <see cref="_ZipFile.OpenRead(string)"/> for testability. </remarks>
public delegate IZipArchive? OpenRead(string archiveFileName);

[ExcludeFromCodeCoverage]
public static class ZipFileDefaults
{
    public static OpenRead OpenRead => o => new ZipArchiveWrapper(_ZipFile.OpenRead(o)!);
}