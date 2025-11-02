using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using MemoryFileSystem.Tests.TestExtensions;
using MemoryFileSystem.Types;
using Shouldly;
using Xunit;

namespace MemoryFileSystem.Tests;

[SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
public sealed class TestsMemoryFileSystemAdd
{
    private static readonly DateTime _Date = new(2000, 1, 2);

    [Fact]
    public void Add_Directory() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();

        // Act
        sut.Add("folder");

        // Assert
        sut.Items.Count.ShouldBe(1);
        sut.Items.ShouldContainKeyWhereValue("folder", o => o.ShouldBeEquivalentTo(new MemoryEntry("folder")));
    }

    [Fact]
    public void Add_Directory_WithLastWriteTime() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();

        // Act
        sut.Add("folder", _Date);

        // Assert
        sut.Items.Count.ShouldBe(1);
        sut.Items.ShouldContainKeyWhereValue("folder", o => o.ShouldBeEquivalentTo(new MemoryEntry("folder", _Date)));
    }

    [Fact]
    public void Add_File_Binary() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();

        // Act
        sut.Add("file", [1, 2, 3]);

        // Assert
        sut.Items.Count.ShouldBe(1);
        sut.Items.ShouldContainKeyWhereValue("file", o => o.ShouldBeEquivalentTo(new MemoryEntry("file", [1, 2, 3])));
    }

    [Fact]
    public void Add_File_Binary_WithLastWriteTime() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();

        // Act
        sut.Add("file", [1, 2, 3], _Date);

        // Assert
        sut.Items.Count.ShouldBe(1);
        sut.Items.ShouldContainKeyWhereValue("file", o => o.ShouldBeEquivalentTo(new MemoryEntry("file", [1, 2, 3], _Date)));
    }

    [Fact]
    public void Add_File_Text() {
        // Arrange
        var sut          = new MemoryFileSystemBaseImpl();
        var contentBytes = Encoding.UTF8.GetBytes("Content");

        // Act
        sut.Add("file", "Content");

        // Assert
        sut.Items.Count.ShouldBe(1);
        sut.Items.ShouldContainKeyWhereValue("file", o => o.ShouldBeEquivalentTo(new MemoryEntry("file", contentBytes)));
    }

    [Fact]
    public void Add_File_Text_WithLastWriteTime() {
        // Arrange
        var sut          = new MemoryFileSystemBaseImpl();
        var contentBytes = Encoding.UTF8.GetBytes("Content");

        // Act
        sut.Add("file", "Content", _Date);

        // Assert
        sut.Items.Count.ShouldBe(1);
        sut.Items.ShouldContainKeyWhereValue("file", o => o.ShouldBeEquivalentTo(new MemoryEntry("file", contentBytes, _Date)));
    }

    [Fact]
    public void Add_File_Zip() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();
        var zip = new MemoryZip();

        // Act
        sut.Add("file", zip);

        // Assert
        sut.Items.Count.ShouldBe(1);
        sut.Items.ShouldContainKeyWhereValue("file", o => o.ShouldBeEquivalentTo(new MemoryEntry("file", zip.GetBytes())));
    }

    [Fact]
    public void Add_File_Zip_WithLastWriteTime() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();
        var zip = new MemoryZip();

        // Act
        sut.Add("file", zip, _Date);

        // Assert
        sut.Items.Count.ShouldBe(1);
        sut.Items.ShouldContainKeyWhereValue("file", o => o.ShouldBeEquivalentTo(new MemoryEntry("file", zip.GetBytes(), _Date)));
    }

    [Fact]
    public void Add_File_Exception() {
        // Arrange
        var sut       = new MemoryFileSystemBaseImpl();
        var exception = new Exception();

        // Act
        sut.Add("file", exception);

        // Assert
        sut.Items.Count.ShouldBe(1);
        sut.Items.ShouldContainKeyWhereValue("file", o => o.ShouldBeEquivalentTo(new MemoryEntry("file", exception)));
    }

    [Fact]
    public void Add_File_Exception_WithLastWriteTime() {
        // Arrange
        var sut       = new MemoryFileSystemBaseImpl();
        var exception = new Exception();

        // Act
        sut.Add("file", exception, _Date);

        // Assert
        sut.Items.Count.ShouldBe(1);
        sut.Items.ShouldContainKeyWhereValue("file", o => o.ShouldBeEquivalentTo(new MemoryEntry("file", exception, _Date)));
    }

    [Fact]
    public void Add_Duplicate_Throws() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl {
            "folder"
        };

        // Act
        Should.Throw<InvalidOperationException>(() => sut.Add("folder"))
              .Message.ShouldBe("Path 'folder' already exists.");

        sut.Items.Count.ShouldBe(1);
        sut.Items.ShouldContainKeyWhereValue("folder", o => o.ShouldBeEquivalentTo(new MemoryEntry("folder")));
    }

    [Fact]
    public void Add_AddParents() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();

        // Act
        sut.Add(@"folder\nested\deep");

        // Assert
        sut.Items.Count.ShouldBe(3);
        sut.Items.ShouldContainKeyWhereValue("folder", o => o.ShouldBeEquivalentTo(new MemoryEntry("folder")));
        sut.Items.ShouldContainKeyWhereValue("folder\\nested", o => o.ShouldBeEquivalentTo(new MemoryEntry("folder\\nested")));
        sut.Items.ShouldContainKeyWhereValue(@"folder\nested\deep", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"folder\nested\deep")));
    }

    [Fact]
    public void Add_UpdateParentsLastWriteTime() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl {
            "folder"
        };

        // Act
        sut.Add(@"folder\nested\deep", _Date);

        // Assert
        sut.Items.Count.ShouldBe(3);
        sut.Items.ShouldContainKeyWhereValue("folder", o => o.ShouldBeEquivalentTo(new MemoryEntry("folder", _Date)));
        sut.Items.ShouldContainKeyWhereValue("folder\\nested", o => o.ShouldBeEquivalentTo(new MemoryEntry("folder\\nested", _Date)));
        sut.Items.ShouldContainKeyWhereValue(@"folder\nested\deep", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"folder\nested\deep", _Date)));
    }

    [Fact]
    public void Add_Throws_WhenCannotAddParents() {
        // Arrange
        byte[] content = [1, 2, 3];
        var sut = new MemoryFileSystemBaseImpl {
            { "file", content }
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => sut.Add("file\\nested"))
              .Message.ShouldBe("Path 'file' is a file, not a directory.");

        sut.Items.Count.ShouldBe(1);
        sut.Items.ShouldContainKeyWhereValue("file", o => o.ShouldBeEquivalentTo(new MemoryEntry("file", content)));
    }

    [Fact]
    public void AddRange_AddsINCorrectOrder() {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();

        // Act
        sut.AddRange([
            new MemoryEntry(@"c:\path"),
            new MemoryEntry(@"c:\path\to\file.txt", [1, 2, 3]),
            new MemoryEntry(@"c:\path\to")
        ]);

        // Assert
        sut.Items.Count.ShouldBe(4);
        sut.Items.ShouldContainKeyWhereValue(@"c:\", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"c:\")));
        sut.Items.ShouldContainKeyWhereValue(@"c:\path", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"c:\path")));
        sut.Items.ShouldContainKeyWhereValue(@"c:\path\to", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"c:\path\to")));
        sut.Items.ShouldContainKeyWhereValue(@"c:\path\to\file.txt", o => o.ShouldBeEquivalentTo(new MemoryEntry(@"c:\path\to\file.txt", [1, 2, 3])));
    }

    private sealed class MemoryFileSystemBaseImpl : MemoryFileSystem
    {
        public override string NormalizePath(string path) => path.ToLower();

        protected override string? GetParentPath(string path) => Path.GetDirectoryName(path);
    }
}
