using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using MemoryFileSystem;
using Newtonsoft.Json;
using NSubstitute;
using Railroader.ModManager.Features;
using Railroader.ModManager.Services;
using Shouldly;

namespace Railroader.ModManager.Tests.Features;

public sealed class TestsModExtractor
{
    [DebuggerStepThrough]
    private static void ExtractAll(IMemoryLogger logger, MemoryFs memoryFs) =>
        ModExtractor.ExtractAll(logger, memoryFs.DirectoryInfo, memoryFs.Directory.Exists, memoryFs.Directory.GetCurrentDirectory, memoryFs.ZipFile.OpenRead, memoryFs.ZipFile.ExtractToDirectory, memoryFs.File.Exists);

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

        var expected = new MemoryFs {
            { @"C:\Mods\Mod1.bak", zipFile },
            { @"C:\Mods\MyMod\Definition.json", Encoding.UTF8.GetBytes("""{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""") },
            { @"C:\Mods\MyMod\File.txt", Encoding.UTF8.GetBytes("Content") }
        };

        // Act
        ExtractAll(logger, memoryFs);

        // Assert
        memoryFs.ShouldBeEquivalentTo(expected);
        logger.Received().Information("Processing mod archive {ZipPath} for extraction.", @"C:\Mods\Mod1.zip");
        logger.Received().Information("Successfully extracted mod {ModId} from {ZipPath} to {ExtractPath}.", "MyMod", @"C:\Mods\Mod1.zip", @"C:\Mods\MyMod");
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_WhenFailsToOpenZip() {
        // Arrange
        var memoryFs = new MemoryFs {
            { @"C:\Mods\Mod1.zip", "ZIP" }
        };

        var logger = Substitute.For<IMemoryLogger>();

        var expected = new MemoryFs {
            { @"C:\Mods\Mod1.zip", "ZIP" }
        };

        // Act
        ExtractAll(logger, memoryFs);

        // Assert
        memoryFs.ShouldBeEquivalentTo(expected);
        logger.Received().Information("Processing mod archive {ZipPath} for extraction.", @"C:\Mods\Mod1.zip");
        logger.Received().Error(Arg.Any<JsonReaderException>(), "Failed to unzip archive {ZipPath}.", @"C:\Mods\Mod1.zip");
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_ValidZipWithDefinition_SkipIfDestinationFolderExits() {
        // Arrange
        var zipFile = new MemoryZip {
            { "File.txt", "Content" },
            { "Definition.json", """{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""" },
        };

        var memoryFs = new MemoryFs {
            { @"C:\Mods\Mod1.zip", zipFile },
            @"C:\Mods\MyMod"
        };

        var logger = Substitute.For<IMemoryLogger>();

        var expected = new MemoryFs {
            { @"C:\Mods\Mod1.dup", zipFile },
            @"C:\Mods\MyMod",
        };

        // Act
        ExtractAll(logger, memoryFs);

        // Assert
        memoryFs.ShouldBeEquivalentTo(expected);
        logger.Received().Information("Processing mod archive {ZipPath} for extraction.", @"C:\Mods\Mod1.zip"); 
        logger.Received().Warning("Extraction path {ExtractPath} already exists – skipping mod {ModId}.", @"C:\Mods\MyMod", "MyMod");
        memoryFs.ZipFile.ExtractToDirectory.DidNotReceive().Invoke(Arg.Any<string>(), Arg.Any<string>());
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

        var expected = new MemoryFs {
            { @"C:\Mods\Mod1.zip", zipFile }
        };

        // Act
        ExtractAll(logger, memoryFs);

        // Assert
        memoryFs.ShouldBeEquivalentTo(expected);
        logger.Received().Information("Processing mod archive {ZipPath} for extraction.", @"C:\Mods\Mod1.zip");
        logger.Received().Error("Skipping archive {ZipPath}: Missing 'Definition.json'.", @"C:\Mods\Mod1.zip");
        logger.DidNotReceive().Error(Arg.Any<Exception>(), "Failed to unzip archive {ZipPath}.", @"C:\Mods\Mod1.zip");
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

        var expected = new MemoryFs {
            { @"C:\Mods\Mod1.zip", zipFile }
        };

        // Act
        ExtractAll(logger, memoryFs);

        // Assert
        memoryFs.ShouldBeEquivalentTo(expected);
        logger.Received().Information("Processing mod archive {ZipPath} for extraction.", @"C:\Mods\Mod1.zip");
        logger.Received().Error(Arg.Any<JsonReaderException>(), "Skipping archive {ZipPath}: Failed to parse Definition.json.", @"C:\Mods\Mod1.zip");
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_MissingRequiredFields_SkipsZipAndLogsError() {
        // Arrange
        var zipFile = new MemoryZip {
            { "Definition.json", """{"id": "id", "version": "1.0.0"}""" }
        };

        var memoryFs = new MemoryFs {
            { @"C:\Mods\Mod1.zip", zipFile }
        };

        var logger = Substitute.For<IMemoryLogger>();

        var expected = new MemoryFs {
            { @"C:\Mods\Mod1.zip", zipFile }
        };

        // Act
        ExtractAll(logger, memoryFs);

        // Assert
        memoryFs.ShouldBeEquivalentTo(expected);
        logger.Received().Information("Processing mod archive {ZipPath} for extraction.", @"C:\Mods\Mod1.zip");
        logger.Received().Error("Skipping archive {ZipPath}: Invalid mod definition.", @"C:\Mods\Mod1.zip");
        memoryFs.Directory.Exists.DidNotReceive().Invoke(Arg.Any<string>());
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_NoZipFiles_ReturnsEmptyList() {
        // Arrange
        var memoryFs = new MemoryFs {
            { @"C:\Mods\Mod1.txt", [1, 2, 3] }
        };

        var logger = Substitute.For<IMemoryLogger>();

        var expected = new MemoryFs {
            { @"C:\Mods\Mod1.txt", [1, 2, 3] }
        };

        // Act
        ExtractAll(logger, memoryFs);

        // Assert
        memoryFs.ShouldBeEquivalentTo(expected);
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

        var expected = new MemoryFs {
            { @"C:\Mods\Mod1.bak", zipFile2 },
            { @"C:\Mods\MyMod\Definition.json", Encoding.UTF8.GetBytes("""{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""") },
            { @"C:\Mods\MyMod\File.zip", zipFile }
        };

        // Act
        ExtractAll(logger, memoryFs);

        // Assert
        memoryFs.ShouldBeEquivalentTo(expected);
        logger.DidNotReceive().Error(Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_InvalidZip() {
        // Arrange
        var memoryFs = new MemoryFs {
            { @"C:\Mods\Mod1.zip", [1, 2, 3] }
        };

        var logger = Substitute.For<IMemoryLogger>();

        var expected = new MemoryFs {
            { @"C:\Mods\Mod1.zip", [1, 2, 3] }
        };

        // Act
        ExtractAll(logger, memoryFs);

        // Assert
        memoryFs.ShouldBeEquivalentTo(expected);
        logger.Received().Information("Processing mod archive {ZipPath} for extraction.", @"C:\Mods\Mod1.zip");
        logger.Received().Error(Arg.Any<JsonReaderException>(), "Failed to unzip archive {ZipPath}.", @"C:\Mods\Mod1.zip");
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_MoveZipToBackup() {
        // Arrange
        var zipFile = new MemoryZip {
            { "File.txt", "Content" },
            { "Definition.json", """{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""" }
        };

        var memoryFs = new MemoryFs {
            { @"C:\Mods\Mod1.zip", zipFile },
            { @"C:\Mods\Mod1.bak", "BAK" },
        };

        var logger = Substitute.For<IMemoryLogger>();

        var expected = new MemoryFs {
            { @"C:\Mods\Mod1.bak", "BAK" },
            { @"C:\Mods\Mod1.bak1", zipFile },
            { @"C:\Mods\MyMod\Definition.json", Encoding.UTF8.GetBytes("""{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""") },
            { @"C:\Mods\MyMod\File.txt", Encoding.UTF8.GetBytes("Content") }
        };

        // Act
        ExtractAll(logger, memoryFs);

        // Assert
        memoryFs.ShouldBeEquivalentTo(expected);
        logger.Received().Information("Processing mod archive {ZipPath} for extraction.", @"C:\Mods\Mod1.zip");
        logger.Received().Information("Successfully extracted mod {ModId} from {ZipPath} to {ExtractPath}.", "MyMod", @"C:\Mods\Mod1.zip", @"C:\Mods\MyMod");
    }

    [Fact]
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public void ExtractMods_WhenZipMoveFails() {
        // Arrange
        var zipFile = new MemoryZip {
            { "File.txt", "Content" },
            { "Definition.json", """{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""" }
        };

        var memoryFs = new MemoryFs {
            { @"C:\Mods\Mod1.zip", zipFile },
            { @"C:\Mods\Mod1.bak", "BAK" },
        };

        var logger = Substitute.For<IMemoryLogger>();

        var expected = new MemoryFs {
            { @"C:\Mods\Mod1.bak", "BAK" },
            { @"C:\Mods\Mod1.bak1", zipFile },
            { @"C:\Mods\MyMod\Definition.json", Encoding.UTF8.GetBytes("""{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""") },
            { @"C:\Mods\MyMod\File.txt", Encoding.UTF8.GetBytes("Content") }
        };

        // Act
        ModExtractor.ExtractAll(logger, memoryFs.DirectoryInfo, memoryFs.Directory.Exists, memoryFs.Directory.GetCurrentDirectory, memoryFs.ZipFile.OpenRead, memoryFs.ZipFile.ExtractToDirectory, memoryFs.File.Exists);

        // Assert
        memoryFs.ShouldBeEquivalentTo(expected);
        logger.Received().Information("Processing mod archive {ZipPath} for extraction.", @"C:\Mods\Mod1.zip");
        logger.Received().Information("Successfully extracted mod {ModId} from {ZipPath} to {ExtractPath}.", "MyMod", @"C:\Mods\Mod1.zip", @"C:\Mods\MyMod");
    }

    [Fact]
    public void ExtractionActuallyWritesFiles()
    {
        // Arrange
        var zip = new MemoryZip {
            { "Definition.json", "{ \"id\": \"M\", \"name\": \"X\", \"version\": \"1.0\" }" },
            { "data.bin", new byte[] { 1,2,3 } }
        };
        var fs     = new MemoryFs { { @"C:\Mods\X.zip", zip } };
        var logger = Substitute.For<IMemoryLogger>();

        // Act
        ExtractAll(logger, fs);

        // Assert
        logger.Received().Information("Processing mod archive {ZipPath} for extraction.", @"C:\Mods\X.zip");
        logger.Received().Information("Successfully extracted mod {ModId} from {ZipPath} to {ExtractPath}.", "M", @"C:\Mods\X.zip", @"C:\Mods\M");
    }

    [Fact]
    public void SuccessfulExtraction_MovesZipToBackup()
    {
        // Arrange
        var zip    = new MemoryZip { { "Definition.json", "{ \"id\": \"M\", \"version\": \"1.0\", \"name\": \"X\" }" } };
        var fs     = new MemoryFs { { @"C:\Mods\X.zip", zip } };
        var logger = Substitute.For<IMemoryLogger>();

        // Act
        ExtractAll(logger, fs);

        // Assert
        fs.File.Exists(@"C:\Mods\X.zip").ShouldBeFalse();
        fs.File.Exists(@"C:\Mods\X.bak").ShouldBeTrue();
    }

    [Fact]
    public void BackupNameCollision_UsesIncrementalSuffix()
    {
        // Arrange
        var zip = new MemoryZip { { "Definition.json", "{ \"id\": \"M\", \"version\": \"1.0\", \"name\": \"X\" }" } };
        var fs = new MemoryFs {
            { @"C:\Mods\X.zip", zip },
            { @"C:\Mods\X.bak",  "occupied" },
            { @"C:\Mods\X.bak1", "occupied" }
        };
        var logger = Substitute.For<IMemoryLogger>();

        // Act
        ExtractAll(logger, fs);

        // Assert
        fs.File.Exists(@"C:\Mods\X.bak2").ShouldBeTrue();   // needs i++ twice
    }
}
