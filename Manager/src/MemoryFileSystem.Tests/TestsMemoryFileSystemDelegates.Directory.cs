using System;
using FluentAssertions;
using Xunit;

namespace MemoryFileSystem.Tests;

public class TestsMemoryFileSystemDelegatesDirectory
{
    [Fact]
    public void EnumerateDirectories() {
        // Arrange
        var fileSystem = new MemoryFs {
            @"C:\Path\Folder",
            { @"C:\Path\File.txt", "File" }
        };

        // Act
        var actual = fileSystem.Directory.EnumerateDirectories(@"C:\\Path");

        // Assert
        actual.Should().BeEquivalentTo(@"C:\Path\Folder");
    }

    [Fact]
    public void GetCurrentDirectory_ReturnsMemoryFsCurrentDirectory() {
        // Arrange
        var fileSystem = new MemoryFs(@"C:\Current\Path");

        // Act
        var currentDirectory = fileSystem.Directory.GetCurrentDirectory();

        // Assert
        currentDirectory.Should().Be(@"C:\Current\Path");
    }

    [Fact]
    public void GetCurrentDirectory_ThrowsForNonMemoryFs() {
        // Arrange
        var fileSystem = new MemoryZip();

        // Act
        var act = () => fileSystem.Directory.GetCurrentDirectory();

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"Only {typeof(MemoryFs)} supports concept of '{nameof(MemoryFs.CurrentDirectory)}'.");
    }
}
