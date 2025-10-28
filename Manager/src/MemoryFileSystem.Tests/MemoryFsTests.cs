using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Xunit;

namespace MemoryFileSystem.Tests;

public sealed class MemoryFsTests
{
    [Theory]
    [InlineData(null!, @"C:\")]
    [InlineData("D:\\Foo", "D:\\Foo")]
    public void Constructor_SetsDefaultCurrentDirectory(string? input, string expected) {
        
        // Arrange & Act
        var sut = new MemoryFs(input);
   
        // Assert
        sut.CurrentDirectory.Should().Be(expected);
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void CurrentDirectory_SetNormalizesPath() {
        // Arrange
        var sut = new MemoryFs();

        // Act
        sut.CurrentDirectory = @"C:\Test\Path\";

        // Assert
        sut.CurrentDirectory.Should().Be(@"C:\Test\Path");
    }

    [Fact]
    public void CurrentDirectory_SetEmptyPath_ThrowsArgumentException() {
        // Arrange
        var sut = new MemoryFs();

        // Act
        Action act = () => sut.CurrentDirectory = "";

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Path cannot be null or empty.*");
    }

    [Fact]
    public void CurrentDirectory_SetInvalidPathChars_ThrowsArgumentException() {
        // Arrange
        var sut = new MemoryFs();

        // Act
        Action act = () => sut.CurrentDirectory = @"C:\Test\*Path";

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Illegal characters in path.");
    }
}