//using MemoryFileSystem.Internal;
//using NSubstitute;
//using Railroader.ModManager.Services.Wrappers.FileSystem;

//namespace MemoryFileSystem;

//public sealed class MemoryFileSystem(IMemoryFileSystem fileSystem) : IFileSystem
//{
//    public IDirectoryInfo DirectoryInfo(string path) => new MemoryDirectoryInfo(fileSystem, path).Mock();
//    public IZipFile ZipFile { get; } = new MemoryZipFile(fileSystem).Mock();

//    public IFileSystem Mock() {
//        var mock = Substitute.For<IFileSystem>();
//        mock.DirectoryInfo(Arg.Any<string>()).Returns(o => DirectoryInfo(o.Arg<string>()));
//        mock.ZipFile.Returns(_ => ZipFile);
//        return mock;
//    }
//}
