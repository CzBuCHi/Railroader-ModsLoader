using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using MemoryFileSystem2.Types;
using Xunit;

namespace MemoryFileSystem2.Tests;

public sealed class TestsMemoryFileSystemBase
{
    [Fact]
    public void LockFile()
    {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl {
            { "C:\\File.txt", [1, 2, 3] }
        };
        // Act
        sut.LockFile("C:\\File.txt");

        // Assert
        sut.Items.Should().ContainKey("C:\\File.txt").WhoseValue.Locked.Should().BeTrue();
    }

    [Fact]
    public void UnlockFile()
    {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl {
            new MemoryEntry("C:\\File.txt", false, MemoryEntry.DefaultLastWriteTime, [1, 2, 3], null, true)
        };

        // Act
        sut.UnlockFile("C:\\File.txt");

        // Assert
        sut.Items.Should().ContainKey("C:\\File.txt").WhoseValue.Locked.Should().BeFalse();
    }

    [Fact]
    public void Enumerate_TopDirectoryOnly_MatchesPattern()
    {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl {
            @"C:\Test\Dir1",                      // Directory
            @"C:\Test\Dir2",                      // Directory
            { @"C:\Test\File1.txt", [1, 2, 3] },  // File
            { @"C:\Test\File2.doc", [4, 5, 6] },  // File
            @"C:\Test\Dir3",                      // Directory
            @"C:\Test\Dir3\SubDir",               // Nested Directory
            { @"C:\Test\Dir3\File3.txt", [7, 8] } // Nested File
        };

        // Act
        var result = sut.Enumerate(@"C:\Test", "*.txt").ToList();

        // Assert
        result.Should().BeEquivalentTo([new MemoryEntry(@"c:\test\file1.txt", [1, 2, 3])]);
    }

    [Fact]
    public void Enumerate_AllDirectories_MatchesPattern()
    {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl {
            @"C:\Test\Dir1",
            @"C:\Test\Dir2",
            { @"C:\Test\File1.txt", [1, 2, 3] },
            { @"C:\Test\File2.doc", [4, 5, 6] },
            @"C:\Test\Dir3",
            @"C:\Test\Dir3\SubDir",
            { @"C:\Test\Dir3\File3.txt", [7, 8] }
        };

        // Act
        var result = sut.Enumerate(@"C:\Test", "*.txt", SearchOption.AllDirectories).ToList();

        // Assert
        result.Should().BeEquivalentTo([
            new MemoryEntry(@"c:\test\dir3\file3.txt", [7, 8]),
            new MemoryEntry(@"c:\test\file1.txt", [1, 2, 3]),
        ]);

    }

    [Fact]
    public void Enumerate_NoMatchingFiles_ReturnsEmpty()
    {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl {
            @"C:\Test\Dir1",
            { @"C:\Test\File1.doc", [1, 2, 3] }
        };

        // Act
        var result = sut.Enumerate(@"C:\Test", "*.txt").ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Enumerate_InvalidSearchPattern_ThrowsArgumentException()
    {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl {
            { @"C:\Test\File1.txt", [1, 2, 3] }
        };

        // Act
        var act = () => sut.Enumerate(@"C:\Test", "File<1>.txt").ToArray();

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Invalid search pattern.");
    }

    [Fact]
    public void Enumerate_CaseInsensitivePathAndPattern_MatchesCorrectly()
    {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl {
            { @"C:\TEST\FILE1.TXT", [1, 2, 3] },
            { @"C:\test\file2.txt", [4, 5, 6] }
        };

        // Act
        var result = sut.Enumerate(@"c:\test", "*.TXT").ToList();

        // Assert
        result.Should().BeEquivalentTo([
            new MemoryEntry(@"c:\test\file1.txt", [1, 2, 3]),
            new MemoryEntry(@"c:\test\file2.txt", [4, 5, 6]),
        ]);
    }

    [Fact]
    public void Enumerate_WildcardPattern_MatchesAllFiles()
    {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl {
            @"C:\Test\Dir1",
            { @"C:\Test\File1.txt", [1, 2, 3] },
            { @"C:\Test\File2.doc", [4, 5, 6] }
        };

        // Act
        var result = sut.Enumerate(@"C:\Test", "*.*").ToList();

        // Assert
        result.Should().BeEquivalentTo([
            new MemoryEntry(@"c:\test\dir1"),
            new MemoryEntry(@"c:\test\file1.txt", [1, 2, 3]),
            new MemoryEntry(@"c:\test\file2.doc", [4, 5, 6]),
        ]);
    }

    private static readonly MemoryEntry[] _EnumerateSpecificWildcardPatternsMatchesCorrectlyEntries = [
        // @formatter:off
        new(@"c:\test\__.__",  [0 ] ),
        new(@"c:\test\-.__",   [1 ] ),
        new(@"c:\test\__.-",   [2 ] ),
        new(@"c:\test\-.-",    [3 ] ),

        new(@"c:\test\a__.__", [4 ] ),
        new(@"c:\test\a-.__",  [5 ] ),
        new(@"c:\test\a__.-",  [6 ] ),
        new(@"c:\test\a-.-",   [7 ] ),

        new(@"c:\test\__b.__", [8 ] ),
        new(@"c:\test\-b.__",  [9 ] ),
        new(@"c:\test\__b.-",  [10] ),
        new(@"c:\test\-b.-",   [11] ),

        new(@"c:\test\__.c__", [12] ),
        new(@"c:\test\-.c__",  [13] ),
        new(@"c:\test\__.c-",  [14] ),
        new(@"c:\test\-.c-",   [15] ),

        new(@"c:\test\__.__d", [16] ),
        new(@"c:\test\-.__d",  [17] ),
        new(@"c:\test\__.-d",  [18] ),
        new(@"c:\test\-.-d",   [19] ),
        // @formatter:on
    ];

    public static IEnumerable<object?[]> Enumerate_SpecificWildcardPatterns_MatchesCorrectlyData()
    {
        return Enumerate().Select(o => new object?[] {
            o.searchPattern,
            o.entries!.Select(p => _EnumerateSpecificWildcardPatternsMatchesCorrectlyEntries[p]).ToArray()
        });

        IEnumerable<(string searchPattern, int[] entries)> Enumerate()
        {
            // @formatter:off

            // Core wildcard patterns
            yield return ("*.*", [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19]);
            yield return ("?.*", [1, 3, 13, 15, 17, 19]);
            yield return ("*.?", [2, 3, 6, 7, 10, 11,]);
            yield return ("?.?", [3]);
            yield return ("a*.*", [4, 5, 6, 7]);
            yield return ("a?.*", [5, 7]);
            yield return ("a*.?", [6, 7]);
            yield return ("a?.?", [7]);
            yield return ("*b.*", [8, 9, 10, 11]);
            yield return ("?b.*", [9, 11]);
            yield return ("*b.?", [10, 11]);
            yield return ("?b.?", [11]);
            yield return ("*.c*", [12, 13, 14, 15]);
            yield return ("?.c*", [13, 15]);
            yield return ("*.c?", [14, 15]);
            yield return ("?.c?", [15]);
            yield return ("*.*d", [16, 17, 18, 19]);
            yield return ("?.*d", [17, 19]);
            yield return ("*.?d", [18, 19]);
            yield return ("?.?d", [19]);
            // @formatter:on
        }
    }

    [Theory]
    [MemberData(nameof(Enumerate_SpecificWildcardPatterns_MatchesCorrectlyData))]
    public void Enumerate_SpecificWildcardPatterns_MatchesCorrectly(string searchPattern, MemoryEntry[] entries)
    {
        // Arrange
        var sut = new MemoryFileSystemBaseImpl();
        sut.AddRange(_EnumerateSpecificWildcardPatternsMatchesCorrectlyEntries);

        // Act
        var result = sut.Enumerate(@"C:\Test", searchPattern).ToList();

        // Assert
        result.Should().BeEquivalentTo(entries, o => o.WithoutStrictOrdering(), "pattern is {0}", searchPattern);
    }

    private sealed class MemoryFileSystemBaseImpl : MemoryFileSystem
    {
        public override string NormalizePath(string path) => path.ToLower();

        protected override string? GetParentPath(string path) => Path.GetDirectoryName(path);
    }
}
