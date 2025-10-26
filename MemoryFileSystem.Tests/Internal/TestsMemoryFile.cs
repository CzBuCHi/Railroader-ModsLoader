using System;
using System.IO;
using System.Text;
using FluentAssertions;
using MemoryFileSystem.Internal;
using MemoryFileSystem.Types;
using NSubstitute;
using Xunit;

namespace MemoryFileSystem.Tests.Internal;

public sealed class TestsMemoryFile
{
    private IMemoryFileSystem CreateMemoryFileSystem(EntryDictionary entries) {
        var fileSystem = Substitute.For<IMemoryFileSystem>();
        fileSystem.Items.Returns(entries);
        fileSystem.NormalizePath(Arg.Any<string>()).Returns(o => o.Arg<string>().ToLower());
        return fileSystem;
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("Folder")]
    [InlineData("File")]
    public void Exists(string? type) {
        // Arrange
        var entries = new EntryDictionary();
        switch (type) {
            case "Folder": entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path")); break;
            case "File":   entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path", [1, 2, 3])); break;
        }

        var fileSystem = CreateMemoryFileSystem(entries);
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        var actual = sut.Exists(@"C:\path");

        // Assert
        actual.Should().Be(type == "File");
    }

    [Fact]
    public void ReadAllText_WhenValid() {
        // Arrange
        var entries = new EntryDictionary();
        entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path", Encoding.UTF8.GetBytes("Content")));
        var fileSystem = CreateMemoryFileSystem(entries);
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        var actual = sut.ReadAllText(@"C:\path");

        // Assert
        actual.Should().Be("Content");
    }

    [Fact]
    public void ReadAllText_ThrowsWhenException() {
        // Arrange
        var exception = new Exception();
        var entries   = new EntryDictionary();
        entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path", exception));
        var fileSystem = CreateMemoryFileSystem(entries);
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        var act = () => sut.ReadAllText(@"C:\path");

        // Assert
        act.Should().Throw<Exception>().Which.Should().Be(exception);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadAllText_ThrowsWhenNotFoundOrDirectory(bool isDirectory) {
        // Arrange
        var entries = new EntryDictionary();
        if (isDirectory) {
            entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path"));
        }

        var fileSystem = CreateMemoryFileSystem(entries);
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        var act = () => sut.ReadAllText(@"C:\path");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"File not found: c:\path");
    }

    [Fact]
    public void GetLastWriteTime_WhenValid() {
        // Arrange
        var entries = new EntryDictionary();
        entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path", Encoding.UTF8.GetBytes("Content")));
        var fileSystem = CreateMemoryFileSystem(entries);
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        var actual = sut.GetLastWriteTime(@"C:\path");

        // Assert
        actual.Should().Be(MemoryEntry.DefaultLastWriteTime);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetLastWriteTime_ThrowsWhenNotFoundOrDirectory(bool isDirectory) {
        // Arrange
        var entries = new EntryDictionary();
        if (isDirectory) {
            entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path"));
        }

        var fileSystem = CreateMemoryFileSystem(entries);
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        var act = () => sut.GetLastWriteTime(@"C:\path");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"File not found: c:\path");
    }

    [Fact]
    public void Delete_WhenValid() {
        // Arrange
        var entries = new EntryDictionary();
        entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path", Encoding.UTF8.GetBytes("Content")));
        var fileSystem = CreateMemoryFileSystem(entries);
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        sut.Delete(@"C:\path");

        // Assert
        entries.Should().BeEmpty();
    }

    [Fact]
    public void Delete_DoNothingWhenNotFound() {
        // Arrange
        var entries = new EntryDictionary();
        entries.TryAdd(@"c:\foo", new MemoryEntry(@"c:\foo"));

        var fileSystem = CreateMemoryFileSystem(entries);
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        sut.Delete(@"C:\path");

        // Assert
        entries.Should().HaveCount(1);
        entries.Should().ContainKey(@"c:\foo").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"c:\foo"));
    }

    [Fact]
    public void Delete_ThrowsWhenDirectory() {
        // Arrange
        var entries = new EntryDictionary();
        entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path"));

        var fileSystem = CreateMemoryFileSystem(entries);
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        var act = () => sut.Delete(@"C:\path");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(@"Entry at c:\path is directory.");
    }

    [Fact]
    public void Delete_ThrowsWhenLocked() {
        // Arrange
        var entries = new EntryDictionary();
        entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path", false, MemoryEntry.DefaultLastWriteTime, null, null, true));

        var fileSystem = CreateMemoryFileSystem(entries);
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        var act = () => sut.Delete(@"C:\path");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(@"File 'c:\path' is locked.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Move_WhenSourceNotFoundOrDirectory(bool isDirectory) {
        // Arrange
        var entries = new EntryDictionary();
        if (isDirectory) {
            entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path"));
        }

        var fileSystem = CreateMemoryFileSystem(entries);
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        var act = () => sut.Move(@"C:\path", @"C:\target");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"Source file not found: 'C:\path'.");
    }

    [Fact]
    public void Move_WhenDestinationExists() {
        // Arrange
        var entries = new EntryDictionary();
        entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path", [1, 2, 3]));
        entries.TryAdd(@"c:\target", new MemoryEntry(@"c:\target", [1, 2, 3]));


        var fileSystem = CreateMemoryFileSystem(entries);
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        var act = () => sut.Move(@"C:\path", @"C:\target");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(@"Destination path already exists: 'C:\target'.");
    }

    [Fact]
    public void Move_WhenSourceLocked() {
        // Arrange
        var entries = new EntryDictionary();
        entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path", false, MemoryEntry.DefaultLastWriteTime, null, null, true));

        var fileSystem = CreateMemoryFileSystem(entries);
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        var act = () => sut.Move(@"C:\path", @"C:\target");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(@"File 'c:\path' is locked.");
    }

    [Fact]
    public void Move_WhenValid() {
        // Arrange
        var entries = new EntryDictionary();
        entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path", [1, 2, 3]));

        var fileSystem = CreateMemoryFileSystem(entries);
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        sut.Move(@"C:\path", @"C:\target");

        // Assert
        entries.Should().HaveCount(1);
        entries.Should().ContainKey(@"c:\target").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"c:\target", [1, 2, 3]));
    }

    [Fact]
    public void Create_WhenAddThrows() {
        // Arrange
        var entries = new EntryDictionary();
        entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path", [1, 2, 3]));

        var fileSystem = CreateMemoryFileSystem(entries);
        fileSystem.When(o => o.Add(Arg.Any<string>(), Arg.Any<byte[]>())).Throw<InvalidOperationException>();

        var sut = new MemoryFile(fileSystem).Mock();

        // Act
        var act = () => sut.Create(@"C:\path");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_WhenSucceed() {
        // Arrange
        var fileSystem = new MemoryFs();
        var sut        = new MemoryFile(fileSystem).Mock();

        // Act
        var actual = sut.Create(@"C:\path");

        // Assert
        actual.Should().BeOfType<MemoryFileStream>();

        fileSystem.Items.Should().HaveCount(2);
        fileSystem.Items.Should().ContainKey(@"C:\").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"C:\"));
        fileSystem.Items.Should().ContainKey(@"C:\path").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"C:\path", []));

        actual.Write([1, 2, 3], 0, 3);
        actual.Dispose();

        fileSystem.Items.Should().ContainKey(@"C:\path").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"C:\path", [1, 2, 3]));
    }
}
