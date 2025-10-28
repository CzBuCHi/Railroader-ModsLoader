using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using FluentAssertions;
using MemoryFileSystem.Internal;
using Xunit;

namespace MemoryFileSystem.Tests;

[SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
public sealed class TestsMemoryFileSystemBaseAdd
{
    private static readonly DateTime _Date = new(2000, 1, 2);

    [Fact]
    public void Add_Directory() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();

        // Act
        sut.Add("Folder");

        // Assert
        sut.Items.Should().HaveCount(1);
        sut.Items.Should().ContainKey("folder").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("folder"));
    }

    [Fact]
    public void Add_Directory_WithLastWriteTime() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();

        // Act
        sut.Add("Folder", _Date);

        // Assert
        sut.Items.Should().HaveCount(1);
        sut.Items.Should().ContainKey("folder").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("folder", _Date));
    }

    [Fact]
    public void Add_File_Binary() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();

        // Act
        sut.Add("File", [1, 2, 3]);

        // Assert
        sut.Items.Should().HaveCount(1);
        sut.Items.Should().ContainKey("file").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("file", [1, 2, 3]));
    }

    [Fact]
    public void Add_File_Binary_WithLastWriteTime() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();

        // Act
        sut.Add("File", [1, 2, 3], _Date);

        // Assert
        sut.Items.Should().HaveCount(1);
        sut.Items.Should().ContainKey("file").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("file", [1, 2, 3], _Date));
    }

    [Fact]
    public void Add_File_Text() {
        // Arrange
        var sut          = new MemoryFileSystemBaseImpl();
        var contentBytes = Encoding.UTF8.GetBytes("Content");

        // Act
        sut.Add("File", "Content");

        // Assert
        sut.Items.Should().HaveCount(1);
        sut.Items.Should().ContainKey("file").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("file", contentBytes));
    }

    [Fact]
    public void Add_File_Text_WithLastWriteTime() {
        // Arrange
        var sut          = new MemoryFileSystemBaseImpl();
        var contentBytes = Encoding.UTF8.GetBytes("Content");

        // Act
        sut.Add("File", "Content", _Date);

        // Assert
        sut.Items.Should().HaveCount(1);
        sut.Items.Should().ContainKey("file").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("file", contentBytes, _Date));
    }

    [Fact]
    public void Add_File_Zip() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();
        var zip = new MemoryZip();

        // Act
        sut.Add("File", zip);

        // Assert
        sut.Items.Should().HaveCount(1);
        sut.Items.Should().ContainKey("file").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("file", zip.GetBytes()));
    }

    [Fact]
    public void Add_File_Zip_WithLastWriteTime() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();
        var zip = new MemoryZip();

        // Act
        sut.Add("File", zip, _Date);

        // Assert
        sut.Items.Should().HaveCount(1);
        sut.Items.Should().ContainKey("file").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("file", zip.GetBytes(), _Date));
    }

    [Fact]
    public void Add_File_Exception() {
        // Arrange
        var sut       = new MemoryFileSystemBaseImpl();
        var exception = new Exception();

        // Act
        sut.Add("File", exception);

        // Assert
        sut.Items.Should().HaveCount(1);
        sut.Items.Should().ContainKey("file").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("file", exception));
    }

    [Fact]
    public void Add_File_Exception_WithLastWriteTime() {
        // Arrange
        var sut       = new MemoryFileSystemBaseImpl();
        var exception = new Exception();

        // Act
        sut.Add("File", exception, _Date);

        // Assert
        sut.Items.Should().HaveCount(1);
        sut.Items.Should().ContainKey("file").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("file", exception, _Date));
    }

    [Fact]
    public void Add_Duplicate_Throws() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl {
            "Folder"
        };

        // Act
        var act = () => sut.Add("Folder");

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Path 'folder' already exists.");

        sut.Items.Should().HaveCount(1);
        sut.Items.Should().ContainKey("folder").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("folder"));
    }

    [Fact]
    public void Add_AddParents() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();

        // Act
        sut.Add(@"Folder\Nested\Deep");

        // Assert
        sut.Items.Should().HaveCount(3);
        sut.Items.Should().ContainKey("folder").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("folder"));
        sut.Items.Should().ContainKey(@"folder\nested").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"folder\nested"));
        sut.Items.Should().ContainKey(@"folder\nested\deep").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"folder\nested\deep"));
    }

    [Fact]
    public void Add_UpdateParentsLastWriteTime() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl {
            "Folder"
        };

        // Act
        sut.Add(@"Folder\Nested\Deep", _Date);

        // Assert
        sut.Items.Should().HaveCount(3);
        sut.Items.Should().ContainKey("folder").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("folder", _Date));
        sut.Items.Should().ContainKey(@"folder\nested").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"folder\nested", _Date));
        sut.Items.Should().ContainKey(@"folder\nested\deep").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"folder\nested\deep", _Date));
    }

    [Fact]
    public void Add_Throws_WhenCannotAddParents() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl {
            { "File", [1, 2, 3] }
        };

        // Act
        var act = () => sut.Add("File\\Nested");

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Path 'file' is a file, not a directory.");

        sut.Items.Should().HaveCount(1);
        sut.Items.Should().ContainKey("file").WhoseValue.Should().BeEquivalentTo(new MemoryEntry("file", [1, 2, 3]));
    }

    [Fact]
    public void AddRange_AddsINCorrectOrder() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();

        // Act
        sut.AddRange([
            new MemoryEntry(@"C:\Path"),
            new MemoryEntry(@"C:\Path\To\File.txt", [1, 2, 3]),
            new MemoryEntry(@"C:\Path\To"),
        ]);

        // Assert
        sut.Items.Should().ContainKey(@"c:\").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"c:\"));
        sut.Items.Should().ContainKey(@"c:\path").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"c:\path"));
        sut.Items.Should().ContainKey(@"c:\path\to").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"c:\path\to"));
        sut.Items.Should().ContainKey(@"c:\path\to\file.txt").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"c:\path\to\file.txt", [1, 2, 3]));
    }

    private sealed class MemoryFileSystemBaseImpl : MemoryFileSystemBase
    {
        public override string NormalizePath(string path) => path.ToLower();

        protected override string? GetParentPath(string path) => Path.GetDirectoryName(path);
    }
}
