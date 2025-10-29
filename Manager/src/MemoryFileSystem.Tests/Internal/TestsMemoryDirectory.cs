//using System;
//using FluentAssertions;
//using MemoryFileSystem.Internal;
//using NSubstitute;
//using Xunit;

//namespace MemoryFileSystem.Tests.Internal;

//public sealed class TestsMemoryDirectory
//{
//    [Fact]
//    public void EnumerateDirectories() {
//        // Arrange
//        MemoryEntry[] entries = [
//            new(@"C:\Path\Folder"),
//            new(@"C:\Path\File.txt", [1, 2, 3])
//        ];

//        var fileSystem = Substitute.For<IMemoryFileSystem>();
//        fileSystem.Enumerate(@"C:\\Path", "*.*").Returns(entries);

//        var sut = new MemoryDirectory(fileSystem).Mock();

//        // Act
//        var actual = sut.EnumerateDirectories(@"C:\\Path");

//        // Assert
//        fileSystem.Received().Enumerate(@"C:\\Path", "*.*");

//        actual.Should().BeEquivalentTo(@"C:\Path\Folder");
//    }

//    [Fact]
//    public void GetCurrentDirectory_ReturnsMemoryFsCurrentDirectory() {
//        // Arrange
//        var fileSystem = new MemoryFs(@"C:\Current\Path");
//        var sut        = new MemoryDirectory(fileSystem).Mock();

//        // Act
//        var currentDirectory = sut.GetCurrentDirectory();

//        // Assert
//        currentDirectory.Should().Be(@"C:\Current\Path");
//    }

//    [Fact]
//    public void GetCurrentDirectory_ThrowsForNonMemoryFs() {
//        // Arrange
//        var fileSystem = Substitute.For<IMemoryFileSystem>();
//        var sut        = new MemoryDirectory(fileSystem).Mock();

//        // Act
//        var act = () => sut.GetCurrentDirectory();

//        // Assert
//        act.Should().Throw<InvalidOperationException>()
//           .WithMessage("Only MemoryFs support concept of 'CurrentDirectory'.");
//    }
//}