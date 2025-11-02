using System.IO;
using MemoryFileSystem.Internal;
using MemoryFileSystem.Types;
using Shouldly;
using Xunit;

namespace MemoryFileSystem.Tests.Internal;

public sealed class TestsMemoryZipArchiveEntry
{
    [Fact]
    public void FullName() {
        // Arrange
        var entry = new MemoryEntry(@"C:\Path\File.txt", [1, 2, 3]);
        var sut   = new MemoryZipArchiveEntry(entry);

        // Act
        var actual = sut.FullName;

        // Assert
        actual.ShouldBe(@"C:\Path\File.txt");
    }

    [Fact]
    public void Name() {
        // Arrange
        var entry = new MemoryEntry(@"C:\Path\File.txt", [1, 2, 3]);
        var sut   = new MemoryZipArchiveEntry(entry);

        // Act
        var actual = sut.Name;

        // Assert
        actual.ShouldBe("File.txt");
    }

    [Fact]
    public void Open() {
        // Arrange
        byte[] content = [1, 2, 3];
        var    entry   = new MemoryEntry(@"C:\Path\File.txt", content);
        var    sut     = new MemoryZipArchiveEntry(entry);

        // Act
        var actual = sut.Open();

        // Assert
        var stream = actual.ShouldBeOfType<MemoryStream>();
        stream.ToArray().ShouldBeEquivalentTo(content);
    }
}
