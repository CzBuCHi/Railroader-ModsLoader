using System.Diagnostics.CodeAnalysis;
using System.Text;
using FluentAssertions;
using MemoryFileSystem2;
using MemoryFileSystem2.Types;
using Newtonsoft.Json;
using NSubstitute;
using Railroader.ModManager.Features;
using Railroader.ModManager.Services;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestsModExtractor
{
    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_ValidZipWithDefinition_ExtractsToCorrectFolder() {
        // Arrange
        var zipFile = new MemoryZip {
            { "File.txt", "Content" },
            { "Definition.json", """{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""" }
        };

        var memoryFs = new MemoryFs {
            { @"C:\Mods\Mod1.zip", zipFile }
        };

        var logger = Substitute.For<IMemoryLogger>();

        // Act
        ModExtractor.ExtractMods(logger, memoryFs.DirectoryInfo, memoryFs.Directory.GetCurrentDirectory, memoryFs.ZipFile.OpenRead, memoryFs.ZipFile.ExtractToDirectory);

        // Assert
        memoryFs.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\"),
            new MemoryEntry(@"C:\Mods"),
            new MemoryEntry(@"C:\Mods\Mod1.bak", zipFile.GetBytes()),
            new MemoryEntry(@"C:\Mods\MyMod"),
            new MemoryEntry(@"C:\Mods\MyMod\Definition.json", Encoding.UTF8.GetBytes("""{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""")),
            new MemoryEntry(@"C:\Mods\MyMod\File.txt", Encoding.UTF8.GetBytes("Content"))
        ]);
        logger.Received().Information("Processing mod archive '{ZipPath}' for extraction.", @"C:\Mods\Mod1.zip");
        logger.Received().Information("Successfully extracted mod '{ModId}' from '{ZipPath}' to '{ExtractPath}'.", "MyMod", @"C:\Mods\Mod1.zip", @"C:\Mods\MyMod");
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_MissingDefinitionJson_SkipsZipAndLogsError() {
        // Arrange
        var zipFile = new MemoryZip {
            { "File.txt", "Content" }
        };

        var memoryFs = new MemoryFs {
            { @"C:\Mods\Mod1.zip", zipFile }
        };

        var logger = Substitute.For<IMemoryLogger>();

        // Act
        ModExtractor.ExtractMods(logger, memoryFs.DirectoryInfo, memoryFs.Directory.GetCurrentDirectory, memoryFs.ZipFile.OpenRead, memoryFs.ZipFile.ExtractToDirectory);


        // Assert
        memoryFs.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\"),
            new MemoryEntry(@"C:\Mods"),
            new MemoryEntry(@"C:\Mods\Mod1.zip", zipFile.GetBytes())
        ]);
        logger.Received().Information("Processing mod archive '{ZipPath}' for extraction.", @"C:\Mods\Mod1.zip");
        logger.Received().Error("Skipping archive '{ZipPath}': Missing 'Definition.json'.", @"C:\Mods\Mod1.zip");
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_InvalidDefinitionJson_SkipsZipAndLogsError() {
        // Arrange
        var zipFile = new MemoryZip {
            { "Definition.json", "Invalid JSON" }
        };

        var memoryFs = new MemoryFs {
            { @"C:\Mods\Mod1.zip", zipFile }
        };

        var logger = Substitute.For<IMemoryLogger>();

        // Act
        ModExtractor.ExtractMods(logger, memoryFs.DirectoryInfo, memoryFs.Directory.GetCurrentDirectory, memoryFs.ZipFile.OpenRead, memoryFs.ZipFile.ExtractToDirectory);

        // Assert
        memoryFs.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\"),
            new MemoryEntry(@"C:\Mods"),
            new MemoryEntry(@"C:\Mods\Mod1.zip", zipFile.GetBytes())
        ]);
        logger.Received(1).Error(Arg.Any<JsonException>(), "Skipping archive '{ZipPath}': Failed to parse Definition.json.", @"C:\Mods\Mod1.zip");
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_MissingRequiredFields_SkipsZipAndLogsError() {
        // Arrange
        var zipFile = new MemoryZip {
            { "Definition.json", """{"version": "1.0.0"}""" }
        };

        var memoryFs = new MemoryFs {
            { @"C:\Mods\Mod1.zip", zipFile }
        };

        var logger = Substitute.For<IMemoryLogger>();

        // Act
        ModExtractor.ExtractMods(logger, memoryFs.DirectoryInfo, memoryFs.Directory.GetCurrentDirectory, memoryFs.ZipFile.OpenRead, memoryFs.ZipFile.ExtractToDirectory);


        // Assert
        memoryFs.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\"),
            new MemoryEntry(@"C:\Mods"),
            new MemoryEntry(@"C:\Mods\Mod1.zip", zipFile.GetBytes())
        ]);
        logger.Received().Information("Processing mod archive '{ZipPath}' for extraction.", @"C:\Mods\Mod1.zip");
        logger.Received().Error("Skipping archive '{ZipPath}': Invalid 'Definition.json'.", @"C:\Mods\Mod1.zip");
        logger.ReceivedCalls().Should().HaveCount(2);
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_NoZipFiles_ReturnsEmptyList() {
        // Arrange
        var memoryFs = new MemoryFs {
            { @"C:\Mods\Mod1.txt", [1, 2, 3] }
        };

        var logger = Substitute.For<IMemoryLogger>();

        // Act
        ModExtractor.ExtractMods(logger, memoryFs.DirectoryInfo, memoryFs.Directory.GetCurrentDirectory, memoryFs.ZipFile.OpenRead, memoryFs.ZipFile.ExtractToDirectory);

        // Assert
        memoryFs.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\"),
            new MemoryEntry(@"C:\Mods"),
            new MemoryEntry(@"C:\Mods\Mod1.txt", [1, 2, 3])
        ]);
        logger.DidNotReceive().Error(Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_NestedZip_SkipsRootAndExtractsCorrectly() {
        // Arrange
        var zipFile = new MemoryZip {
            { "ZipPath\\File.txt", "Content" }
        };

        var zipFile2 = new MemoryZip {
            { "Definition.json", """{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""" },
            { "File.zip", zipFile }
        };

        var memoryFs = new MemoryFs {
            { @"C:\Mods\Mod1.zip", zipFile2 }
        };

        var logger = Substitute.For<IMemoryLogger>();

        // Act
        ModExtractor.ExtractMods(logger, memoryFs.DirectoryInfo, memoryFs.Directory.GetCurrentDirectory, memoryFs.ZipFile.OpenRead, memoryFs.ZipFile.ExtractToDirectory);


        // Assert
        memoryFs.Should().BeEquivalentTo([
            new MemoryEntry(@"C:\"),
            new MemoryEntry(@"C:\Mods"),
            new MemoryEntry(@"C:\Mods\Mod1.bak", zipFile2.GetBytes()),
            new MemoryEntry(@"C:\Mods\MyMod"),
            new MemoryEntry(@"C:\Mods\MyMod\Definition.json", Encoding.UTF8.GetBytes("""{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""")),
            new MemoryEntry(@"C:\Mods\MyMod\File.zip", zipFile.GetBytes())
        ]);
        logger.DidNotReceive().Error(Arg.Any<string>(), Arg.Any<object[]>());
    }
}
