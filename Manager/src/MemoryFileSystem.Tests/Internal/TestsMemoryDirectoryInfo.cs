//using System.IO;
//using System.Linq;
//using FluentAssertions;
//using MemoryFileSystem.Internal;
//using NSubstitute;
//using Xunit;

//namespace MemoryFileSystem.Tests.Internal;

//public sealed class TestsMemoryDirectoryInfo
//{
//    [Fact]
//    public void EnumerateFiles() {
//        // Arrange
//        MemoryEntry[] entries = [
//            new(@"C:\Path\Folder"),
//            new(@"C:\Path\File.txt", [1, 2, 3])
//        ];

//        var fileSystem = Substitute.For<IMemoryFileSystem>();
//        fileSystem.NormalizePath(Arg.Any<string>()).Returns(o => o.Arg<string>().ToLower());
//        fileSystem.Enumerate(@"c:\path", "*.cs", SearchOption.AllDirectories).Returns(entries);

//        var sut = new MemoryDirectoryInfo(fileSystem, @"C:\Path").Mock();

//        // Act
//        var files = sut.EnumerateFiles("*.cs", SearchOption.AllDirectories).ToArray();

//        // Assert
//        files.Should().HaveCount(1);
//        files[0].FullName.Should().Be(@"c:\path\file.txt");
//    }
//}