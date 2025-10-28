using System;
using System.IO;
using JetBrains.Annotations;
using NSubstitute;

namespace MemoryFileSystem;

[PublicAPI]
public sealed class MemoryFs : MemoryFileSystemBase
{
    public MemoryFs(string? currentDirectory = null) {
        CurrentDirectory = currentDirectory ?? "C:\\";
    }

    private string _CurrentDirectory = null!;

    public string CurrentDirectory {
        get => _CurrentDirectory;
        set {
            var normalized = NormalizePath(value);
            VerifyParents(normalized);
            _CurrentDirectory = normalized;
        }
    }
    
    public override string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) {
            throw new ArgumentException("Path cannot be null or empty.");
        }

        // Resolve relative paths against _currentDirectory
        if (!Path.IsPathRooted(path)) {
            path = Path.Combine(CurrentDirectory, path);
        }

        path = Path.GetFullPath(path);
        if (path.Length > 3) { // Trim trailing slash for non-root paths
            path = path.TrimEnd('\\');
        }

        return path;
    }

    protected override string? GetParentPath(string path) => Path.GetDirectoryName(path);
}
