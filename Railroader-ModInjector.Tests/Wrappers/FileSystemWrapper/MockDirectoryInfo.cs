using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Railroader.ModInjector.Wrappers;

namespace Railroader_ModInterfaces.Tests.Wrappers.FileSystemWrapper;

[DebuggerDisplay("DirectoryInfo: {Path,nq}")]
internal sealed class MockDirectoryInfo(string path, MockFileSystem fileSystem) : IDirectoryInfo
{
    [UsedImplicitly]
    [ExcludeFromCodeCoverage]
    private string Path => path;

    public IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly) {
        IEnumerable<string> query = fileSystem.Items
                                              .Where(o => o.Key!.StartsWith($@"{path}\") && o.Value is MockFileData)
                                              .Select(o => o.Key);
        
        if (searchOption == SearchOption.TopDirectoryOnly) {
            query = query.Where(o => o!.IndexOf('\\', path.Length + 1) == -1);
        }

        if (searchPattern != "*.*") {
            var regexPattern = $"^{Regex.Escape(searchPattern).Replace(@"\?", ".").Replace(@"\*", ".*")}$";
            query = query.Where(o => Regex.IsMatch(System.IO.Path.GetFileName(o), regexPattern, RegexOptions.IgnoreCase));
        }

        return query.Select(o => new MockFileInfo(o!, fileSystem));
    }
}

public sealed class MockDirectoryInfoTests : IAsyncLifetime
{
    private readonly DateTime       _Date       = DateTime.Now;
    private          MockFileSystem _FileSystem = null!;

    public Task InitializeAsync() {
        _FileSystem = new MockFileSystem();
        _FileSystem.Items.GetOrAdd(@"\path", new MockDirectoryData());
        _FileSystem.Items.GetOrAdd(@"\path\foo", new MockDirectoryData());
        _FileSystem.Items.GetOrAdd(@"\path\bar", new MockFileData("bar", _Date));
        _FileSystem.Items.GetOrAdd(@"\path\bar\baz", new MockFileData("baz", _Date));

        _FileSystem.Items.GetOrAdd(@"\fizz\foo.cs"      , new MockFileData("foo", _Date));
        _FileSystem.Items.GetOrAdd(@"\fizz\bar.cs"      , new MockFileData("foo", _Date));
        _FileSystem.Items.GetOrAdd(@"\fizz\baz.css"     , new MockFileData("foo", _Date));
        _FileSystem.Items.GetOrAdd(@"\fizz\buzz\bar.cs" , new MockFileData("bar", _Date));
        _FileSystem.Items.GetOrAdd(@"\fizz\baz.dll"     , new MockFileData("baz", _Date));
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public void EnumerateFiles_TopDirectoryOnly() {
        // Arrange
        var sut = new MockDirectoryInfo(@"\path", _FileSystem);

        // Act
        var actual = sut.EnumerateFiles("*.*").ToArray();

        // Assert
        actual.Should().HaveCount(1);
        actual.Should().ContainEquivalentOf(new { FullName = @"\path\bar", LastWriteTime = _Date });
    }

    [Fact]
    public void EnumerateFiles_AllDirectories() {
        // Arrange
        var sut = new MockDirectoryInfo(@"\path", _FileSystem);

        // Act
        var actual = sut.EnumerateFiles("*.*", SearchOption.AllDirectories).ToArray();

        // Assert
        actual.Should().HaveCount(2);
        actual.Should().ContainEquivalentOf(new { FullName = @"\path\bar", LastWriteTime = _Date });
        actual.Should().ContainEquivalentOf(new { FullName = @"\path\bar\baz", LastWriteTime = _Date });
    }

    [Fact]
    public void EnumerateFiles_SpecificFiles() {
        // Arrange
        var sut = new MockDirectoryInfo(@"\fizz", _FileSystem);

        // Act
        var actual = sut.EnumerateFiles("ba?.c*", SearchOption.AllDirectories).ToArray();

        // Assert
        actual.Should().HaveCount(3);
        actual.Should().ContainEquivalentOf(new { FullName = @"\fizz\bar.cs"     , LastWriteTime = _Date });
        actual.Should().ContainEquivalentOf(new { FullName = @"\fizz\baz.css"    , LastWriteTime = _Date });
        actual.Should().ContainEquivalentOf(new { FullName = @"\fizz\buzz\bar.cs", LastWriteTime = _Date });
        

    }
}

