using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Railroader.ModManager.Services.Wrappers.FileSystem;

/// <summary> Wrapper for <see cref="DirectoryInfo"/>. </summary>
public interface IDirectoryInfo
{
    /// <inheritdoc cref="DirectoryInfo.EnumerateFiles(string, SearchOption)"/>
    IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly);
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
internal sealed class DirectoryInfoWrapper(DirectoryInfo directoryInfo) : IDirectoryInfo
{
    /// <inheritdoc />
    public IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption)
        => directoryInfo.EnumerateFiles(searchPattern, searchOption).Select(o => new FileInfoWrapper(o));
}
