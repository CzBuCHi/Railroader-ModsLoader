using System.Diagnostics.CodeAnalysis;
using Railroader.ModManager.Delegates.System.IO.Compression;

namespace Railroader.ModManager.Delegates.System.IO;

public interface IFileSystem
{
    IDirectoryStatic Directory { get; }
    IFileStatic      File      { get; }
    IZipFileStatic   ZipFile   { get; }
    IDirectoryInfo DirectoryInfo(string path);
    IFileInfo FileInfo(string path);
}

[ExcludeFromCodeCoverage]
public sealed class FileSystem : IFileSystem
{
    public static readonly IFileSystem Instance = new FileSystem();
    
    public IDirectoryStatic Directory { get; } = new DirectoryStatic();
    public IFileStatic      File      { get; } = new FileStatic();
    public IZipFileStatic   ZipFile   { get; } = new ZipFileStatic();
    public IDirectoryInfo DirectoryInfo(string path) =>  new DirectoryInfoWrapper(new(path));
    public IFileInfo FileInfo(string path) => new FileInfoWrapper(new(path));
}
