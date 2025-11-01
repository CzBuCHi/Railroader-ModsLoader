using System;
using System.IO;
using FluentAssertions;
using MemoryFileSystem2.Types;
using Xunit;

namespace MemoryFileSystem2.Tests;

public class TestsMemoryFileSystemDelegatesFile
{
    [Theory]
    [InlineData(null!)]
    [InlineData("Folder")]
    [InlineData("File")]
    public void Exists(string? type) {
        // Arrange
        var fileSystem = new MemoryFs();
        switch (type) {
            case "Folder": fileSystem.Add(new MemoryEntry(@"c:\path")); break;
            case "File":   fileSystem.Add(new MemoryEntry(@"c:\path", [1, 2, 3])); break;
        }

        // Act
        var actual = fileSystem.File.Exists(@"C:\path");

        // Assert
        actual.Should().Be(type == "File");
    }

    [Fact]
    public void ReadAllText_WhenValid() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\Path", "Content" }
        };

        // Act
        var actual = fileSystem.File.ReadAllText(@"C:\path");

        // Assert
        actual.Should().Be("Content");
    }

    [Fact]
    public void ReadAllText_ThrowsWhenException() {
        // Arrange
        var exception = new Exception();
        var fileSystem = new MemoryFs {
            { @"C:\Path", exception }
        };

        // Act
        var act = () => fileSystem.File.ReadAllText(@"C:\path");

        // Assert
        act.Should().Throw<Exception>().Which.Should().Be(exception);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadAllText_ThrowsWhenNotFoundOrDirectory(bool isDirectory) {
        // Arrange
        var fileSystem = new MemoryFs();
        if (isDirectory) {
            fileSystem.Add(@"C:\Path");
        }

        // Act
        var act = () => fileSystem.File.ReadAllText(@"C:\path");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"File not found: c:\path");
    }

    [Fact]
    public void GetLastWriteTime_WhenValid() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"c:\path", "Content" }
        };

        // Act
        var actual = fileSystem.File.GetLastWriteTime(@"C:\path");

        // Assert
        actual.Should().Be(MemoryEntry.DefaultLastWriteTime);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetLastWriteTime_ThrowsWhenNotFoundOrDirectory(bool isDirectory) {
        // Arrange
        var fileSystem = new MemoryFs();
        if (isDirectory) {
            fileSystem.Add(@"c:\path");
        }

        // Act
        var act = () => fileSystem.File.GetLastWriteTime(@"C:\path");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"File not found: c:\path");
    }

    [Fact]
    public void Delete_WhenValid() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"c:\path", "Content" }
        };


        // Act
        fileSystem.File.Delete(@"C:\path");

        // Assert
        fileSystem.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Delete_DoNothingWhenNotFound() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\foo", "Content" }
        };

        // Act
        fileSystem.File.Delete(@"C:\path");

        // Assert
        fileSystem.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Delete_ThrowsWhenDirectory() {
        // Arrange
        var fileSystem = new MemoryFs {
            @"c:\path"
        };

        // Act
        var act = () => fileSystem.File.Delete(@"C:\path");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(@"Entry at c:\path is directory.");
    }

    [Fact]
    public void Delete_ThrowsWhenLocked() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"c:\path", "Content" }
        };
        fileSystem.LockFile(@"c:\path");

        // Act
        var act = () => fileSystem.File.Delete(@"C:\path");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(@"File 'c:\path' is locked.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Move_WhenSourceNotFoundOrDirectory(bool isDirectory) {
        // Arrange
        var fileSystem = new MemoryFs();
        if (isDirectory) {
            fileSystem.Add(@"c:\path");
        }

        // Act
        var act = () => fileSystem.File.Move(@"C:\path", @"C:\target");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"Source file not found: 'C:\path'.");
    }

    [Fact]
    public void Move_WhenDestinationExists() {
        // Arrange
        var fileSystem = new MemoryFs();
        fileSystem.Add(@"c:\path", "Source");
        fileSystem.Add(@"c:\target", "target");

        // Act
        var act = () => fileSystem.File.Move(@"C:\path", @"C:\target");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(@"Destination path already exists: 'C:\target'.");
    }

    [Fact]
    public void Move_WhenSourceLocked() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\path", "Source" }
        };
        fileSystem.LockFile(@"C:\path");

        // Act
        var act = () => fileSystem.File.Move(@"C:\path", @"C:\target");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(@"File 'C:\path' is locked.");
    }

    [Fact]
    public void Move_WhenValid() {
        // Arrange
        var fileSystem = new MemoryFs {
            new MemoryEntry(@"c:\path", [1, 2, 3])
        };

        // Act
        fileSystem.File.Move(@"C:\path", @"C:\target");

        // Assert
        fileSystem.Items.Should().ContainKey(@"C:\target").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"C:\target", [1, 2, 3]));
    }

    [Fact]
    public void Create_WhenAddThrows() {
        // Arrange
        var fileSystem = new MemoryFs {
            new MemoryEntry(@"c:\path", [1, 2, 3])
        };

        // Act
        var act = () => fileSystem.File.Create(@"C:\path");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_WhenSucceed() {
        // Arrange
        var fileSystem = new MemoryFs();

        // Act
        var actual = fileSystem.File.Create(@"C:\path");

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
