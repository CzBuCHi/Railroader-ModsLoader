using System.IO;
using MemoryFileSystem.Tests.TestExtensions;
using MemoryFileSystem.Types;
using Shouldly;
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

        // Act & Assert
        Should.Throw<FileNotFoundException>(() => fileSystem.ZipFile.ExtractToDirectory(@"C:\Source.zip", @"C:\target"))
              .Message.ShouldBe(@"Zip file 'C:\Source.zip' not found.");
    }

    [Fact]
    public void ExtractToDirectory_ThrowWhenSourceNotZip() {
        // Arrange
        var fileSystem = new MemoryFs();
        fileSystem.Add(@"C:\Source.zip", [1, 2, 3]);

        // Act & Assert
        Should.Throw<InvalidDataException>(() => fileSystem.ZipFile.ExtractToDirectory(@"C:\Source.zip", @"C:\target"))
              .Message.ShouldBe(@"Failed to deserialize zip contents for 'C:\Source.zip'.");
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
        fileSystem.Items.ShouldContainKeyWhereValue(@"C:\", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"C:\")));
        fileSystem.Items.ShouldContainKeyWhereValue(@"C:\Real", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"C:\Real")));
        fileSystem.Items.ShouldContainKeyWhereValue(@"C:\Real\Path", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"C:\Real\Path")));
        fileSystem.Items.ShouldContainKeyWhereValue(@"C:\Real\Path\Dest", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"C:\Real\Path\Dest")));
        fileSystem.Items.ShouldContainKeyWhereValue(@"C:\Real\Path\Dest\Path", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"C:\Real\Path\Dest\Path")));
        fileSystem.Items.ShouldContainKeyWhereValue(@"C:\Real\Path\Dest\Path\In", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"C:\Real\Path\Dest\Path\In")));
        fileSystem.Items.ShouldContainKeyWhereValue(@"C:\Real\Path\Dest\Path\In\Zip", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"C:\Real\Path\Dest\Path\In\Zip")));
        fileSystem.Items.ShouldContainKeyWhereValue(@"C:\Real\Path\Dest\Path\In\Zip\File.txt", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"C:\Real\Path\Dest\Path\In\Zip\File.txt", [1, 2, 3])));
        fileSystem.Items.ShouldContainKeyWhereValue(@"C:\Real\Path\File.zip", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"C:\Real\Path\File.zip", zipFile.GetBytes())));
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

        // Act & Assert
        Should.Throw<FileNotFoundException>(() => fileSystem.ZipFile.OpenRead(@"C:\Source.zip"))
              .Message.ShouldBe(@"Zip file 'C:\Source.zip' not found.");
    }

    [Fact]
    public void OpenRead_ReturnsCorrectZipArchive() {
        // Arrange
        byte[] content = [1, 2, 3];

        var zipFile = new MemoryZip();
        zipFile.Add(@"Path\In\Zip\File.txt", content);

        var fileSystem = new MemoryFs();
        fileSystem.Add(@"C:\Real\Path\File.zip", zipFile);

        // Act
        var zipArchive = fileSystem.ZipFile.OpenRead(@"C:\Real\Path\File.zip");

        // Assert
        zipArchive.ShouldNotBeNull();
        zipArchive.Entries.Count.ShouldBe(1);
        var entry = zipArchive.GetEntry("Path/In/Zip/File.txt");
        entry.ShouldNotBeNull();
        entry.Name.ShouldBe("File.txt");
        entry.Open().ShouldBeOfType<MemoryStream>().ToArray().ShouldBeEquivalentTo(content);
    }
}
