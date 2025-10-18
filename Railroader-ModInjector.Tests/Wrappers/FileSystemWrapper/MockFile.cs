using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Railroader.ModInjector.Wrappers;

namespace Railroader_ModInterfaces.Tests.Wrappers.FileSystemWrapper;

internal sealed class MockFile(MockFileSystem fileSystem) : IFile
{
    public bool Exists(string path) => fileSystem.Items.TryGetValue(path, out var data) && data is MockFileData;

    public string ReadAllText(string path) {
        if (!fileSystem.Items.TryGetValue(path, out var data) || data is not MockFileData fileData) {
            throw new FileNotFoundException("File not found", path);
        }

        return fileData.LoadException != null ? throw fileData.LoadException : fileData.Content;
    }

    public DateTime GetLastWriteTime(string path) => ((MockFileData)fileSystem.Items[path]!).LastWriteTime;

    public void Delete(string path) => fileSystem.Items.TryRemove(path, out _);

    public void Move(string sourceFileName, string destFileName) {
        if (!fileSystem.Items.TryGetValue(sourceFileName, out var data)) {
            return;
        }

        fileSystem.Items.TryRemove(sourceFileName, out _);
        fileSystem.Items[destFileName] = data;
    }
}

public sealed class MockFileTests : IAsyncLifetime
{
    private readonly DateTime       _Date       = DateTime.Now;
    private          MockFileSystem _FileSystem = null!;
    private          MockFile       _File       = null!;

    public Task InitializeAsync() {
        _FileSystem = new MockFileSystem();
        _FileSystem.Items.GetOrAdd(@"\path", new MockFileData("content", _Date));
        _FileSystem.Items.GetOrAdd(@"\error", new MockFileData("content", _Date, new IOException()));
        _File = new MockFile(_FileSystem);

        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(@"\path", true)]
    [InlineData(@"\", false)]
    [InlineData(@"\other", false)]
    public void Exists(string path, bool expected) {
        // Act
        var actual = _File.Exists(path);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void ReadAllText_Success() {
        // Act
        var actual = _File.ReadAllText(@"\path");

        // Assert
        actual.Should().Be("content");
    }

    [Fact]
    public void ReadAllText_NotExistingFile() {
        // Act
        Action act = () => _File.ReadAllText(@"\not_exist");

        // Assert
        act.Should().Throw<FileNotFoundException>().Which.FileName.Should().Be(@"\not_exist");
    }

    [Fact]
    public void ReadAllText_LoadError() {
        // Act
        Action act = () => _File.ReadAllText(@"\error");

        // Assert
        act.Should().Throw<IOException>();
    }

    [Fact]
    public void GetLastWriteTime() {
        // Act
        var actual = _File.GetLastWriteTime(@"\path");

        // Assert
        actual.Should().Be(_Date);
    }

    [Fact]
    public void Delete() {
        // Act
        _File.Delete(@"\path");

        // Assert
        _FileSystem.Items.Should().NotContainKey(@"\path");
    }

    [Fact]
    public void Move_WhenExists() {
        // Act
        _File.Move(@"\path", @"\new_path");

        // Assert
        _FileSystem.Items.Should().NotContainKey(@"\path");
        _FileSystem.Items
                   .Should().ContainKey(@"\new_path").WhoseValue
                   .Should().BeOfType<MockFileData>().Which
                   .Should().BeEquivalentTo(new { Content = "content", LastWriteTime = _Date });
    }

    [Fact]
    public void Move_WhenNotExists() {
        // Act
        _File.Move(@"\invalid", @"\new_path");

        // Assert
        _FileSystem.Items.Should().NotContainKey(@"\invalid");
        _FileSystem.Items.Should().NotContainKey(@"\new_path");
    }
}
