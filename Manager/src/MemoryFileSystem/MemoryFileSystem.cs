using MemoryFileSystem.Internal;
using NSubstitute;
using Railroader.ModManager.Services.Wrappers.FileSystem;

namespace MemoryFileSystem;

public sealed class MemoryFileSystem(IMemoryFileSystem fileSystem) : IFileSystem
{
    public IDirectory Directory { get; } = new MemoryDirectory(fileSystem).Mock();
    public IFile      File      { get; } = new MemoryFile(fileSystem).Mock();
    public IDirectoryInfo DirectoryInfo(string path) => new MemoryDirectoryInfo(fileSystem, path).Mock();
    public IZipFile ZipFile { get; } = new MemoryZipFile(fileSystem).Mock();

    public IFileSystem Mock() {
        var mock = Substitute.For<IFileSystem>();
        mock.Directory.Returns(_ => Directory);
        mock.File.Returns(_ => File);
        mock.DirectoryInfo(Arg.Any<string>()).Returns(o => DirectoryInfo(o.Arg<string>()));
        mock.ZipFile.Returns(_ => ZipFile);
        return mock;
    }
}
