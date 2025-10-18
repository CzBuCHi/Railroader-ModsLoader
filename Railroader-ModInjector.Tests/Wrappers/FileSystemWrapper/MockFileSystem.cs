using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using JetBrains.Annotations;
using Railroader.ModInjector.Wrappers;

namespace Railroader_ModInterfaces.Tests.Wrappers.FileSystemWrapper;

internal interface IMockItemData;

internal sealed record MockDirectoryData : IMockItemData;

internal sealed record MockFileData(string Content, DateTime LastWriteTime, Exception? LoadException = null) : IMockItemData;

internal sealed record MockFileSystemFile(string FullPath, string Content, DateTime LastWriteTime, Exception? LoadException = null)
{
    public MockFileSystemFile(string fullPath, string content, Exception? loadException = null)
        : this(fullPath, content, DateTime.Now, loadException) {
    }
}

internal sealed class MockFileSystem : IFileSystem, IEnumerable
{
    public MockFileSystem() {
        Items = new MockItemDataDictionary();
        Items.GetOrAdd(@"\", _ => new MockDirectoryData());
    }

    public string CurrentDirectory { get; set; } = @"\";

    public MockItemDataDictionary Items { get; }

    public IDirectory Directory => new MockDirectory(this);
    public IFile      File      => new MockFile(this);
    public IDirectoryInfo DirectoryInfo(string path) => new MockDirectoryInfo(path, this);

    public IEnumerator GetEnumerator() => Items.GetEnumerator();

    public void Add(MockFileSystemFile file) {
        var parts = file.FullPath.Split('\\');

        //["foo", "bar", "baz"]
        var path = "";
        for (var i = 1; i < parts.Length - 1; i++) {
            path += @"\" + parts[i];
            Items.GetOrAdd(path, _ => new MockDirectoryData());
        }

        Items.GetOrAdd(file.FullPath, _ => new MockFileData(file.Content, file.LastWriteTime, file.LoadException));
    }
}

[DebuggerTypeProxy(typeof(MockItemDataDictionaryProxy))]
internal sealed class MockItemDataDictionary : ConcurrentDictionary<string, IMockItemData>;

[ExcludeFromCodeCoverage]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed class MockItemDataDictionaryProxy(MockItemDataDictionary dictionary){
    public string[]                   Keys   => dictionary.Keys.OrderBy(o => o).ToArray();
    public ICollection<IMockItemData> Values => dictionary.Values;
}

public sealed class MockFileSystemTests
{
    private readonly DateTime _Date = DateTime.Now;

    [Fact]
    public void HasImplicitRoot() {
        // Act
        var fileSystem = new MockFileSystem();

        // Assert
        fileSystem.Items.Should().ContainKey(@"\").WhoseValue.Should().BeOfType<MockDirectoryData>();
    }

    [Fact]
    public void AddFileAddsParentDirectories() {
        // Arrange
        // ReSharper disable once UseObjectOrCollectionInitializer
        var fileSystem = new MockFileSystem();

        // Act
        fileSystem.Add(new MockFileSystemFile(@"\path\file.txt", "Content", _Date));

        // Assert
        fileSystem.Items.Should().ContainKey(@"\").WhoseValue.Should().BeOfType<MockDirectoryData>();
        fileSystem.Items.Should().ContainKey(@"\path").WhoseValue.Should().BeOfType<MockDirectoryData>();
        fileSystem.Items.Should().ContainKey(@"\path\file.txt").WhoseValue
                  .Should().BeOfType<MockFileData>().Which
                  .Should().BeEquivalentTo(new {
                      Content = "Content",
                      LastWriteTime = _Date
                  });
    }

    [Fact]
    public void Directory() {
        // Act
        var fileSystem = new MockFileSystem();

        // Assert
        fileSystem.Directory.Should().BeOfType<MockDirectory>();
    }

    [Fact]
    public void File() {
        // Act
        var fileSystem = new MockFileSystem();

        // Assert
        fileSystem.File.Should().BeOfType<MockFile>();
    }

    [Fact]
    public void DirectoryInfo() {
        // Act
        var fileSystem = new MockFileSystem();

        // Assert
        fileSystem.DirectoryInfo(@"\").Should().BeOfType<MockDirectoryInfo>();
    }

    [Fact]
    public void GetEnumerator() {
        // Arrange
        var fileSystem = new MockFileSystem();

        // Act
        // ReSharper disable once GenericEnumeratorNotDisposed
        var enumerator = fileSystem.GetEnumerator();

        // Assert
        var list = new List<object>();
        while (enumerator.MoveNext()) {
            list.Add(enumerator.Current);
        }

        list.Should().HaveCount(1);
        var pair = list[0].Should().BeOfType<KeyValuePair<string, IMockItemData>>().Which;
        pair.Key.Should().Be(@"\");
        pair.Value.Should().BeOfType<MockDirectoryData>();

    }
}
