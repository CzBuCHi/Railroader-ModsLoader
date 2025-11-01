using System.IO;
using FluentAssertions;
using MemoryFileSystem.Types;
using Xunit;

namespace MemoryFileSystem.Tests;

public class TestsMemoryFileSystemDelegatesZipFile
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExtractToDirectory_ThrowWhenSourceNotFoundOrDirectory(bool isDirectory) {
        // Arrange
        var fileSystem = new MemoryFs();
        if (isDirectory) {
            fileSystem.Add(@"C:\Source.zip");
        }

        // Act
        var act = () => fileSystem.ZipFile.ExtractToDirectory(@"C:\Source.zip", @"C:\target");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"Zip file 'C:\Source.zip' not found.");
    }

    [Fact]
    public void ExtractToDirectory_ThrowWhenSourceNotZip() {
        // Arrange
        var fileSystem = new MemoryFs();
        fileSystem.Add(@"C:\Source.zip", [1, 2, 3]);

        // Act
        var act = () => fileSystem.ZipFile.ExtractToDirectory(@"C:\Source.zip", @"C:\target");

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage(@"Failed to deserialize zip contents for 'c:\source.zip'.");
    }

    [Fact]
    public void ExtractNestedZipFile_CreatesCorrectEntries() {
        // Arrange
        var zipFile = new MemoryZip();
        zipFile.Add(@"Path\In\Zip\File.txt", [1, 2, 3]);

        var fileSystem = new MemoryFs();
        fileSystem.Add(@"C:\Real\Path\File.zip", zipFile);

        // Act
        fileSystem.ZipFile.ExtractToDirectory(@"C:\Real\Path\File.zip", @"C:\Real\Path\Dest");

        // Assert
        fileSystem.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\"),
            new MemoryEntry(@"C:\Real"),
            new MemoryEntry(@"C:\Real\Path"),
            new MemoryEntry(@"C:\Real\Path\Dest"),
            new MemoryEntry(@"C:\Real\Path\Dest\Path"),
            new MemoryEntry(@"C:\Real\Path\Dest\Path\In"),
            new MemoryEntry(@"C:\Real\Path\Dest\Path\In\Zip"),
            new MemoryEntry(@"C:\Real\Path\Dest\Path\In\Zip\File.txt", [1, 2, 3]),
            new MemoryEntry(@"C:\Real\Path\File.zip", zipFile.GetBytes())
        ]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OpenRead_ThrowWhenSourceNotFoundOrDirectory(bool isDirectory) {
        // Arrange
        var fileSystem = new MemoryFs();
        if (isDirectory) {
            fileSystem.Add(@"C:\Source.zip");
        }

        // Act
        var act = () => fileSystem.ZipFile.OpenRead(@"C:\Source.zip");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"Zip file 'C:\Source.zip' not found.");
    }

    [Fact]
    public void OpenRead_ReturnsCorrectZipArchive() {
        // Arrange
        var zipFile = new MemoryZip();
        zipFile.Add(@"Path\In\Zip\File.txt", [1, 2, 3]);

        var fileSystem = new MemoryFs();
        fileSystem.Add(@"C:\Real\Path\File.zip", zipFile);

        // Act
        var zipArchive = fileSystem.ZipFile.OpenRead(@"C:\Real\Path\File.zip");

        // Assert
        zipArchive.Should().NotBeNull();
        zipArchive.Entries.Should().HaveCount(1);
        var entry = zipArchive.GetEntry("Path/In/Zip/File.txt");
        entry.Should().NotBeNull();
        entry.Name.Should().Be("File.txt");
        entry.Open().Should().BeOfType<MemoryStream>().Which.ToArray().Should().BeEquivalentTo([1, 2, 3]);
    }
}
