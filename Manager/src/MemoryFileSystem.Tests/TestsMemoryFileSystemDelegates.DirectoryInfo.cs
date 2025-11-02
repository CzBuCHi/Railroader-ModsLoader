using System.Linq;
using Shouldly;
using Xunit;

namespace MemoryFileSystem.Tests;

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
        files.Length.ShouldBe(1);
        files[0].FullName.ShouldBe(@"C:\Path\File.txt");
    }
}
