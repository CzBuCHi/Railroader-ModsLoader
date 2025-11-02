using System;
using MemoryFileSystem.Tests.TestExtensions;
using MemoryFileSystem.Types;
using Shouldly;
using Xunit;

namespace MemoryFileSystem.Tests;

public class TestsMemoryFs
{
    [Fact]
    public void Constructor_SetDefaultCurrentDirectory() {
        // Act
        var sut = new MemoryFs();

        // Assert
        sut.CurrentDirectory.ShouldBe("C:\\");
    }

    [Fact]
    public void Constructor_SetCurrentDirectoryAddsAlParents() {
        // Act
        var sut = new MemoryFs(@"D:\Path\To\Current");

        // Assert
        sut.CurrentDirectory.ShouldBe(@"D:\Path\To\Current");
        sut.Items.ShouldContainKeyWhereValue(@"D:\", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"D:\")));
        sut.Items.ShouldContainKeyWhereValue(@"D:\Path", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"D:\Path")));
        sut.Items.ShouldContainKeyWhereValue(@"D:\Path\To", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"D:\Path\To")));
        sut.Items.ShouldContainKeyWhereValue(@"D:\Path\To\Current", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"D:\Path\To\Current")));
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("")]
    public void NormalizePath_ThrowWhenEmpty(string? path) {
        // Arrange
        var sut = new MemoryFs();

        // Act & Assert
        Should.Throw<ArgumentException>(() => sut.NormalizePath(path!))
              .Message.ShouldBe("Path cannot be null or empty.");
    }

    [Fact]
    public void NormalizePath_RelativePath_FromCurrent() {
        // Arrange
        var sut = new MemoryFs(@"C:\Current");

        // Act
        var actual = sut.NormalizePath(@"Relative\Path.txt");

        // Assert
        actual.ShouldBe(@"C:\Current\Relative\Path.txt");
    }

    [Fact]
    public void NormalizePath_TrimTrailingSlash() {
        // Arrange
        var sut = new MemoryFs(@"C:\Current");

        // Act
        var actual = sut.NormalizePath(@"C:\Absolute\Path\");

        // Assert
        actual.ShouldBe(@"C:\Absolute\Path");
    }

    [Fact]
    public void NormalizePath_KeepTrailingSlashOnRoot() {
        // Arrange
        var sut = new MemoryFs(@"C:\Current");

        // Act
        var actual = sut.NormalizePath(@"C:\");

        // Assert
        actual.ShouldBe(@"C:\");
    }
}
