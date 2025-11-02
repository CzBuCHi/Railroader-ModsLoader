using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using NSubstitute;
using Railroader.ModManager.Delegates.System.IO;

namespace MemoryFileSystem.Internal;

[DebuggerStepThrough]
public sealed class MemoryDirectoryInfo(IMemoryFileSystem fileSystem, string path) : IDirectoryInfo
{
    private readonly string _Path = fileSystem.NormalizePath(path);

    public IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
        fileSystem.Enumerate(_Path, searchPattern, searchOption)
                  .Where(o => !o.IsDirectory)
                  .Select(o => new MemoryFileInfo(fileSystem, o.Path).Mock());

    [ExcludeFromCodeCoverage]
    public IDirectoryInfo Mock() {
        var mock = Substitute.For<IDirectoryInfo>();
        mock.EnumerateFiles(Arg.Any<string>(), Arg.Any<SearchOption>())
            .Returns(o => EnumerateFiles(o.Arg<string>(), o.Arg<SearchOption>()));
        return mock;
    }
}
