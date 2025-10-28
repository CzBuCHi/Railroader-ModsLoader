using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MemoryFileSystem.Internal;
using MemoryFileSystem.Types;
using Railroader.ModInjector.Wrappers.FileSystem;

namespace MemoryFileSystem;

public interface IMemoryFileSystem
{
    void Add(string folderPath, DateTime? lastWriteTime = null);
    void Add(string filePath, byte[] binaryContent, DateTime? lastWriteTime = null);
    void Add(string filePath, string textContent, DateTime? lastWriteTime = null);
    void Add(string filePath, MemoryZip zipFile, DateTime? lastWriteTime = null);
    void Add(string filePath, Exception exception, DateTime? lastWriteTime = null);
    void Add(MemoryEntry entry);

    string NormalizePath(string path);

    IEnumerable<MemoryEntry> Enumerate(string path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly);

    EntryDictionary Items { get; }
}

//[PublicAPI]
//[DebuggerStepThrough]
public abstract partial class MemoryFileSystemBase : IMemoryFileSystem, IEnumerable<MemoryEntry>
{
    protected MemoryFileSystemBase() => FileSystem = new MemoryFileSystem(this).Mock();

    public IFileSystem     FileSystem { get; }
    public EntryDictionary Items      { get; } = new();

    public abstract string NormalizePath(string path);

    public void LockFile(string path) => SetLock(path, true);

    public void UnlockFile(string path) => SetLock(path, false);

    private void SetLock(string path, bool locked) {
        path = NormalizePath(path);
        Items[path] = Items[path]! with { Locked = locked };
    }
    
    private static Regex ToRegex(string searchPattern) {
        var invalidPathChars = Path.GetInvalidFileNameChars();
        if (searchPattern.Any(o => (o != '?') & (o != '*') && invalidPathChars.Contains(o))) {
            throw new ArgumentException("Invalid search pattern.");
        }

        var regexPattern = searchPattern == "*.*"
            ? $"[^{Path.DirectorySeparatorChar}{Path.AltDirectorySeparatorChar}]*"
            : Regex.Escape(searchPattern).Replace("\\*", ".*").Replace("\\?", ".");

        return new Regex("^" + regexPattern + "$", RegexOptions.IgnoreCase);
    }

    [ExcludeFromCodeCoverage]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<MemoryEntry> GetEnumerator() => Items.Values.OrderBy(o => o.Path).GetEnumerator();

    public IEnumerable<MemoryEntry> Enumerate(string path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly) {
        path = NormalizePath(path);

        // _Items.Keys = [@"C:\", @"C:\Foo", @"C:\Test", @"C:\Test\Dir1", @"C:\Test\Dir2", @"C:\Test\File1.txt", @"C:\Test\Dir3", @"C:\Test\Dir3\SubDir", @"C:\Test\Dir3\File2.txt" ]
        // path = @"C:\Test"

        // filter out all where Key do not start with path
        var query = Items.Where(o => o.Key!.StartsWith(path, StringComparison.OrdinalIgnoreCase));

        // _Items.Keys = [@"C:\Test", @"C:\Test\Dir1", @"C:\Test\Dir2", @"C:\Test\File1.txt", @"C:\Test\Dir3", @"C:\Test\Dir3\SubDir", @"C:\Test\Dir3\File2.txt" ]

        // filter out 'self'
        query = query.Where(o => o.Key!.Length > path.Length);

        // _Items.Keys = [ @"C:\Test\Dir1", @"C:\Test\Dir2", @"C:\Test\File1.txt", @"C:\Test\Dir3", @"C:\Test\Dir3\SubDir", @"C:\Test\Dir3\File2.txt" ]
        if (searchOption == SearchOption.TopDirectoryOnly) {
            // filter out nested entries
            query = query.Where(o => {
                // o.Key one of = [ @"C:\Test\Dir1", @"C:\Test\Dir2", @"C:\Test\File1.txt", @"C:\Test\Dir3", @"C:\Test\Dir3\SubDir", @"C:\Test\Dir3\File2.txt" ]
                var index = o.Key!.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], path.Length + 1);
                if (index == -1) {
                    // o.Key one of [ @"C:\Test\Dir1", @"C:\Test\Dir2", @"C:\Test\File1.txt", @"C:\Test\Dir3" ]
                    return true;
                }

                // o.Key one of [ @"C:\Test\Dir3\SubDir", @"C:\Test\Dir3\File2.txt" ]
                return false;
            });
        }

        var regex = ToRegex(searchPattern);

        // filter out files that do not match pattern
        query = query.Where(o => regex.IsMatch(Path.GetFileName(o.Key!)));

        return query.Select(o => o.Value).OrderBy(o => o!.Path);
    }
}
