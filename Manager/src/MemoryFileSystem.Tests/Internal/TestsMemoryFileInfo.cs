using System.IO;
using MemoryFileSystem.Internal;
using MemoryFileSystem.Types;
using NSubstitute;
using Shouldly;
using Xunit;

namespace MemoryFileSystem.Tests.Internal;

public sealed class TestsMemoryFileInfo
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LastWriteTime_ThrowsWhenNotFoundOrDirectory(bool isDirectory) {
        // Arrange
        var fileSystem = new MemoryFs();
        if (isDirectory) {
            fileSystem.Add(@"C:\path");
        }

        var sut = new MemoryFileInfo(fileSystem, @"C:\path");

        // Act & Assert
        Should.Throw<FileNotFoundException>(() => sut.LastWriteTime).Message.ShouldBe(@"File not found: 'C:\path'.");
    }

    [Fact]
    public void LastWriteTime_ForFile() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\path", "Content" }
        };

        var sut = new MemoryFileInfo(fileSystem, @"C:\path");

        // Act
        var actual = sut.LastWriteTime;

        // Assert
        actual.ShouldBe(MemoryEntry.DefaultLastWriteTime);
    }

    [Fact]
    public void FullName() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\path", "Content" }
        };

        var sut = new MemoryFileInfo(fileSystem, @"C:\path");

        // Act
        var actual = sut.FullName;

        // Assert
        actual.ShouldBe(@"C:\path");
    }

    [Fact]
    public void MoveTo() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\path", "Content" }
        };

        var sut = new MemoryFileInfo(fileSystem, @"C:\path");

        // Act
        sut.MoveTo(@"C:\target");

        // Assert
        fileSystem.File.Move.Received().Invoke(@"C:\path", @"C:\target");
        sut.FullName.ShouldBe(@"C:\target");
    }
}
