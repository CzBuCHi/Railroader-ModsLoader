using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Railroader.ModInjector.Wrappers;

// wrappers around System.IO types to simplify testing

public interface IFileSystem {
    IDirectory Directory { get; }
    IFile File { get; }
    IDirectoryInfo DirectoryInfo(string path);
}

public interface IDirectory {
    IEnumerable<string> EnumerateDirectories(string path);
    string GetCurrentDirectory();
}

public interface IFile {
    bool Exists(string path);
    string ReadAllText(string path);
    DateTime GetLastWriteTime(string path);
    void Delete(string path);
}

public interface IDirectoryInfo {
    IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption);
}

public interface IFileInfo {
    DateTime LastWriteTime { get; }
    string FullName { get; }
}

[ExcludeFromCodeCoverage]
public sealed class FileSystemWrapper : IFileSystem {
    public IDirectory Directory => new DirectoryWrapper();
    public IFile File => new FileWrapper();
    public IDirectoryInfo DirectoryInfo(string path) => new DirectoryInfoWrapper(new DirectoryInfo(path));
}

[ExcludeFromCodeCoverage]
public sealed class DirectoryWrapper : IDirectory {
    public IEnumerable<string> EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);
    public string GetCurrentDirectory() => Directory.GetCurrentDirectory();
}

[ExcludeFromCodeCoverage]
public sealed class FileWrapper : IFile {
    public bool Exists(string path) => File.Exists(path);
    public string ReadAllText(string path) => File.ReadAllText(path);
    public DateTime GetLastWriteTime(string path) => File.GetLastWriteTime(path);
    public void Delete(string path) => File.Delete(path);
}

[ExcludeFromCodeCoverage]
public sealed class DirectoryInfoWrapper(DirectoryInfo directoryInfo) : IDirectoryInfo {
    public IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption) => directoryInfo.EnumerateFiles(searchPattern, searchOption).Select(o => new FileInfoWrapper(o));
}

[ExcludeFromCodeCoverage]
public sealed class FileInfoWrapper(FileInfo fileInfo) : IFileInfo {
    public DateTime LastWriteTime => fileInfo.LastWriteTime;
    public string FullName => fileInfo.FullName;
}
