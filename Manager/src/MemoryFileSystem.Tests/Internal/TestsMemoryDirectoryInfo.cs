using System.IO;
using System.Linq;
using MemoryFileSystem.Internal;
using NSubstitute;
using Shouldly;
using Xunit;

namespace MemoryFileSystem.Tests.Internal;

public sealed class TestsMemoryDirectoryInfo
{
    [Theory]
    [InlineData("txt.*", SearchOption.TopDirectoryOnly)]
    [InlineData("*.txt", SearchOption.AllDirectories)]
    public void EnumerateFiles_WhenDirectoryNotFound(string searchPattern, SearchOption searchOption) {
        // Arrange
        var fileSystem = Substitute.For<IMemoryFileSystem>();
        fileSystem.NormalizePath(Arg.Any<string>()).Returns(o => o.Arg<string>());

        var sut = new MemoryDirectoryInfo(fileSystem, @"C:\Path");

        // Act
        _ = sut.EnumerateFiles(searchPattern, searchOption).ToArray();

        // Assert
        fileSystem.Received(1).Enumerate(@"C:\Path", searchPattern, searchOption);
    }

    [Theory]
    [InlineData(SearchOption.AllDirectories, 2)]
    [InlineData(SearchOption.TopDirectoryOnly, 1)]
    public void EnumerateFiles_WhenDirectoryExists(SearchOption searchOption, int expectedCount) {
        // Arrange
        var fileSystem = new MemoryFs {
            { @"C:\Path\Foo", "Foo" },
            { @"C:\Path\Bar\Baz", "Baz" }
        };
        var sut = new MemoryDirectoryInfo(fileSystem, @"C:\Path");

        // Act
        var actual = sut.EnumerateFiles("*.*", searchOption).ToArray();

        // Assert
        actual.Length.ShouldBe(expectedCount);
    }
}
