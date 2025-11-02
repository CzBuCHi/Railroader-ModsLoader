using System;
using System.Linq;
using Shouldly;
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
        var actual = fileSystem.Directory.EnumerateDirectories(@"C:\\Path").ToArray();

        // Assert
        actual.ShouldBeEquivalentTo(new[] { @"C:\Path\Folder" });
    }

    [Fact]
    public void GetCurrentDirectory_ReturnsMemoryFsCurrentDirectory() {
        // Arrange
        var fileSystem = new MemoryFs(@"C:\Current\Path");

        // Act
        var currentDirectory = fileSystem.Directory.GetCurrentDirectory();

        // Assert
        currentDirectory.ShouldBe(@"C:\Current\Path");
    }

    [Fact]
    public void GetCurrentDirectory_ThrowsForNonMemoryFs() {
        // Arrange
        var fileSystem = new MemoryZip();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => fileSystem.Directory.GetCurrentDirectory())
              .Message.ShouldBe($"Only {typeof(MemoryFs)} supports concept of '{nameof(MemoryFs.CurrentDirectory)}'.");
    }
}
