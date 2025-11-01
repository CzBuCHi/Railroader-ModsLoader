using System;
using FluentAssertions;
using MemoryFileSystem2.Types;
using Xunit;

namespace MemoryFileSystem2.Tests;

public class TestsMemoryZip
{
    [Fact]
    public void Constructor_CreateEmptyZip()
    {
        // Act
        var sut = new MemoryZip();

        // Assert
        sut.Items.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_DeserializeEntriesFromByteArray()
    {
        // Arrange
        var zip = new MemoryZip();
        zip.Add("Path/To/File.txt", [1, 2, 3]);
        var bytes = zip.GetBytes();

        // Act
        var sut = new MemoryZip(bytes);

        // Assert
        sut.Items.Should().HaveCount(3);
        sut.Items.Should().ContainKey("Path").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("Path"));
        sut.Items.Should().ContainKey("Path/To").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("Path/To"));
        sut.Items.Should().ContainKey("Path/To/File.txt").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("Path/To/File.txt", [1, 2, 3]));
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("")]
    public void NormalizePath_ThrowWhenEmpty(string? path)
    {
        // Arrange
        var sut = new MemoryZip();

        // Act
        var act = () => sut.NormalizePath(path!);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Path cannot be null or empty.");
    }

    [Fact]
    public void NormalizePath_ThrowWhenAbsolute()
    {
        // Arrange
        var sut = new MemoryZip();

        // Act
        var act = () => sut.NormalizePath("C:\\Path");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Zip file do not support absolute paths.");
    }

    [Fact]
    public void NormalizePath_WhenValidPath()
    {
        // Arrange
        var sut = new MemoryZip();

        // Act
        var actual = sut.NormalizePath(@"\Path\To\File.txt");

        // Assert
        actual.Should().Be("Path/To/File.txt");
    }
}
