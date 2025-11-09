using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Railroader.ModManager.Delegates.System.IO;

/// <inheritdoc cref="DirectoryInfo(string)" />
/// <remarks> Wraps <see cref="DirectoryInfo(string)" /> for testability. </remarks>
public delegate IDirectoryInfo DirectoryInfoFactory(string path);

public interface IDirectoryInfo
{
    /// <inheritdoc cref="DirectoryInfo.EnumerateFiles(string, SearchOption)" />
    IEnumerable<IFileInfo> EnumerateFiles(
        string searchPattern,
        SearchOption searchOption = SearchOption.TopDirectoryOnly
    );
}

[ExcludeFromCodeCoverage]
public sealed class DirectoryInfoWrapper(DirectoryInfo directoryInfo) : IDirectoryInfo
{
    public IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption) =>
        directoryInfo.EnumerateFiles(searchPattern, searchOption).Select(o => new FileInfoWrapper(o));
}
