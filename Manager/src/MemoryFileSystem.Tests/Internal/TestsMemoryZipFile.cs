//using System.IO;
//using FluentAssertions;
//using MemoryFileSystem.Internal;
//using MemoryFileSystem.Types;
//using NSubstitute;
//using Xunit;

//namespace MemoryFileSystem.Tests.Internal;

//public sealed class TestsMemoryZipFile
//{
//    private IMemoryFileSystem CreateMemoryFileSystem(EntryDictionary entries) {
//        var fileSystem = Substitute.For<IMemoryFileSystem>();
//        fileSystem.Items.Returns(entries);
//        fileSystem.NormalizePath(Arg.Any<string>()).Returns(o => o.Arg<string>().ToLower());
//        return fileSystem;
//    }

//    [Theory]
//    [InlineData(true)]
//    [InlineData(false)]
//    public void ExtractToDirectory_ThrowWhenSourceNotFoundOrDirectory(bool isDirectory) {
//        // Arrange
//        var entries   = new EntryDictionary();
//        if (isDirectory) {
//            entries.TryAdd(@"C:\Source.zip", new MemoryEntry(@"C:\Source.zip"));
//        }

//        var fileSystem = CreateMemoryFileSystem(entries);
//        var sut        = new MemoryZipFile(fileSystem);

//        // Act
//        var act = () => sut.ExtractToDirectory(@"C:\Source.zip", @"C:\target");
        
//        // Assert
//        act.Should().Throw<FileNotFoundException>().WithMessage(@"Zip file 'C:\Source.zip' not found.");
//    }

//    [Fact]
//    public void ExtractToDirectory_ThrowWhenSourceNotZip() {
//        // Arrange
//        var entries   = new EntryDictionary();
//        entries.TryAdd(@"C:\Source.zip", new MemoryEntry(@"C:\Source.zip", [1, 2, 3]));

//        var fileSystem = CreateMemoryFileSystem(entries);
//        var sut        = new MemoryZipFile(fileSystem);

//        // Act
//        var act = () => sut.ExtractToDirectory(@"C:\Source.zip", @"C:\target");
        
//        // Assert
//        act.Should().Throw<InvalidDataException>().WithMessage(@"Failed to deserialize zip contents for 'c:\source.zip'.");
//    }

//    [Fact]
//    public void ExtractNestedZipFile_CreatesCorrectEntries() {
//        // Arrange
//        var zipFile = new MemoryZip();
//        zipFile.Add(@"Path\In\Zip\File.txt", [1, 2, 3]);

//        var sut = new MemoryFs();
//        sut.Add(@"C:\Real\Path\File.zip", zipFile);

//        // Act
//        sut.FileSystem.ZipFile.ExtractToDirectory(@"C:\Real\Path\File.zip", @"C:\Real\Path\Dest");

//        // Assert
//        sut.Should().BeEquivalentTo([
//            new MemoryEntry(@"C:\"),
//            new MemoryEntry(@"C:\Real"),
//            new MemoryEntry(@"C:\Real\Path"),
//            new MemoryEntry(@"C:\Real\Path\Dest"),
//            new MemoryEntry(@"C:\Real\Path\Dest\Path"),
//            new MemoryEntry(@"C:\Real\Path\Dest\Path\In"),
//            new MemoryEntry(@"C:\Real\Path\Dest\Path\In\Zip"),
//            new MemoryEntry(@"C:\Real\Path\Dest\Path\In\Zip\File.txt", [1, 2, 3]),
//            new MemoryEntry(@"C:\Real\Path\File.zip", zipFile.GetBytes())
//        ]);
//    }

//    [Theory]
//    [InlineData(true)]
//    [InlineData(false)]
//    public void OpenRead_ThrowWhenSourceNotFoundOrDirectory(bool isDirectory) {
//        // Arrange
//        var entries   = new EntryDictionary();
//        if (isDirectory) {
//            entries.TryAdd(@"C:\Source.zip", new MemoryEntry(@"C:\Source.zip"));
//        }

//        var fileSystem = CreateMemoryFileSystem(entries);
//        var sut        = new MemoryZipFile(fileSystem);

//        // Act
//        var act = () => sut.OpenRead(@"C:\Source.zip");
        
//        // Assert
//        act.Should().Throw<FileNotFoundException>().WithMessage(@"Zip file 'C:\Source.zip' not found.");
//    }

//    [Fact]
//    public void OpenRead_ReturnsCorrectZipArchive() {
//        // Arrange
//        var zipFile = new MemoryZip();
//        zipFile.Add(@"Path\In\Zip\File.txt", [1, 2, 3]);

//        var sut = new MemoryFs();
//        sut.Add(@"C:\Real\Path\File.zip", zipFile);

//        // Act
//        var zipArchive = sut.FileSystem.ZipFile.OpenRead(@"C:\Real\Path\File.zip");

//        // Assert
//        zipArchive.Should().BeOfType<MemoryZipArchive>();
//        zipArchive.Entries.Should().HaveCount(1);
//        var entry = zipArchive.GetEntry("Path/In/Zip/File.txt");
//        entry.Should().NotBeNull();
//        entry.Open().Should().BeOfType<MemoryStream>().Which.ToArray().Should().BeEquivalentTo([1, 2, 3]);
//    }


//}
