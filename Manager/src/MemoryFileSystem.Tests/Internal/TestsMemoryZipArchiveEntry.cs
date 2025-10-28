using System.IO;
using FluentAssertions;
using MemoryFileSystem.Internal;
using Xunit;

namespace MemoryFileSystem.Tests.Internal;

public sealed class TestsMemoryZipArchiveEntry
{
    [Fact]
    public void Constructor_SetProperties() {
        // Arrange
        var entry = new MemoryEntry("path/file.txt", [1, 2, 3]);

        // Act
        var sut = new MemoryZipArchiveEntry(entry).Mock();

        // Assert
        sut.FullName.Should().Be("path/file.txt");
        sut.Name.Should().Be("file.txt");
    }

    [Fact]
    public void Open_OpensStreamWithContent() {
        // Arrange
        var entry = new MemoryEntry("path/file.txt", [1, 2, 3]);
        var sut   = new MemoryZipArchiveEntry(entry).Mock();

        // Act
        var actual = sut.Open();

        // Assert
        var stream = actual.Should().BeOfType<MemoryStream>().Which;
        stream.ToArray().Should().BeEquivalentTo([1, 2, 3]);
    }
}
