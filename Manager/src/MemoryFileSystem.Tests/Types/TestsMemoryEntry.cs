using System;
using MemoryFileSystem.Types;
using Shouldly;
using Xunit;

namespace MemoryFileSystem.Tests.Types;

public class TestsMemoryEntry
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Folder(bool locked) {
        // Actual
        var date = DateTime.Now;

        // Act
        var sut = new MemoryEntry(@"C:\Folder", date, locked);

        // Assert
        sut.Path.ShouldBe(@"C:\Folder");
        sut.IsDirectory.ShouldBeTrue();
        sut.LastWriteTime.ShouldBe(date);
        sut.Content.ShouldBeNull();
        sut.ReadException.ShouldBeNull();
        sut.Locked.ShouldBe(locked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BinaryFile(bool locked) {
        // Arrange
        var    date    = DateTime.Now;
        byte[] content = [1, 2, 3];

        // Act
        var sut = new MemoryEntry(@"C:\File.txt", content, date, locked);

        // Assert
        sut.Path.ShouldBe(@"C:\File.txt");
        sut.IsDirectory.ShouldBeFalse();
        sut.LastWriteTime.ShouldBe(date);
        sut.Content.ShouldBeEquivalentTo(content);
        sut.ReadException.ShouldBeNull();
        sut.Locked.ShouldBe(locked);
    }

    [Fact]
    public void UnreadableFile() {
        // Arrange
        var date      = DateTime.Now;
        var exception = new Exception();

        // Act
        var sut = new MemoryEntry(@"C:\File.txt", exception, date);

        // Assert
        sut.Path.ShouldBe(@"C:\File.txt");
        sut.IsDirectory.ShouldBeFalse();
        sut.LastWriteTime.ShouldBe(date);
        sut.Content.ShouldBeNull();
        sut.ReadException.ShouldBe(exception);
        sut.Locked.ShouldBeFalse();
    }

    [Fact]
    public void CheckLock_ThrowsWhenLocked() {
        // Arrange
        var sut = new MemoryEntry(@"C:\File.txt", [1, 2, 3], MemoryEntry.DefaultLastWriteTime, true);

        // Act & Assert
        Should.Throw<InvalidOperationException>(sut.CheckLock).Message.ShouldBe(@"File 'C:\File.txt' is locked.");
    }
}
