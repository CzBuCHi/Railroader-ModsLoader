using System.IO;
using MemoryFileSystem.Types;
using Shouldly;
using Xunit;

namespace MemoryFileSystem.Tests;

public class TestsMemoryFileSystemDelegatesFileInfo
{
    [Fact]
    public void LastWriteTime() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\Path\File.txt", "File" }
        };
        var file = fileSystem.FileInfo(@"C:\Path\File.txt");

        // Act
        var actual = file.LastWriteTime;

        // Assert
        actual.ShouldBe(MemoryEntry.DefaultLastWriteTime);
    }

    [Fact]
    public void LastWriteTime_WhenNotFound() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\Path\File.txt", "File" }
        };
        var file = fileSystem.FileInfo(@"C:\Path\File.txt");
        fileSystem.Items.Clear();

        // Act & Assert
        Should.Throw<FileNotFoundException>(() => file.LastWriteTime)
              .Message.ShouldBe(@"File not found: 'C:\Path\File.txt'.");
    }

    [Fact]
    public void FullName() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\Path\File.txt", "File" }
        };
        var file = fileSystem.FileInfo(@"C:\Path\File.txt");

        // Act
        var actual = file.FullName;

        // Assert
        actual.ShouldBe(@"C:\Path\File.txt");
    }

    [Fact]
    public void MoveTo() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\Path\File.txt", "File" }
        };
        var file = fileSystem.FileInfo(@"C:\Path\File.txt");

        // Act
        file.MoveTo(@"C:\Path\Target.txt");

        // Assert
        fileSystem.Items.ShouldNotContainKey(@"C:\Path\File.txt");
        fileSystem.Items.ShouldContainKey(@"C:\Path\Target.txt");
    }
}
