using System.Diagnostics;
using MemoryFileSystem2.Internal;
using Railroader.ModManager.Delegates.System.IO;

namespace MemoryFileSystem2;

partial class MemoryFileSystem
{
    public DirectoryInfoFactory DirectoryInfo {
        [DebuggerStepThrough]
        get { return path => new MemoryDirectoryInfo(this, path).Mock(); }
    }
}
