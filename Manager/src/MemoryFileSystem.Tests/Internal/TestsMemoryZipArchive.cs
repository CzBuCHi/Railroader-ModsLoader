//using System.Linq;
//using FluentAssertions;
//using MemoryFileSystem.Internal;
//using Xunit;

//namespace MemoryFileSystem.Tests.Internal;

//public sealed class TestsMemoryZipArchive
//{
//    [Fact]
//    public void Entries() {
//        // Arrange
//        var memoryZip = new MemoryZip {
//            { "Foo", [1, 2, 3] },
//            { @"Bar\Baz", [1, 2, 3] }
//        };
//        var sut = new MemoryZipArchive(memoryZip).Mock();

//        var expected = new[] {
//            new { FullName = "Bar/Baz", Name = "Baz" },
//            new { FullName = "Foo", Name = "Foo" }
//        };

//        // Act
//        var entries = sut.Entries.ToArray();

//        // Assert
//        entries.Should().BeEquivalentTo(expected);
//    }

//    [Fact]
//    public void GetEntry_ReturnNullWhenNotFound() {
//        // Arrange
//        var memoryZip = new MemoryZip();
//        var sut       = new MemoryZipArchive(memoryZip).Mock();

//        // Act
//        var entry = sut.GetEntry("Foo");

//        // Assert
//        entry.Should().BeNull();
//    }

//    [Fact]
//    public void GetEntry_ReturnCorrectEntry() {
//        // Arrange
//        var memoryZip = new MemoryZip {
//            { "Foo", [1, 2, 3] },
//            { @"Bar\Baz", [1, 2, 3] }
//        };
//        var sut = new MemoryZipArchive(memoryZip).Mock();

//        var expected = new { FullName = "Bar/Baz", Name = "Baz" };

//        // Act
//        var entry = sut.GetEntry("Bar/Baz");

//        // Assert
//        entry.Should().BeEquivalentTo(expected);
//    }
//}