using System.IO;
using System.Linq;
using FluentAssertions;
using MemoryFileSystem2.Types;
using Xunit;

namespace MemoryFileSystem2.Tests;

public class TestsMemoryFileSystemDelegatesFileInfo
{
    [Fact]
    public void LastWriteTime() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\Path\File.txt", "File" }
        };
        var file = fileSystem.DirectoryInfo(@"C:\\Path").EnumerateFiles("*.*").First();

        // Act
        var actual = file.LastWriteTime;
        // Assert
        
        actual.Should().Be(MemoryEntry.DefaultLastWriteTime);
    }

    [Fact]
    public void LastWriteTime_WhenNotFound() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\Path\File.txt", "File" }
        };
        var file = fileSystem.DirectoryInfo(@"C:\\Path").EnumerateFiles("*.*").First();
        fileSystem.Items.Clear();

        // Act
        var act = () => file.LastWriteTime;

        // Assert
        act.Should().Throw<FileNotFoundException>()
           .WithMessage(@"File not found: C:\Path\File.txt");
    }

    [Fact]
    public void FullName() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\Path\File.txt", "File" }
        };
        var file = fileSystem.DirectoryInfo(@"C:\\Path").EnumerateFiles("*.*").First();

        // Act
        var actual = file.FullName;
        // Assert
        
        actual.Should().Be(@"C:\Path\File.txt");
    }

    [Fact]
    public void MoveTo() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\Path\File.txt", "File" }
        };
        var file = fileSystem.DirectoryInfo(@"C:\\Path").EnumerateFiles("*.*").First();

        // Act
        file.MoveTo(@"C:\Path\Target.txt");

        // Assert
        fileSystem.Items.Should().NotContainKey(@"C:\Path\File.txt");
        fileSystem.Items.Should().ContainKey(@"C:\Path\Target.txt");
    }
}
