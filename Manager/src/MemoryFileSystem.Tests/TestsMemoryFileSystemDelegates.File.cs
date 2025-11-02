using System;
using System.IO;
using MemoryFileSystem.Tests.TestExtensions;
using MemoryFileSystem.Types;
using Shouldly;
using Xunit;

namespace MemoryFileSystem.Tests;

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
        actual.ShouldBe(type == "File");
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
        actual.ShouldBe("Content");
    }

    [Fact]
    public void ReadAllText_ThrowsWhenException() {
        // Arrange
        var exception = new Exception();
        var fileSystem = new MemoryFs {
            { @"C:\Path", exception }
        };

        // Act & Assert
        Should.Throw<Exception>(() => fileSystem.File.ReadAllText(@"C:\path")).ShouldBe(exception);
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

        // Act & Assert
        Should.Throw<FileNotFoundException>(() => fileSystem.File.ReadAllText(@"c:\path"))
              .Message.ShouldBe(@"File not found: c:\path");
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
        actual.ShouldBe(MemoryEntry.DefaultLastWriteTime);
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

        // Act & Assert
        Should.Throw<FileNotFoundException>(() => fileSystem.File.GetLastWriteTime(@"c:\path"))
              .Message.ShouldBe(@"File not found: c:\path");
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
        fileSystem.Items.Count.ShouldBe(1);
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
        fileSystem.Items.Count.ShouldBe(2);
    }

    [Fact]
    public void Delete_ThrowsWhenDirectory() {
        // Arrange
        var fileSystem = new MemoryFs {
            @"c:\path"
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => fileSystem.File.Delete(@"c:\path"))
              .Message.ShouldBe(@"Entry at c:\path is directory.");
    }

    [Fact]
    public void Delete_ThrowsWhenLocked() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"c:\path", "Content" }
        };
        fileSystem.LockFile(@"c:\path");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => fileSystem.File.Delete(@"c:\path"))
              .Message.ShouldBe(@"File 'c:\path' is locked.");
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

        // Act & Assert
        Should.Throw<FileNotFoundException>(() => fileSystem.File.Move(@"C:\path", @"C:\target"))
              .Message.ShouldBe(@"Source file not found: 'C:\path'.");
    }

    [Fact]
    public void Move_WhenDestinationExists() {
        // Arrange
        var fileSystem = new MemoryFs();
        fileSystem.Add(@"c:\path", "Source");
        fileSystem.Add(@"c:\target", "target");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => fileSystem.File.Move(@"C:\path", @"C:\target"))
              .Message.ShouldBe(@"Destination path already exists: 'C:\target'.");
    }

    [Fact]
    public void Move_WhenSourceLocked() {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\path", "Source" }
        };
        fileSystem.LockFile(@"C:\path");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => fileSystem.File.Move(@"C:\path", @"C:\target"))
              .Message.ShouldBe(@"File 'C:\path' is locked.");
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
        fileSystem.Items.ShouldContainKeyWhereValue(@"C:\target", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"C:\target", [1, 2, 3])));
    }

    [Fact]
    public void Create_WhenAddThrows() {
        // Arrange
        var fileSystem = new MemoryFs {
            new MemoryEntry(@"c:\path", [1, 2, 3])
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => fileSystem.File.Create(@"C:\path"));
    }

    [Fact]
    public void Create_WhenSucceed() {
        // Arrange
        var fileSystem = new MemoryFs();

        // Act
        var actual = fileSystem.File.Create(@"C:\path");

        // Assert
        actual.ShouldBeOfType<MemoryFileStream>();


        fileSystem.Items.Count.ShouldBe(2);
        fileSystem.Items.ShouldContainKeyWhereValue(@"C:\", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"C:\")));
        fileSystem.Items.ShouldContainKeyWhereValue(@"C:\path", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"C:\path", [])));

        actual.Write([1, 2, 3], 0, 3);
        actual.Dispose();
        fileSystem.Items.ShouldContainKeyWhereValue(@"C:\path", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"C:\path", [1, 2, 3])));
    }
}
