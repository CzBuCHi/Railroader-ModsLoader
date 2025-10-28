using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using FluentAssertions;
using MemoryFileSystem;
using MemoryFileSystem.Internal;
using NSubstitute;
using Railroader.ModManager.Services;
using Serilog;

namespace Railroader.ModManager.Tests.Services;

public sealed class TestsModExtractorService
{
    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_ValidZipWithDefinition_ExtractsToCorrectFolder() {
        // Arrange
        var zipFile = new MemoryZip();
        zipFile.Add("File.txt", "Content");
        zipFile.Add("Definition.json", @"{""id"": ""MyMod"", ""name"": ""My Mod"", ""version"": ""1.0.0""}");

        var fileSystem = new MemoryFs();
        fileSystem.Add( @"C:\Mods\Mod1.zip",  zipFile);
        var logger = Substitute.For<ILogger>();
        var sut = new ModExtractorService {
            FileSystem = fileSystem.FileSystem,
            Logger = logger
        };

        // Act
        sut.ExtractMods();

        // Assert
        fileSystem.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\"),
            new MemoryEntry(@"C:\Mods"),
            new MemoryEntry(@"C:\Mods\Mod1.bak", zipFile.GetBytes()),
            new MemoryEntry(@"C:\Mods\MyMod"),
            new MemoryEntry(@"C:\Mods\MyMod\Definition.json", Encoding.UTF8.GetBytes("""{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""")),
            new MemoryEntry(@"C:\Mods\MyMod\File.txt", Encoding.UTF8.GetBytes("Content"))
        ]);
        logger.Received().Information("Processing mod archive '{ZipPath}' for extraction.", @"C:\Mods\Mod1.zip");
        logger.Received().Information("Successfully extracted mod '{ModId}' from '{ZipPath}' to '{ExtractPath}'.", "MyMod", @"C:\Mods\Mod1.zip", @"C:\Mods\MyMod");
        logger.ReceivedCalls().Should().HaveCount(2);
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_MissingDefinitionJson_SkipsZipAndLogsError() {
        // Arrange
        var zipFile = new MemoryZip();
        zipFile.Add( @"Path\In\Zip\File.txt",  "Content");

        var fileSystem = new MemoryFs();
        fileSystem.Add( @"C:\Mods\Mod1.zip",  zipFile);
        var logger = Substitute.For<ILogger>();
        var sut = new ModExtractorService {
            FileSystem = fileSystem.FileSystem,
            Logger = logger
        };

        // Act
        sut.ExtractMods();

        // Assert
        fileSystem.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\"),
            new MemoryEntry(@"C:\Mods"),
            new MemoryEntry(@"C:\Mods\Mod1.zip", zipFile.GetBytes())
        ]);
        logger.Received().Information("Processing mod archive '{ZipPath}' for extraction.", @"C:\Mods\Mod1.zip");
        logger.Received().Error("Skipping archive '{ZipPath}': Invalid or missing 'Definition.json'.", @"C:\Mods\Mod1.zip");
        logger.ReceivedCalls().Should().HaveCount(2);
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_InvalidDefinitionJson_SkipsZipAndLogsError() {
        // Arrange
        var zipFile = new MemoryZip();
        zipFile.Add("Definition.json", "Invalid JSON");

        var fileSystem = new MemoryFs();
        fileSystem.Add( @"C:\Mods\Mod1.zip",  zipFile);
        var logger = Substitute.For<ILogger>();
        var sut = new ModExtractorService {
            FileSystem = fileSystem.FileSystem,
            Logger = logger
        };

        // Act
        sut.ExtractMods();

        // Assert
        fileSystem.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\"),
            new MemoryEntry(@"C:\Mods"),
            new MemoryEntry(@"C:\Mods\Mod1.zip", zipFile.GetBytes())
        ]);
        logger.Received(1).Error(Arg.Any<Exception>(), "Failed to parse Definition.json in {ZipPath}.", @"C:\Mods\Mod1.zip");
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_MissingRequiredFields_SkipsZipAndLogsError() {
        // Arrange
        var zipFile = new MemoryZip();
        zipFile.Add("Definition.json", """{"version": "1.0.0"}"""); // Missing id and name

        var fileSystem = new MemoryFs();
        fileSystem.Add( @"C:\Mods\Mod1.zip",  zipFile);
        var logger = Substitute.For<ILogger>();
        var sut = new ModExtractorService {
            FileSystem = fileSystem.FileSystem,
            Logger = logger
        };

        // Act
        sut.ExtractMods();

        // Assert
        fileSystem.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\"),
            new MemoryEntry(@"C:\Mods"),
            new MemoryEntry(@"C:\Mods\Mod1.zip", zipFile.GetBytes())
        ]);
        logger.Received().Information("Processing mod archive '{ZipPath}' for extraction.", @"C:\Mods\Mod1.zip");
        logger.Received().Error("Skipping archive '{ZipPath}': Invalid or missing 'Definition.json'.", @"C:\Mods\Mod1.zip");
        logger.ReceivedCalls().Should().HaveCount(2);
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_NoZipFiles_ReturnsEmptyList() {
        // Arrange
        var fileSystem = new MemoryFs();
        var logger     = Substitute.For<ILogger>();
        var sut = new ModExtractorService {
            FileSystem = fileSystem.FileSystem,
            Logger = logger
        };

        // Act
        sut.ExtractMods();

        // Assert
        fileSystem.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\")
        ]);
        logger.DidNotReceive().Error(Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_NestedZip_SkipsRootAndExtractsCorrectly() {
        // Arrange
        var zipFile = new MemoryZip();
        zipFile.Add( @"Path\In\Zip\File.txt",  "Content");

        var zipFile2 = new MemoryZip();
        zipFile2.Add( @"Path\In\Zip\File.txt",  zipFile);

        var zipFile3 = new MemoryZip();
        zipFile3.Add( "Definition.json",  @"{""id"": ""MyMod"", ""name"": ""My Mod"", ""version"": ""1.0.0""}");
        zipFile3.Add( @"Path\In\Zip\File.txt",  zipFile2);

        var fileSystem = new MemoryFs();
        fileSystem.Add( @"C:\Mods\Mod1.zip",  zipFile3);
        var logger = Substitute.For<ILogger>();

        var sut = new ModExtractorService {
            FileSystem = fileSystem.FileSystem,
            Logger = logger
        };

        // Act
        sut.ExtractMods();

        // Assert
        fileSystem.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\"),
            new MemoryEntry(@"C:\Mods"),
            new MemoryEntry(@"C:\Mods\Mod1.bak", zipFile3.GetBytes()),
            new MemoryEntry(@"C:\Mods\MyMod"),
            new MemoryEntry(@"C:\Mods\MyMod\Definition.json", Encoding.UTF8.GetBytes("""{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""")),
            new MemoryEntry(@"C:\Mods\MyMod\Path"),
            new MemoryEntry(@"C:\Mods\MyMod\Path\In"),
            new MemoryEntry(@"C:\Mods\MyMod\Path\In\Zip"),
            new MemoryEntry(@"C:\Mods\MyMod\Path\In\Zip\File.txt", zipFile2.GetBytes())
        ]);
        logger.DidNotReceive().Error(Arg.Any<string>(), Arg.Any<object[]>());
    }
}
