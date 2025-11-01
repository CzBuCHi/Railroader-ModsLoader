using System.Diagnostics;
using MemoryFileSystem.Internal;
using Railroader.ModManager.Delegates.System.IO;

namespace MemoryFileSystem;

partial class MemoryFileSystem
{
    public DirectoryInfoFactory DirectoryInfo {
        [DebuggerStepThrough]
        get { return path => new MemoryDirectoryInfo(this, path).Mock(); }
    }
}
