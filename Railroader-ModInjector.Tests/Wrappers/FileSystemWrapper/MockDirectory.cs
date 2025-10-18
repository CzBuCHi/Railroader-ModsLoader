using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Railroader.ModInjector.Wrappers;

namespace Railroader_ModInterfaces.Tests.Wrappers.FileSystemWrapper;

internal sealed class MockDirectory(MockFileSystem fileSystem) : IDirectory
{
    public IEnumerable<string> EnumerateDirectories(string path)
        => fileSystem.Items
                     .Where(o => o.Key!.StartsWith($@"{path}\") && o.Value is MockDirectoryData)
                     .Select(o => o.Key)
                     .Where(o => o!.IndexOf('\\', path.Length + 1) == -1);

    public string GetCurrentDirectory() => fileSystem.CurrentDirectory;
}

public sealed class MockDirectoryTests
{
    [Fact]
    public void EnumerateDirectories() {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.Items.GetOrAdd(@"\path", new MockDirectoryData());
        fileSystem.Items.GetOrAdd(@"\path\foo", new MockDirectoryData());
        fileSystem.Items.GetOrAdd(@"\path\bar", new MockDirectoryData());
        fileSystem.Items.GetOrAdd(@"\path\bar\baz", new MockDirectoryData());

        var sut = new MockDirectory(fileSystem);

        // Act
        var actual = sut.EnumerateDirectories(@"\path").ToArray();

        // Assert
        actual.Should().BeEquivalentTo(@"\path\foo", @"\path\bar");
    }

    [Fact]
    public void GetCurrentDirectory() {
        // Arrange
        const string currentDirectory = @"\Current\Directory";
        var fileSystem = new MockFileSystem {
            CurrentDirectory = currentDirectory
        };

        var sut = new MockDirectory(fileSystem);

        // Act
        var actual = sut.GetCurrentDirectory();

        // Assert
        actual.Should().Be(currentDirectory);
    }
}
