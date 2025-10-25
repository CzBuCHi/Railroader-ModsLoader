using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Railroader.ModInjector.Wrappers.FileSystem;

namespace NSubstitute.FileSystem;

public sealed record MemoryEntry(string Path, bool IsDirectory, DateTime LastWriteTime, string? Content, Exception? ReadException, bool Locked = false);

[PublicAPI]
[DebuggerStepThrough]
public sealed class MemoryFileSystem : IEnumerable<MemoryEntry>
{
    public static readonly DateTime First  = new(2000, 1, 1);
    public static readonly DateTime Second = new(2000, 1, 2);
    public static readonly DateTime Third  = new(2000, 1, 3);
    public static readonly DateTime Fourth = new(2000, 1, 4);

    public MemoryFileSystem(string currentDirectory = "C:\\") {
        FileSystem = FileSystemFactory();
        CurrentDirectory = currentDirectory;
    }

    public IFileSystem FileSystem { get; }

    private string _CurrentDirectory = null!;

    public string CurrentDirectory {
        get => _CurrentDirectory;
        set {
            var normalized = NormalizePath(value);
            EnsureDirectory(normalized, First);
            _CurrentDirectory = normalized;
        }
    }

    private readonly EntryDictionary _Items = new();

    public void Add(string folderPath) => AddInternal(new MemoryEntry(folderPath, true, First, null, null));

    public void Add((string Path, DateTime LastWriteTime) folder) => AddInternal(new MemoryEntry(folder.Path!, true, folder.LastWriteTime, null, null));

    public void Add((string Path, string Content) file) => AddInternal(new MemoryEntry(file.Path!, false, First, file.Content, null));

    public void Add((string Path, DateTime LastWriteTime, string Content) file) => AddInternal(new MemoryEntry(file.Path!, false, file.LastWriteTime, file.Content, null));

    public void Add((string Path, Exception LoadException) file) => AddInternal(new MemoryEntry(file.Path!, false, First, null, file.LoadException));

    public void Add((string Path, DateTime LastWriteTime, Exception LoadException) file) => AddInternal(new MemoryEntry(file.Path!, false, file.LastWriteTime, null, file.LoadException));

    public void LockFile(string path) {
        path = NormalizePath(path);
        _Items[path] = _Items[path]! with { Locked = true };
    }

    public void UnlockFile(string path) {
        path = NormalizePath(path);
        _Items[path] = _Items[path]! with { Locked = false };
    }

    private static string NormalizePath(string path) {
        if (string.IsNullOrEmpty(path)) {
            throw new ArgumentException("Path cannot be null or empty.");
        }

        path = Path.GetFullPath(path);

        if (path.Length == 3) {
            return path;
        }

        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private void AddInternal(MemoryEntry entry) {
        entry = entry with { Path = NormalizePath(entry.Path) };

        if (_Items.ContainsKey(entry.Path)) {
            throw new InvalidOperationException($"Path '{entry.Path}' already exists.");
        }

        var path = Path.GetDirectoryName(entry.Path);
        if (path != null) {
            EnsureDirectory(path, entry.LastWriteTime);
        }

        _Items.TryAdd(entry.Path, entry);
    }

    private void EnsureDirectory(string path, DateTime lastWriteTime) {
        var paths = new Stack<string>();

        var current = path;
        while (current != null) {
            paths.Push(current);
            current = Path.GetDirectoryName(current);
        }

        while (paths.Any()) {
            var directoryPath = paths.Pop()!;
            if (_Items.TryGetValue(directoryPath, out var directoryEntry)) {
                if (directoryEntry is not { IsDirectory: true }) {
                    throw new InvalidOperationException($"Path '{directoryPath}' is a file, not a directory.");
                }

                _Items[directoryPath] = directoryEntry with { LastWriteTime = lastWriteTime };
            } else {
                _Items.TryAdd(directoryPath, new MemoryEntry(directoryPath, true, lastWriteTime, null, null));
            }
        }
    }

    private IFileSystem FileSystemFactory() {
        var mock      = Substitute.For<IFileSystem>();
        var directory = DirectoryFactory();
        var file      = FileFactory();
        mock.Directory.Returns(_ => directory);
        mock.File.Returns(_ => file);
        mock.DirectoryInfo(Arg.Any<string>()).Returns(o => DirectoryInfoFactory(o.Arg<string>()));
        return mock;
    }

    private IDirectory DirectoryFactory() {
        var mock = Substitute.For<IDirectory>();
        mock.EnumerateDirectories(Arg.Any<string>()).Returns(o => Directory_EnumerateDirectories(o.Arg<string>()));
        mock.GetCurrentDirectory().Returns(_ => CurrentDirectory);
        return mock;
    }

    private IFile FileFactory() {
        var mock = Substitute.For<IFile>();
        mock.Exists(Arg.Any<string>()).Returns(o => File_Exists(o.Arg<string>()));
        mock.ReadAllText(Arg.Any<string>()).Returns(o => File_ReadAllText(o.Arg<string>()));
        mock.GetLastWriteTime(Arg.Any<string>()).Returns(o => File_GetLastWriteTime(o.Arg<string>()));
        mock.When(o => o.Delete(Arg.Any<string>())).Do(o => File_Delete(o.Arg<string>()));
        mock.When(o => o.Move(Arg.Any<string>(), Arg.Any<string>())).Do(o => File_Move(o.ArgAt<string>(0), o.ArgAt<string>(1)));
        mock.Create(Arg.Any<string>()).Returns(o => File_Create(o.Arg<string>()));
        return mock;
    }

    private IDirectoryInfo DirectoryInfoFactory(string path) {
        var mock = Substitute.For<IDirectoryInfo>();
        mock.EnumerateFiles(Arg.Any<string>(), Arg.Any<SearchOption>()).Returns(o => Directory_EnumerateFiles(path, o.Arg<string>(), o.Arg<SearchOption>()));
        return mock;
    }

    private IFileInfo FileInfoFactory(string path) {
        var mock = Substitute.For<IFileInfo>();
        mock.FullName.Returns(_ => path);
        mock.LastWriteTime.Returns(_ => File_GetLastWriteTime(path));
        return mock;
    }

    private bool File_Exists(string path) {
        path = NormalizePath(path);
        return _Items.TryGetValue(path, out var entry) && entry is { IsDirectory: false };
    }

    private string? File_ReadAllText(string path) {
        path = NormalizePath(path);
        if (_Items.TryGetValue(path, out var entry) && entry is { IsDirectory: false }) {
            if (entry.ReadException != null) {
                throw entry.ReadException;
            }

            return entry.Content;
        }

        throw new FileNotFoundException($"File not found: {path}");
    }

    private DateTime File_GetLastWriteTime(string path) {
        path = NormalizePath(path);
        if (_Items.TryGetValue(path, out var entry) && entry is { IsDirectory: false }) {
            return entry.LastWriteTime;
        }

        throw new FileNotFoundException($"File not found: {path}");
    }

    private void File_Delete(string path) {
        path = NormalizePath(path);
        if (_Items.TryGetValue(path, out var entry) && entry is { IsDirectory: false }) {
            if (entry.Locked) {
                throw new InvalidOperationException($"File {path} is locked");
            }

            _Items.TryRemove(path, out _);
        }
    }

    private readonly object _FileMoveLock = new();

    private void File_Move(string sourceFileName, string destFileName) {
        sourceFileName = NormalizePath(sourceFileName);
        destFileName = NormalizePath(destFileName);

        lock (_FileMoveLock) {
            if (!_Items.TryGetValue(sourceFileName, out var sourceFile) || sourceFile is not { IsDirectory: false }) {
                throw new FileNotFoundException($"Source file not found: {sourceFileName}");
            }

            if (_Items.ContainsKey(destFileName)) {
                throw new InvalidOperationException($"Destination path already exists: {destFileName}");
            }

            if (sourceFile.Locked) {
                throw new InvalidOperationException($"File {sourceFileName} is locked");
            }

            var removed = _Items.TryRemove(sourceFileName, out _);
            // Stryker disable once statement, string
            Debug.Assert(removed, $"Failed to remove source file '{sourceFileName}'.");

            var added = _Items.TryAdd(destFileName, sourceFile with { Path = destFileName });
            // Stryker disable once statement, string
            Debug.Assert(added, $"Failed to add destination file '{destFileName}'.");
        }
    }

    private readonly object _FileCreateLock = new();

    private Stream File_Create(string path) {
        path = NormalizePath(path);

        lock (_FileCreateLock) {
            Add((path, ""));

            var data = new List<byte>();
            return new MemoryFileStream((buffer, offset, count) => data.AddRange(buffer.Skip(offset).Take(count)), () => { _Items[path] = _Items[path]! with { Content = Encoding.UTF8.GetString(data.ToArray()) }; });
        }
    }

    private IEnumerable<string> Directory_EnumerateDirectories(string path)
        => Directory_Enumerate(path, "*.*").Where(o => o.IsDirectory).Select(o => o.Path);

    private IEnumerable<IFileInfo> Directory_EnumerateFiles(string path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        => Directory_Enumerate(path, searchPattern, searchOption).Where(o => !o.IsDirectory).Select(o => FileInfoFactory(o.Path));

    private IEnumerable<MemoryEntry> Directory_Enumerate(string path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly) {
        path = NormalizePath(path);

        // _Items.Keys = [@"C:\", @"C:\Foo", @"C:\Test", @"C:\Test\Dir1", @"C:\Test\Dir2", @"C:\Test\File1.txt", @"C:\Test\Dir3", @"C:\Test\Dir3\SubDir", @"C:\Test\Dir3\File2.txt" ]
        // path = @"C:\Test"

        // filter out all where Key do not start with path
        var query = _Items.Where(o => o.Key!.StartsWith(path, StringComparison.OrdinalIgnoreCase));

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

    public IEnumerator<MemoryEntry> GetEnumerator() => _Items.Values.OrderBy(o => o.Path).GetEnumerator();

    [DebuggerTypeProxy(typeof(EntryDictionaryProxy))]
    private sealed class EntryDictionary() : ConcurrentDictionary<string, MemoryEntry>(StringComparer.OrdinalIgnoreCase);

    [ExcludeFromCodeCoverage]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private sealed class EntryDictionaryProxy(EntryDictionary dictionary)
    {
        public ICollection<string> Keys = dictionary.OrderBy(o => o.Key)
                                                    .Select(o => $"[{(o.Value!.IsDirectory ? "D" : "F")};{o.Value.LastWriteTime:T}] {o.Key}")
                                                    .ToArray();

        public ICollection<MemoryEntry> Values = dictionary.Values;
        public int                      Count => dictionary.Count;
    }

    [ExcludeFromCodeCoverage]
    private sealed class MemoryFileStream(Action<byte[], int, int> write, Action dispose) : Stream
    {
        public override void Flush() {
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing) {
                dispose();
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) {
            write(buffer, offset, count);
        }

        public override bool CanRead  => false;
        public override bool CanSeek  => false;
        public override bool CanWrite => true;
        public override long Length   => 0;
        public override long Position { get; set; }
    }
}
