using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Serilog;

namespace Railroader.ModInjector.Wrappers;

/// <summary> Wrapper for System.IO namespace. </summary>
internal interface IFileSystem
{
    /// <inheritdoc cref="System.IO.Directory"/>
    IDirectory Directory { get; }

    /// <inheritdoc cref="System.IO.File"/>
    IFile File { get; }

    /// <inheritdoc cref="System.IO.DirectoryInfo(string)"/>
    IDirectoryInfo DirectoryInfo(string path);
}

/// <summary> Wrapper for <see cref="Directory"/>. </summary>
internal interface IDirectory
{
    /// <inheritdoc cref="Directory.EnumerateDirectories(string)"/>
    IEnumerable<string> EnumerateDirectories(string path);

    /// <inheritdoc cref="Directory.GetCurrentDirectory()"/>
    string GetCurrentDirectory();
}

/// <summary> Wrapper for <see cref="File"/>. </summary>
internal interface IFile
{
    /// <inheritdoc cref="File.Exists(string)"/>
    bool Exists(string path);

    /// <inheritdoc cref="File.ReadAllText(string)"/>
    string ReadAllText(string path);

    /// <inheritdoc cref="File.GetLastWriteTime(string)"/>
    DateTime GetLastWriteTime(string path);

    /// <inheritdoc cref="File.Delete(string)"/>
    void Delete(string path);

    /// <inheritdoc cref="File.Move(string, string)"/>
    void Move(string sourceFileName, string destFileName);
}

/// <summary> Wrapper for <see cref="DirectoryInfo"/>. </summary>
internal interface IDirectoryInfo
{
    /// <inheritdoc cref="DirectoryInfo.EnumerateFiles(string, SearchOption)"/>
    IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly);
}

/// <summary> Wrapper for <see cref="FileInfo"/>. </summary>
internal interface IFileInfo
{
    /// <inheritdoc cref="FileSystemInfo.LastWriteTime"/>
    DateTime LastWriteTime { get; }

    /// <inheritdoc cref="FileSystemInfo.FullName"/>
    string FullName { get; }
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
internal sealed class FileSystemWrapper : IFileSystem
{
    public required ILogger Logger { get; init; }

    /// <inheritdoc />
    public IDirectory Directory => new DirectoryWrapper();

    /// <inheritdoc />
    public IFile File => new FileWrapper(Logger);

    /// <inheritdoc />
    public IDirectoryInfo DirectoryInfo(string path) => new DirectoryInfoWrapper(new DirectoryInfo(path));
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
internal sealed class DirectoryWrapper : IDirectory
{
    /// <inheritdoc />
    public IEnumerable<string> EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);

    /// <inheritdoc />
    public string GetCurrentDirectory() => Directory.GetCurrentDirectory();
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
internal sealed class FileWrapper(ILogger logger) : IFile
{
    /// <inheritdoc />
    public bool Exists(string path) => File.Exists(path);

    /// <inheritdoc />
    public string ReadAllText(string path) => File.ReadAllText(path);

    /// <inheritdoc />
    public DateTime GetLastWriteTime(string path) => File.GetLastWriteTime(path);

    /// <inheritdoc />
    public void Delete(string path) {
        try {
            File.Delete(path);
        } catch (Exception ex) {
            logger.Warning(ex, "Failed to delete temporary file {TempPath}", path);
        }
    }

    /// <inheritdoc />
    public void Move(string tempFilePath, string assemblyPath) {
        int retries = 5;
        while (retries > 0) {
            try {
                File.Move(tempFilePath, assemblyPath);
                logger.Information("Moved patched assembly to {AssemblyPath}", assemblyPath);
                break;
            } catch (IOException ex) {
                if (--retries == 0) {
                    logger.Error(ex, "Failed to move patched assembly from {TempPath} to {AssemblyPath} after retries", tempFilePath, assemblyPath);
                    break;
                }
                logger.Warning(ex, "Retrying move of {TempPath} to {AssemblyPath} (retries left: {Retries})", tempFilePath, assemblyPath, retries);
                Thread.Sleep(200);
            }
        }
    }
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
internal sealed class DirectoryInfoWrapper(DirectoryInfo directoryInfo) : IDirectoryInfo
{
    /// <inheritdoc />
    public IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption) 
        => directoryInfo.EnumerateFiles(searchPattern, searchOption).Select(o => new FileInfoWrapper(o));
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
internal sealed class FileInfoWrapper(FileInfo fileInfo) : IFileInfo
{
    /// <inheritdoc />
    public DateTime LastWriteTime => fileInfo.LastWriteTime;

    /// <inheritdoc />
    public string FullName => fileInfo.FullName;
}
