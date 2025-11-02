using System.Linq;
using MemoryFileSystem.Internal;
using Shouldly;
using Xunit;

namespace MemoryFileSystem.Tests.Internal;

public sealed class TestsMemoryZipArchive
{
    [Fact]
    public void Entries() {
        // Arrange
        var memoryZip = new MemoryZip {
            "Directory",
            { "File.txt", "Content" }
        };
        var sut = new MemoryZipArchive(memoryZip);

        // Act
        var actual = sut.Entries.ToArray();

        // Assert
        actual.Length.ShouldBe(1);
    }

    [Fact]
    public void GetEntry_ReturnsCorrectZipArchiveEntry() {
        // Arrange
        var memoryZip = new MemoryZip {
            "Directory",
            { "Path/File.txt", "Content" }
        };
        var sut = new MemoryZipArchive(memoryZip);

        // Act
        var actual = sut.GetEntry("Path/File.txt");

        // Assert
        actual.ShouldNotBeNull();
        actual.FullName.ShouldBe("Path/File.txt");
        actual.Name.ShouldBe("File.txt");
    }

    [Fact]
    public void GetEntry_ReturnNull_WhenNotFound() {
        // Arrange
        var memoryZip = new MemoryZip();
        var sut       = new MemoryZipArchive(memoryZip);

        // Act
        var actual = sut.GetEntry("Path/File.txt");

        // Assert
        actual.ShouldBeNull();
    }
}
