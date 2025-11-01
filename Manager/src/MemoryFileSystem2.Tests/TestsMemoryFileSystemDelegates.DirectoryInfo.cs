using System.Linq;
using FluentAssertions;
using Xunit;

namespace MemoryFileSystem2.Tests;

public class TestsMemoryFileSystemDelegatesDirectoryInfo
{
    [Fact]
    public void EnumerateFiles() {
        // Arrange
        var fileSystem = new MemoryFs {
            @"C:\Path\Folder",
            { @"C:\Path\File.txt", "File" }
        };

        // Act
        var files = fileSystem.DirectoryInfo(@"C:\\Path").EnumerateFiles("*.*").ToArray();

        // Assert
        files.Should().HaveCount(1);
        files[0].FullName.Should().Be(@"C:\Path\File.txt");
    }
}
