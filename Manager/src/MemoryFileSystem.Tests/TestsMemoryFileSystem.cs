//using System.IO;
//using System.Linq;
//using FluentAssertions;
//using NSubstitute;
//using Railroader.ModManager.Services.Wrappers.FileSystem;
//using Xunit;

//namespace MemoryFileSystem.Tests;

//public class TestsMemoryFileSystem
//{
//    [Fact]
//    public void Constructor_CreateCorrectMocks() {
//        // Arrange
//        var fileSystem = Substitute.For<IMemoryFileSystem>();
        
//        // Act
//        var sut = new MemoryFileSystem(fileSystem);

//        // Assert
//        sut.DirectoryInfo("C:\\").Should().BeOfType(Substitute.For<IDirectoryInfo>().GetType());
//        sut.ZipFile.Should().BeOfType(Substitute.For<IZipFile>().GetType());
//    }

//    [Fact]
//    public void Constructor_MockReturnsCorrectValues() {
//        // Arrange
//        var fileSystem = Substitute.For<IMemoryFileSystem>();
//        var sut        = new MemoryFileSystem(fileSystem);

//        // Act
//        var mock = sut.Mock();
        
//        // Assert
//        mock.DirectoryInfo("C:\\").Should().NotBeNull();
//        mock.ZipFile.Should().Be(sut.ZipFile);
//    }

//    [Fact]
//    public void Mock_DirectoryInfo_ReturnsCorrectFullName() {
//        // Arrange
//        var fileSystem = new MemoryFileSystemBaseImpl {
//            { @"C:\Test\Path\File.txt", "Content" }
//        };
//        var sut  = new MemoryFileSystem(fileSystem);
//        var mock = sut.Mock();

//        var directoryInfo = mock.DirectoryInfo(@"C:\Test\Path");
//        // Act
//        var files = directoryInfo.EnumerateFiles("*.*").ToArray();

//        // Assert
//        files.Should().HaveCount(1);
//        files[0].FullName.Should().Be(@"c:\test\path\file.txt");
//    }

//    private sealed class MemoryFileSystemBaseImpl : MemoryFileSystemBase
//    {
//        public override string NormalizePath(string path) => path.ToLower();

//        protected override string? GetParentPath(string path) => Path.GetDirectoryName(path);
//    }
//}
