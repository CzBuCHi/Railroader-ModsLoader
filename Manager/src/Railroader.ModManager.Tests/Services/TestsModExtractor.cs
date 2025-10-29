//using System.Diagnostics.CodeAnalysis;
//using System.Text;
//using FluentAssertions;
//using MemoryFileSystem;
//using MemoryFileSystem.Internal;
//using Newtonsoft.Json;
//using NSubstitute;

//namespace Railroader.ModManager.Tests.Services;

//public sealed class TestsModExtractor
//{
//    [Fact]
//    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
//    public void ExtractMods_ValidZipWithDefinition_ExtractsToCorrectFolder() {
//        // Arrange
//        var serviceManager = new TestServiceManager();

//        var zipFile = new MemoryZip {
//            { "File.txt", "Content" },
//            { "Definition.json", """{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""" }
//        };

//        serviceManager.MemoryFs.Add(@"C:\Mods\Mod1.zip", zipFile);

//        var sut = serviceManager.CreateModExtractor();

//        // Act
//        sut.ExtractMods();

//        // Assert
//        serviceManager.MemoryFs.Should().BeEquivalentTo([
//            new MemoryEntry(@"C:\"),
//            new MemoryEntry(@"C:\Mods"),
//            new MemoryEntry(@"C:\Mods\Mod1.bak", zipFile.GetBytes()),
//            new MemoryEntry(@"C:\Mods\MyMod"),
//            new MemoryEntry(@"C:\Mods\MyMod\Definition.json", Encoding.UTF8.GetBytes("""{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""")),
//            new MemoryEntry(@"C:\Mods\MyMod\File.txt", Encoding.UTF8.GetBytes("Content"))
//        ]);
//        serviceManager.MemoryLogger.Received().Information("Processing mod archive '{ZipPath}' for extraction.", @"C:\Mods\Mod1.zip");
//        serviceManager.MemoryLogger.Received().Information("Successfully extracted mod '{ModId}' from '{ZipPath}' to '{ExtractPath}'.", "MyMod", @"C:\Mods\Mod1.zip", @"C:\Mods\MyMod");
//    }

//    [Fact]
//    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
//    public void ExtractMods_MissingDefinitionJson_SkipsZipAndLogsError() {
//        // Arrange
//        var serviceManager = new TestServiceManager();

//        var zipFile = new MemoryZip {
//            { "File.txt", "Content" }
//        };

//        serviceManager.MemoryFs.Add(@"C:\Mods\Mod1.zip", zipFile);

//        var sut = serviceManager.CreateModExtractor();

//        // Act
//        sut.ExtractMods();

//        // Assert
//        serviceManager.MemoryFs.Should().BeEquivalentTo([
//            new MemoryEntry(@"C:\"),
//            new MemoryEntry(@"C:\Mods"),
//            new MemoryEntry(@"C:\Mods\Mod1.zip", zipFile.GetBytes())
//        ]);
//        serviceManager.MemoryLogger.Received().Information("Processing mod archive '{ZipPath}' for extraction.", @"C:\Mods\Mod1.zip");
//        serviceManager.MemoryLogger.Received().Error("Skipping archive '{ZipPath}': Missing 'Definition.json'.", @"C:\Mods\Mod1.zip");
//    }

//    [Fact]
//    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
//    public void ExtractMods_InvalidDefinitionJson_SkipsZipAndLogsError() {
//        // Arrange
//        var serviceManager = new TestServiceManager();

//        var zipFile = new MemoryZip {
//            { "Definition.json", "Invalid JSON" }
//        };

//        serviceManager.MemoryFs.Add(@"C:\Mods\Mod1.zip", zipFile);

//        var sut = serviceManager.CreateModExtractor();

//        // Act
//        sut.ExtractMods();

//        // Assert
//        serviceManager.MemoryFs.Should().BeEquivalentTo([
//            new MemoryEntry(@"C:\"),
//            new MemoryEntry(@"C:\Mods"),
//            new MemoryEntry(@"C:\Mods\Mod1.zip", zipFile.GetBytes())
//        ]);
//        serviceManager.MemoryLogger.Received(1).Error(Arg.Any<JsonException>(), "Skipping archive '{ZipPath}': Failed to parse Definition.json.", @"C:\Mods\Mod1.zip");
//    }

//    [Fact]
//    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
//    public void ExtractMods_MissingRequiredFields_SkipsZipAndLogsError() {
//        // Arrange
//        var serviceManager = new TestServiceManager();

//        var zipFile = new MemoryZip {
//            { "Definition.json", """{"version": "1.0.0"}""" }
//        };

//        serviceManager.MemoryFs.Add(@"C:\Mods\Mod1.zip", zipFile);

//        var sut = serviceManager.CreateModExtractor();

//        // Act
//        sut.ExtractMods();

//        // Assert
//        serviceManager.MemoryFs.Should().BeEquivalentTo([
//            new MemoryEntry(@"C:\"),
//            new MemoryEntry(@"C:\Mods"),
//            new MemoryEntry(@"C:\Mods\Mod1.zip", zipFile.GetBytes())
//        ]);
//        serviceManager.MemoryLogger.Received().Information("Processing mod archive '{ZipPath}' for extraction.", @"C:\Mods\Mod1.zip");
//        serviceManager.MemoryLogger.Received().Error("Skipping archive '{ZipPath}': Invalid 'Definition.json'.", @"C:\Mods\Mod1.zip");
//        serviceManager.MemoryLogger.ReceivedCalls().Should().HaveCount(2);
//    }

//    [Fact]
//    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
//    public void ExtractMods_NoZipFiles_ReturnsEmptyList() {
//        // Arrange
//        var serviceManager = new TestServiceManager();

//        serviceManager.MemoryFs.Add(@"C:\Mods\Mod1.txt", [1, 2, 3]);

//        var sut = serviceManager.CreateModExtractor();

//        // Act
//        sut.ExtractMods();

//        // Assert
//        serviceManager.MemoryFs.Should().BeEquivalentTo([
//            new MemoryEntry(@"C:\"),
//            new MemoryEntry(@"C:\Mods"),
//            new MemoryEntry(@"C:\Mods\Mod1.txt", [1, 2, 3]),
//        ]);
//        serviceManager.MemoryLogger.DidNotReceive().Error(Arg.Any<string>(), Arg.Any<object[]>());
//    }

//    [Fact]
//    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
//    public void ExtractMods_NestedZip_SkipsRootAndExtractsCorrectly() {
//        // Arrange
//        var serviceManager = new TestServiceManager();

//        var zipFile = new MemoryZip {
//            { "ZipPath\\File.txt", "Content" }
//        };

//        var zipFile2 = new MemoryZip {
//            { "Definition.json", """{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""" },
//            { "File.zip", zipFile }
//        };

//        serviceManager.MemoryFs.Add(@"C:\Mods\Mod1.zip", zipFile2);

//        var sut = serviceManager.CreateModExtractor();

//        // Act
//        sut.ExtractMods();

//        // Assert
//        serviceManager.MemoryFs.Should().BeEquivalentTo([
//            new MemoryEntry(@"C:\"),
//            new MemoryEntry(@"C:\Mods"),
//            new MemoryEntry(@"C:\Mods\Mod1.bak", zipFile2.GetBytes()),
//            new MemoryEntry(@"C:\Mods\MyMod"),
//            new MemoryEntry(@"C:\Mods\MyMod\Definition.json", Encoding.UTF8.GetBytes("""{"id": "MyMod", "name": "My Mod", "version": "1.0.0"}""")),
//            new MemoryEntry(@"C:\Mods\MyMod\File.zip", zipFile.GetBytes())
//        ]);
//        serviceManager.MemoryLogger.DidNotReceive().Error(Arg.Any<string>(), Arg.Any<object[]>());
//    }
//}
