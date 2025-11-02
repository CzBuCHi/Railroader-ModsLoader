using System;
using MemoryFileSystem.Tests.TestExtensions;
using MemoryFileSystem.Types;
using Shouldly;
using Xunit;

namespace MemoryFileSystem.Tests;

public class TestsMemoryZip
{
    [Fact]
    public void Constructor_CreateEmptyZip() {
        // Act
        var sut = new MemoryZip();

        // Assert
        sut.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_DeserializeEntriesFromByteArray() {
        // Arrange
        var zip = new MemoryZip();
        zip.Add("Path/To/File.txt", [1, 2, 3]);
        var bytes = zip.GetBytes();

        // Act
        var sut = new MemoryZip(bytes);

        // Assert
        sut.Items.Count.ShouldBe(3);
        sut.Items.ShouldContainKeyWhereValue("Path", o => o.ShouldBeEquivalentTo(new MemoryEntry("Path")));
        sut.Items.ShouldContainKeyWhereValue("Path/To", o => o.ShouldBeEquivalentTo(new MemoryEntry("Path/To")));
        sut.Items.ShouldContainKeyWhereValue("Path/To/File.txt", o => o.ShouldBeEquivalentTo(new MemoryEntry("Path/To/File.txt", [1, 2, 3])));
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("")]
    public void NormalizePath_ThrowWhenEmpty(string? path) {
        // Arrange
        var sut = new MemoryZip();

        // Act & Assert
        Should.Throw<ArgumentException>(() => sut.NormalizePath(path!))
              .Message.ShouldBe("Path cannot be null or empty.");
    }

    [Fact]
    public void NormalizePath_ThrowWhenAbsolute() {
        // Arrange
        var sut = new MemoryZip();

        // Act & Assert
        Should.Throw<ArgumentException>(() => sut.NormalizePath("C:\\Path"))
              .Message.ShouldBe("Zip file do not support absolute paths.");
    }

    [Fact]
    public void NormalizePath_WhenValidPath() {
        // Arrange
        var sut = new MemoryZip();

        // Act
        var actual = sut.NormalizePath(@"\Path\To\File.txt");

        // Assert
        actual.ShouldBe("Path/To/File.txt");
    }
}
