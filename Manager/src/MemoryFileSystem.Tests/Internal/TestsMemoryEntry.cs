using System;
using System.IO;
using FluentAssertions;
using MemoryFileSystem.Internal;
using Xunit;

namespace MemoryFileSystem.Tests.Internal;

public sealed class TestsMemoryEntry
{
    private static readonly DateTime _Date = new(2000, 1, 2);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_Folder(bool useDefaultLastWriteTime) {
        // Arrange
        DateTime? lastWriteTime = useDefaultLastWriteTime ? null : _Date;

        // Act
        var sut = new MemoryEntry("Path", lastWriteTime);

        // Assert
        sut.Should().BeEquivalentTo(new MemoryEntry("Path", true, lastWriteTime ?? MemoryEntry.DefaultLastWriteTime, null, null, false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_File_Valid(bool useDefaultLastWriteTime) {
        // Arrange
        DateTime? lastWriteTime = useDefaultLastWriteTime ? null : _Date;

        // Act
        var sut = new MemoryEntry("Path", [1, 2, 3], lastWriteTime);

        // Assert
        sut.Should().BeEquivalentTo(new MemoryEntry("Path", false, lastWriteTime ?? MemoryEntry.DefaultLastWriteTime, [1, 2, 3], null, false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_File_Exception(bool useDefaultLastWriteTime) {
        // Arrange
        DateTime? lastWriteTime = useDefaultLastWriteTime ? null : _Date;
        var       exception     = new Exception();

        // Act
        var sut = new MemoryEntry("Path", exception, lastWriteTime);

        // Assert
        sut.Should().BeEquivalentTo(new MemoryEntry("Path", false, lastWriteTime ?? MemoryEntry.DefaultLastWriteTime, null, exception, false));
    }

    [Fact]
    public void ExistingContent_ReturnsContent() {
        // Arrange
        var sut = new MemoryEntry("Path", [1, 2, 3]);

        // Act
        var actual = sut.ExistingContent;

        // Assert
        actual.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void ExistingContent_ThrowsForDirectory() {
        // Arrange
        var sut = new MemoryEntry("Path");

        // Act
        var act = () => sut.ExistingContent;

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("Entry at 'Path' is directory or its content is missing.");
    }

    [Fact]
    public void ExistingContent_ThrowsForMissingContent() {
        // Arrange
        var sut = new MemoryEntry("Path", new Exception());

        // Act
        var act = () => sut.ExistingContent;

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("Entry at 'Path' is directory or its content is missing.");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CheckLock_ThrowsWhenLocked(bool isLocked) {
        // Arrange
        var sut = new MemoryEntry(@"c:\path", false, MemoryEntry.DefaultLastWriteTime, null, null, isLocked);

        // Act
        var act = () => sut.CheckLock();

        // Assert
        if (isLocked) {
            act.Should().Throw<InvalidOperationException>().WithMessage(@"File 'c:\path' is locked.");
        } else {
            act.Should().NotThrow<InvalidOperationException>();
        }
    }
}
