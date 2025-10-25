using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using NSubstitute.FileSystem;
using Xunit;

namespace Railroader_ModInterfaces.Tests;

public sealed class MemoryFileSystemTests
{
    [Fact]
    public void Constructor_SetsDefaultCurrentDirectory() {
        // Arrange & Act
        var sut = new MemoryFileSystem();

        // Assert
        sut.CurrentDirectory.Should().Be(@"C:\");
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void CurrentDirectory_SetNormalizesPath() {
        // Arrange
        var sut = new MemoryFileSystem();

        // Act
        sut.CurrentDirectory = @"C:\Test\Path\";

        // Assert
        sut.CurrentDirectory.Should().Be(@"C:\Test\Path");
    }

    [Fact]
    public void CurrentDirectory_SetEmptyPath_ThrowsArgumentException() {
        // Arrange
        var sut = new MemoryFileSystem();

        // Act
        Action act = () => sut.CurrentDirectory = "";

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Path cannot be null or empty.*");
    }

    [Fact]
    public void CurrentDirectory_SetInvalidPathChars_ThrowsArgumentException() {
        // Arrange
        var sut = new MemoryFileSystem();

        // Act
        Action act = () => sut.CurrentDirectory = @"C:\Test\*Path";

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Illegal characters in path.");
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void Add_Directory_CreatesDirectoryEntry() {
        // Arrange
        var sut = new MemoryFileSystem();

        // Act
        sut.Add(@"C:\Test\Dir");

        // Assert
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test\Dir", true, MemoryFileSystem.First, null, null)
        ]);
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void Add_Directory_ThrowsArgumentException() {
        // Arrange
        var sut = new MemoryFileSystem {
            (@"C:\Test\Dir", "File")
        };

        // Act
        var act = () => sut.Add(@"C:\Test\Dir\Nested");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(@"Path 'C:\Test\Dir' is a file, not a directory.");
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void Add_DirectoryWithLastWriteTime_CreatesDirectoryEntry() {
        // Arrange
        var sut = new MemoryFileSystem();

        // Act
        sut.Add((Path: @"C:\Test\Dir", LastWriteTime: MemoryFileSystem.Second));

        // Assert
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.Second, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.Second, null, null),
            new MemoryEntry(@"C:\Test\Dir", true, MemoryFileSystem.Second, null, null)
        ]);
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void Add_File_CreatesFileEntry() {
        // Arrange
        var sut = new MemoryFileSystem();

        // Act
        sut.Add((Path: @"C:\Test\File.txt", Content: "Hello, World!"));

        // Assert
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test\File.txt", false, MemoryFileSystem.First, "Hello, World!", null)
        ]);
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void Add_FileWithLastWriteTime_CreatesFileEntry() {
        // Arrange
        var sut = new MemoryFileSystem();

        // Act
        sut.Add((Path: @"C:\Test\File.txt", LastWriteTime: MemoryFileSystem.Second, Content: "Hello"));

        // Assert
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.Second, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.Second, null, null),
            new MemoryEntry(@"C:\Test\File.txt", false, MemoryFileSystem.Second, "Hello", null)
        ]);
    }

    [Fact]
    public void Add_FileWithException_CreatesFileEntryWithException() {
        // Arrange
        var sut       = new MemoryFileSystem();
        var exception = new IOException("Read error");

        // Act
        sut.Add((Path: @"C:\Test\File.txt", LoadException: exception));

        // Assert
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test\File.txt", false, MemoryFileSystem.First, null, exception)
        ]);
    }

    [Fact]
    public void Add_FileWithLastWriteTimeAndException_CreatesFileEntryWithException() {
        // Arrange
        var sut       = new MemoryFileSystem();
        var exception = new IOException("Read error");

        // Act
        sut.Add((Path: @"C:\Test\File.txt", MemoryFileSystem.Second, exception));

        // Assert
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.Second, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.Second, null, null),
            new MemoryEntry(@"C:\Test\File.txt", false, MemoryFileSystem.Second, null, exception)
        ]);
    }

    [Fact]
    public void Add_DuplicatePath_ThrowsInvalidOperationException() {
        // Arrange
        var sut = new MemoryFileSystem { @"C:\Test\Dir" };

        // Act
        var act = () => sut.Add(@"C:\Test\Dir");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(@"Path 'C:\Test\Dir' already exists.*");
    }

    [Fact]
    public void File_Exists_ReturnsTrueForExistingFile() {
        // Arrange
        var sut = new MemoryFileSystem { (Path: @"C:\Test\File.txt", Content: "Test") };

        // Act
        var result = sut.FileSystem.File.Exists(@"C:\Test\File.txt");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void File_Exists_ReturnsFalseForNonExistingFile() {
        // Arrange
        var sut = new MemoryFileSystem();

        // Act
        var result = sut.FileSystem.File.Exists(@"C:\Test\NonExisting.txt");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void File_Exists_ReturnsFalseForDirectory() {
        // Arrange
        var sut = new MemoryFileSystem {
            @"C:\Test\Dir"
        };

        // Act
        var result = sut.FileSystem.File.Exists(@"C:\Test\Dir");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void File_ReadAllText_ReturnsContentForExistingFile() {
        // Arrange
        var sut = new MemoryFileSystem { (Path: @"C:\Test\File.txt", Content: "Test Content") };

        // Act
        var content = sut.FileSystem.File.ReadAllText(@"C:\Test\File.txt");

        // Assert
        content.Should().Be("Test Content");
    }

    [Fact]
    public void File_ReadAllText_ThrowsFileNotFoundForNonExistingFile() {
        // Arrange
        var sut = new MemoryFileSystem();

        // Act
        Action act = () => sut.FileSystem.File.ReadAllText(@"C:\Test\NonExisting.txt");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"File not found: C:\Test\NonExisting.txt");
    }

    [Fact]
    public void File_ReadAllText_ThrowsFileNotFoundForDirectory() {
        // Arrange
        var sut = new MemoryFileSystem {
            @"C:\Test\Dir"
        };

        // Act
        Action act = () => sut.FileSystem.File.ReadAllText(@"C:\Test\Dir");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"File not found: C:\Test\Dir");
    }

    [Fact]
    public void File_ReadAllText_ThrowsReadExceptionForFileWithException() {
        // Arrange
        var sut       = new MemoryFileSystem();
        var exception = new IOException("Read error");
        sut.Add((Path: @"C:\Test\File.txt", LoadException: exception));

        // Act
        Action act = () => sut.FileSystem.File.ReadAllText(@"C:\Test\File.txt");

        // Assert
        act.Should().Throw<IOException>().WithMessage("Read error");
    }

    [Fact]
    public void File_GetLastWriteTime_ReturnsLastWriteTimeForExistingFile() {
        // Arrange
        var sut = new MemoryFileSystem { (Path: @"C:\Test\File.txt", LastWriteTime: MemoryFileSystem.Second, Content: "Test") };

        // Act
        var lastWriteTime = sut.FileSystem.File.GetLastWriteTime(@"C:\Test\File.txt");

        // Assert
        lastWriteTime.Should().Be(MemoryFileSystem.Second);
    }

    [Fact]
    public void File_GetLastWriteTime_ThrowsFileNotFoundForNonExistingFile() {
        // Arrange
        var sut = new MemoryFileSystem();

        // Act
        Action act = () => sut.FileSystem.File.GetLastWriteTime(@"C:\Test\NonExisting.txt");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"File not found: C:\Test\NonExisting.txt");
    }

    [Fact]
    public void File_GetLastWriteTime_ThrowsFileNotFoundForDirectory() {
        // Arrange
        var sut = new MemoryFileSystem { 
            @"C:\Test\Dir"
        };

        // Act
        Action act = () => sut.FileSystem.File.GetLastWriteTime(@"C:\Test\Dir");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"File not found: C:\Test\Dir");
    }

    [Fact]
    public void FileInfo_GetLastWriteTime_ReturnsLastWriteTimeForExistingFile() {
        // Arrange
        var sut = new MemoryFileSystem { (Path: @"C:\Test\File.txt", LastWriteTime: MemoryFileSystem.Second, Content: "Test") };

        // Act
        var fileInfo = sut.FileSystem.DirectoryInfo(@"C:\Test").EnumerateFiles("*.*").First();

        // Assert
        fileInfo.FullName.Should().Be(@"C:\Test\File.txt");
        fileInfo.LastWriteTime.Should().Be(MemoryFileSystem.Second);
    }

    [Fact]
    public void File_Delete_RemovesExistingFile() {
        // Arrange
        var sut = new MemoryFileSystem { (Path: @"C:\Test\File.txt", Content: "Test") };

        // Act
        sut.FileSystem.File.Delete(@"C:\Test\File.txt");

        // Assert
        sut.FileSystem.File.Exists(@"C:\Test\File.txt").Should().BeFalse();
    }

    [Fact]
    public void File_Delete_ThrowsWhenFileLocked() {
        // Arrange
        var sut = new MemoryFileSystem { (Path: @"C:\Test\File.txt", Content: "Test") };
        sut.LockFile(@"C:\Test\File.txt");

        // Act
        var act = () => sut.FileSystem.File.Delete(@"C:\Test\File.txt");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(@"File C:\Test\File.txt is locked");
    }

    [Fact]
    public void File_Delete_RemovesExistingFileWhenUnlocked() {
        // Arrange
        var sut = new MemoryFileSystem { (Path: @"C:\Test\File.txt", Content: "Test") };
        sut.LockFile(@"C:\Test\File.txt");

        // Act
        var act = () => sut.FileSystem.File.Delete(@"C:\Test\File.txt");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(@"File C:\Test\File.txt is locked");

        // Act
        sut.UnlockFile(@"C:\Test\File.txt");
        sut.FileSystem.File.Delete(@"C:\Test\File.txt");

        // Assert
        sut.FileSystem.File.Exists(@"C:\Test\File.txt").Should().BeFalse();
    }

    [Fact]
    public void File_Move_MovesFileToNewPath() {
        // Arrange
        var sut = new MemoryFileSystem { (Path: @"C:\Test\File.txt", Content: "Test") };

        // Act
        sut.FileSystem.File.Move(@"C:\Test\File.txt", @"C:\Test\NewFile.txt");

        // Assert
        sut.FileSystem.File.Exists(@"C:\Test\File.txt").Should().BeFalse();
        sut.FileSystem.File.Exists(@"C:\Test\NewFile.txt").Should().BeTrue();
        sut.FileSystem.File.ReadAllText(@"C:\Test\NewFile.txt").Should().Be("Test");
    }

    [Fact]
    public void File_Move_SourceNotExists_ThrowsInvalidOperationException() {
        // Arrange
        var sut = new MemoryFileSystem();

        // Act
        var act = () => sut.FileSystem.File.Move(@"C:\Test\File1.txt", @"C:\Test\File2.txt");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"Source file not found: C:\Test\File1.txt");
    }

    [Fact]
    public void File_Move_SourceIsDirectory_ThrowsInvalidOperationException() {
        // Arrange
        var sut = new MemoryFileSystem {
            @"C:\Test\Dir"
        };

        // Act
        var act = () => sut.FileSystem.File.Move(@"C:\Test\Dir", @"C:\Test\File2.txt");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"Source file not found: C:\Test\Dir");
    }

    [Fact]
    public void File_Move_DestinationExists_ThrowsInvalidOperationException() {
        // Arrange
        var sut = new MemoryFileSystem {
            (Path: @"C:\Test\File1.txt", Content: "Test1"),
            (Path: @"C:\Test\File2.txt", Content: "Test2")
        };

        // Act
        var act = () => sut.FileSystem.File.Move(@"C:\Test\File1.txt", @"C:\Test\File2.txt");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(@"Destination path already exists: C:\Test\File2.txt");
    }

    [Fact]
    public void File_Move_ThrowsWhenSourceFileLocked() {
        // Arrange
        var sut = new MemoryFileSystem { (Path: @"C:\Test\File.txt", Content: "Test") };
        sut.LockFile(@"C:\Test\File.txt");
        
        // Act
        var act = () => sut.FileSystem.File.Move(@"C:\Test\File.txt", @"C:\Test\File2.txt");
        
        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage(@"File C:\Test\File.txt is locked");
    }

    [Fact]
    public void File_Delete_DoNothingWhenPathIsDirectory() {
        // Arrange
        var sut = new MemoryFileSystem { @"C:\Test\Dir" };

        // Act
        sut.FileSystem.File.Delete(@"C:\Test\Dir");

        // Assert
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test\Dir", true, MemoryFileSystem.First, null, null)
        ]);
    }

    [Fact]
    public void Directory_ReadAllText_ThrowsInvalidOperationException() {
        // Arrange
        var sut = new MemoryFileSystem {
            @"C:\Test\Dir"
        };

        // Act
        var act = () => sut.FileSystem.File.ReadAllText(@"C:\Test\Dir");

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage(@"File not found: C:\Test\Dir");
    }

    [Fact]
    public void Directory_EnumerateDirectories_ReturnsDirectoriesInPath() {
        // Arrange
        var sut = new MemoryFileSystem {
            @"C:\Test\Dir1",
            @"C:\Test\Dir2",
            @"C:\Test\Dir3\SubDir"
        };

        // Act
        var directories = sut.FileSystem.Directory.EnumerateDirectories(@"C:\Test").ToList();

        // Assert
        directories.Should().BeEquivalentTo(@"C:\Test\Dir1", @"C:\Test\Dir2", @"C:\Test\Dir3");
    }

    [Fact]
    public void Directory_EnumerateDirectoriesInRoot_ReturnsDirectoriesInPath() {
        // Arrange
        var sut = new MemoryFileSystem {
            @"C:\Dir1",
            @"C:\Dir2",
            @"C:\Dir3\SubDir"
        };

        // Act
        var directories = sut.FileSystem.Directory.EnumerateDirectories(@"C:\").ToList();

        // Assert
        directories.Should().BeEquivalentTo(@"C:\Dir1", @"C:\Dir2", @"C:\Dir3");
    }

    [Fact]
    public void Directory_EnumerateFiles_TopDirectoryOnly_ReturnsFilesInPath() {
        // Arrange
        var sut = new MemoryFileSystem {
            (Path: @"C:\Test\File1.txt", Content: "Test1"),
            (Path: @"C:\Test\File2.txt", Content: "Test2"),
            (Path: @"C:\Test\SubDir\File3.txt", Content: "Test3")
        };

        // Act
        var files = sut.FileSystem.DirectoryInfo(@"C:\Test")
                       .EnumerateFiles("*.*")
                       .Select(f => f.FullName)
                       .ToList();

        // Assert
        files.Should().BeEquivalentTo(@"C:\Test\File1.txt", @"C:\Test\File2.txt");
    }

    [Fact]
    public void Directory_EnumerateFiles_AllDirectories_ReturnsAllFiles() {
        // Arrange
        var sut = new MemoryFileSystem {
            (Path: @"C:\Test\SubDir\File2.txt", Content: "Test2"),
            (Path: @"C:\Test\File1.txt", Content: "Test1"),
        };

        // Act
        var files = sut.FileSystem.DirectoryInfo(@"C:\Test")
                       .EnumerateFiles("*.*", SearchOption.AllDirectories)
                       .Select(f => f.FullName)
                       .ToList();

        // Assert
        files.Should().BeEquivalentTo([@"C:\Test\File1.txt", @"C:\Test\SubDir\File2.txt"], o => o.WithStrictOrdering());
    }

    [Theory]
    [InlineData("Foo.*", new[] { @"C:\Test\Foo.fiz", @"C:\Test\Foo.fizz", @"C:\Test\Foo.doc" })]
    [InlineData("Foo.???", new[] { @"C:\Test\Foo.fiz",  @"C:\Test\Foo.doc" })]
    [InlineData("???.*", new[] { @"C:\Test\Foo.fiz", @"C:\Test\Foo.fizz", @"C:\Test\Foo.doc", @"C:\Test\Buz.txt" })]
    [InlineData("B?z.*", new[] {  @"C:\Test\Buz.txt" })]
    public void Directory_EnumerateFiles_WithPattern_ReturnsMatchingFiles(string searchPattern, string[] expected) {
        // Arrange
        var sut = new MemoryFileSystem {
            (Path: @"C:\Test\Foo.fiz", Content: ""),
            (Path: @"C:\Test\Foo.fizz", Content: ""),
            (Path: @"C:\Test\Foo.doc", Content: ""),
            (Path: @"C:\Test\Buzz.txt", Content: ""),
            (Path: @"C:\Test\Buz.txt", Content: ""),
        };

        // Act
        var files = sut.FileSystem.DirectoryInfo(@"C:\Test")
                       .EnumerateFiles(searchPattern)
                       .Select(f => f.FullName)
                       .ToList();

        // Assert
        files.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Directory_EnumerateFiles_InvalidFileNameCharInPattern_ThrowsArgumentException() {
        // Arrange
        var sut = new MemoryFileSystem();

        // Act
        var act = () => sut.FileSystem.DirectoryInfo(@"C:\").EnumerateFiles("file<.txt").ToList();

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Invalid search pattern.*");
    }

    [Fact]
    public void Directory_GetCurrentDirectory() {
        // Arrange
        var sut = new MemoryFileSystem(@"C:\Test");

        // Act
        var currentDirectory = sut.FileSystem.Directory.GetCurrentDirectory();

        // Assert
        currentDirectory.Should().Be(@"C:\Test");
    }

    [Fact]
    public void EnsureDirectory_CreatesParentDirectories() {
        // Arrange
        var sut  = new MemoryFileSystem();
        var file = (Path: @"C:\Test\SubDir\File.txt", Content: "Test");

        // Act
        sut.Add(file);

        // Assert
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test\SubDir", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test\SubDir\File.txt", false, MemoryFileSystem.First, "Test", null)
        ]);
    }

    [Fact]
    public void Enumerate_ReturnsAllEntries() {
        // Arrange
        var sut = new MemoryFileSystem {
            @"C:\Test\Dir",
            (Path: @"C:\Test\Foo.txt", Content: "Foo"),
            (Path: @"C:\Test\Bar.txt", Content: "Bar"),
            (Path: @"C:\Test\Baz.txt", Content: "Baz")
        };

        var expected = new MemoryEntry[] {
            new(@"C:\", true, MemoryFileSystem.First, null, null),
            new(@"C:\Test", true, MemoryFileSystem.First, null, null),
            new(@"C:\Test\Bar.txt", false, MemoryFileSystem.First, "Bar", null),
            new(@"C:\Test\Baz.txt", false, MemoryFileSystem.First, "Baz", null),
            new(@"C:\Test\Dir", true, MemoryFileSystem.First, null, null),
            new(@"C:\Test\Foo.txt", false, MemoryFileSystem.First, "Foo", null)
        };

        // Act
        var entries = sut.ToList();

        // Assert
        var enumerator = entries.GetEnumerator();
        var i          = 0;
        while (enumerator.MoveNext()) {
            enumerator.Current.Should().BeEquivalentTo(expected[i]);
            ++i;
        }
    }

    [Fact]
    public void File_Create_NewFile_WritesContent() {
        // Arrange
        var sut = new MemoryFileSystem();

        // Act
        using (var stream = sut.FileSystem.File.Create(@"C:\Test\File.txt")) {
            using var writer = new StreamWriter(stream, Encoding.ASCII);
            writer.Write("Hello, World!");
        }

        // Assert
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test\File.txt", false, MemoryFileSystem.First, "Hello, World!", null)
        ]);
    }

    [Fact]
    public void File_Create_NewFile_EmptyContent() {
        // Arrange
        var sut  = new MemoryFileSystem();
        var path = @"C:\Test\File.txt";

        // Act
        using (sut.FileSystem.File.Create(path)) {
            sut.Should().ContainEquivalentOf(new MemoryEntry(@"C:\Test\File.txt", false, MemoryFileSystem.First, "", null));
            // No writing to the stream
        }

        // Assert
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(path, false, MemoryFileSystem.First, "", null)
        ]);
    }

    [Fact]
    public void File_Create_ExistingFile_ThrowsInvalidOperationException() {
        // Arrange
        var sut = new MemoryFileSystem {
            (Path: @"C:\Test\File.txt", Content: "Existing")
        };
        var path = @"C:\Test\File.txt";

        // Act
        var act = () => sut.FileSystem.File.Create(path);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage($"Path '{path}' already exists.*");
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(path, false, MemoryFileSystem.First, "Existing", null)
        ]);
    }

    [Fact]
    public void File_Create_NestedPath_CreatesParentDirectories() {
        // Arrange
        var sut     = new MemoryFileSystem();
        var path    = @"C:\Test\SubDir\File.txt";
        var content = "Nested content";

        // Act
        using (var stream = sut.FileSystem.File.Create(path)) {
            using var writer = new StreamWriter(stream, Encoding.ASCII);
            writer.Write(content);
            writer.Flush();
        }

        // Assert
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test\SubDir", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(path, false, MemoryFileSystem.First, content, null)
        ]);
    }

    [Fact]
    public void File_Create_MultipleWrites_UpdatesContentCorrectly() {
        // Arrange
        var sut      = new MemoryFileSystem();
        var path     = @"C:\Test\File.txt";
        var content1 = "First write.";
        var content2 = "Second write.";

        // Act
        using (var stream = sut.FileSystem.File.Create(path)) {
            using var writer = new StreamWriter(stream, Encoding.ASCII);
            writer.Write(content1);
            writer.Write(content2);
        }

        // Assert
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(path, false, MemoryFileSystem.First, content1 + content2, null)
        ]);
    }

    [Fact]
    public void File_Create_NullPath_ThrowsArgumentException() {
        // Arrange
        var sut = new MemoryFileSystem();

        // Act
        var act = () => sut.FileSystem.File.Create(null!);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Path cannot be null or empty.*");
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.First, null, null)
        ]);
    }

    [Fact]
    public void File_Create_InvalidPathChars_ThrowsArgumentException() {
        // Arrange
        var sut  = new MemoryFileSystem();
        var path = @"C:\Test\File*.txt";

        // Act
        var act = () => sut.FileSystem.File.Create(path);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Illegal characters in path.");
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.First, null, null)
        ]);
    }

    [Fact]
    public void File_Create_MultipleDispose_DoesNotThrow() {
        // Arrange
        var sut     = new MemoryFileSystem();
        var path    = @"C:\Test\File.txt";
        var content = "Test content";

        // Act
        var stream = sut.FileSystem.File.Create(path);
        using (var writer = new StreamWriter(stream, Encoding.ASCII)) {
            writer.Write(content);
        }

        stream.Dispose();                 // First dispose
        var act = () => stream.Dispose(); // Second dispose

        // Assert
        act.Should().NotThrow();
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(path, false, MemoryFileSystem.First, content, null)
        ]);
    }

    [Fact]
    public void File_Create_LargeContent_WritesCorrectly() {
        // Arrange
        var sut          = new MemoryFileSystem();
        var path         = @"C:\Test\File.txt";
        var largeContent = new string('A', 10000); // 10KB content

        // Act
        using (var stream = sut.FileSystem.File.Create(path)) {
            using var writer = new StreamWriter(stream, Encoding.ASCII);
            writer.Write(largeContent);
        }

        // Assert
        sut.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(@"C:\Test", true, MemoryFileSystem.First, null, null),
            new MemoryEntry(path, false, MemoryFileSystem.First, largeContent, null)
        ]);
    }
}
