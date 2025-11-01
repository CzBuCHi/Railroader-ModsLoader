using System;
using FluentAssertions;
using MemoryFileSystem2.Types;
using Xunit;

namespace MemoryFileSystem2.Tests;

public class TestsMemoryFs
{
    [Fact]
    public void Constructor_SetDefaultCurrentDirectory()
    {
        // Act
        var sut = new MemoryFs();

        // Assert
        sut.CurrentDirectory.Should().Be("C:\\");
    }

    [Fact]
    public void Constructor_SetCurrentDirectoryAddsAlParents()
    {
        // Act
        var sut = new MemoryFs(@"D:\Path\To\Current");

        // Assert
        sut.CurrentDirectory.Should().Be(@"D:\Path\To\Current");
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"D:\"),
            new MemoryEntry(@"D:\Path"),
            new MemoryEntry(@"D:\Path\To"),
            new MemoryEntry(@"D:\Path\To\Current")
        ]);
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("")]
    public void NormalizePath_ThrowWhenEmpty(string? path)
    {
        // Arrange
        var sut = new MemoryFs();

        // Act
        var act = () => sut.NormalizePath(path!);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Path cannot be null or empty.");
    }

    [Fact]
    public void NormalizePath_RelativePath_FromCurrent()
    {
        // Arrange
        var sut = new MemoryFs(@"C:\Current");

        // Act
        var actual = sut.NormalizePath(@"Relative\Path.txt");

        // Assert
        actual.Should().Be(@"C:\Current\Relative\Path.txt");
    }

    [Fact]
    public void NormalizePath_TrimTrailingSlash()
    {
        // Arrange
        var sut = new MemoryFs(@"C:\Current");

        // Act
        var actual = sut.NormalizePath(@"C:\Absolute\Path\");

        // Assert
        actual.Should().Be(@"C:\Absolute\Path");
    }

    [Fact]
    public void NormalizePath_KeepTrailingSlashOnRoot()
    {
        // Arrange
        var sut = new MemoryFs(@"C:\Current");

        // Act
        var actual = sut.NormalizePath(@"C:\");

        // Assert
        actual.Should().Be(@"C:\");
    }
}
