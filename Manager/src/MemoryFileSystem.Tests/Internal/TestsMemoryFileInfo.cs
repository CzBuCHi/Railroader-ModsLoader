//using System;
//using System.IO;
//using System.Text;
//using FluentAssertions;
//using MemoryFileSystem.Internal;
//using MemoryFileSystem.Types;
//using NSubstitute;
//using Xunit;

//namespace MemoryFileSystem.Tests.Internal;

//public sealed class TestsMemoryFileInfo
//{
//    private IMemoryFileSystem CreateMemoryFileSystem(EntryDictionary entries) {
//        var fileSystem = Substitute.For<IMemoryFileSystem>();
//        fileSystem.Items.Returns(entries);
//        fileSystem.NormalizePath(Arg.Any<string>()).Returns(o => o.Arg<string>().ToLower());
//        return fileSystem;
//    }

//    [Fact]
//    public void GetLastWriteTime_WhenValid() {
//        // Arrange
//        var entries = new EntryDictionary();
//        entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path", Encoding.UTF8.GetBytes("Content")));
//        var fileSystem = CreateMemoryFileSystem(entries);
//        var sut        = new MemoryFileInfo(fileSystem, @"C:\path").Mock();

//        // Act
//        var actual = sut.LastWriteTime;

//        // Assert
//        actual.Should().Be(MemoryEntry.DefaultLastWriteTime);
//    }

//    [Theory]
//    [InlineData(false)]
//    [InlineData(true)]
//    public void GetLastWriteTime_ThrowsWhenNotFoundOrDirectory(bool isDirectory) {
//        // Arrange
//        var entries = new EntryDictionary();
//        if (isDirectory) {
//            entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path"));
//        }

//        var fileSystem = CreateMemoryFileSystem(entries);
//        var sut        = new MemoryFileInfo(fileSystem, @"C:\path").Mock();

//        // Act
//        var act = () => sut.LastWriteTime;

//        // Assert
//        act.Should().Throw<FileNotFoundException>().WithMessage(@"File not found: c:\path");
//    }

//    [Fact]
//    public void FullName_ReturnsNormalized() {
//        // Arrange
//        var fileSystem = CreateMemoryFileSystem([]);
//        var sut        = new MemoryFileInfo(fileSystem, @"C:\path").Mock();

//        // Act
//        var actual = sut.FullName;

//        // Assert
//        actual.Should().Be(@"c:\path");
//    }

//    [Theory]
//    [InlineData(false)]
//    [InlineData(true)]
//    public void Move_WhenSourceNotFoundOrDirectory(bool isDirectory) {
//        // Arrange
//        var entries    = new EntryDictionary();
//        if (isDirectory) {
//            entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path"));
//        }

//        var fileSystem = CreateMemoryFileSystem(entries);
//        var sut        = new MemoryFileInfo(fileSystem, @"C:\path").Mock();

//        // Act
//        var act = () => sut.MoveTo(@"C:\target");

//        // Assert
//        act.Should().Throw<FileNotFoundException>().WithMessage(@"Source file not found: 'C:\path'.");
//    }

//    [Fact]
//    public void Move_WhenDestinationExists() {
//        // Arrange
//        var entries = new EntryDictionary();
//        entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path", [1, 2, 3]));
//        entries.TryAdd(@"c:\target", new MemoryEntry(@"c:\target", [1, 2, 3]));


//        var fileSystem = CreateMemoryFileSystem(entries);
//        var sut        = new MemoryFileInfo(fileSystem, @"C:\path").Mock();

//        // Act
//        var act = () => sut.MoveTo(@"C:\target");

//        // Assert
//        act.Should().Throw<InvalidOperationException>().WithMessage(@"Destination path already exists: 'C:\target'.");
//    }

//    [Fact]
//    public void Move_WhenSourceLocked() {
//        // Arrange
//        var entries = new EntryDictionary();
//        entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path", false, MemoryEntry.DefaultLastWriteTime, null, null, true));

//        var fileSystem = CreateMemoryFileSystem(entries);
//        var sut        = new MemoryFileInfo(fileSystem, @"C:\path").Mock();

//        // Act
//        var act = () => sut.MoveTo(@"C:\target");

//        // Assert
//        act.Should().Throw<InvalidOperationException>().WithMessage(@"File 'c:\path' is locked.");
//    }

//    [Fact]
//    public void Move_WhenValid() {
//        // Arrange
//        var entries = new EntryDictionary();
//        entries.TryAdd(@"c:\path", new MemoryEntry(@"c:\path", [1, 2, 3]));

//        var fileSystem = CreateMemoryFileSystem(entries);
//        var sut        = new MemoryFileInfo(fileSystem, @"c:\path").Mock();

//        // Act
//        sut.MoveTo(@"C:\target");

//        // Assert
//        sut.FullName.Should().Be(@"c:\target");
//        entries.Should().HaveCount(1);
//        entries.Should().ContainKey(@"c:\target").WhoseValue.Should().BeEquivalentTo(new MemoryEntry(@"c:\target", [1, 2, 3]));
//    }
//}
