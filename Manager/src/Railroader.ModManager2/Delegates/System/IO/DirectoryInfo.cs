using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using _DirectoryInfo = System.IO.DirectoryInfo;

namespace Railroader.ModManager.Delegates.System.IO;

/// <inheritdoc cref="_DirectoryInfo(string)"/>
/// <remarks> Wraps <see cref="_DirectoryInfo(string)"/> for testability. </remarks>
public delegate IDirectoryInfo DirectoryInfoFactory(string path);

public interface IDirectoryInfo
{
    /// <inheritdoc cref="_DirectoryInfo.EnumerateFiles(string, SearchOption)"/>
    IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly);
}

[ExcludeFromCodeCoverage]
public sealed class DirectoryInfoWrapper(_DirectoryInfo directoryInfo) : IDirectoryInfo
{
    public static DirectoryInfoFactory Create => o => new DirectoryInfoWrapper(new _DirectoryInfo(o));

    public IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption)
        => directoryInfo.EnumerateFiles(searchPattern, searchOption).Select(o => new FileInfoWrapper(o));
}
