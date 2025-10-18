using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using JetBrains.Annotations;
using Railroader.ModInjector.Wrappers;

namespace Railroader_ModInterfaces.Tests.Wrappers.FileSystemWrapper;

[DebuggerDisplay("FileInfo: {Path,nq}")]
internal sealed class MockFileInfo(string path, MockFileSystem fileSystem) : IFileInfo
{
    [UsedImplicitly]
    [ExcludeFromCodeCoverage]
    private string Path => path;

    public DateTime LastWriteTime => ((MockFileData)fileSystem.Items[path]!).LastWriteTime;
    public string   FullName      => path;
}

public sealed class MockFileInfoTests
{
    [Fact]
    public void Properties() {
        // Arrange
        var date       = DateTime.Now;
        var fileSystem = new MockFileSystem();
        fileSystem.Items.GetOrAdd(@"\path", new MockFileData("bar", date));

        var sut = new MockFileInfo(@"\path", fileSystem);

        // Assert
        sut.LastWriteTime.Should().Be(date);
        sut.FullName.Should().Be(@"\path");
    }
}
