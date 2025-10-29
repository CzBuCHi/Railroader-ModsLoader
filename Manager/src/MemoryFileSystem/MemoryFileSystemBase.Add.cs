//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using MemoryFileSystem.Internal;

//namespace MemoryFileSystem;

//partial class MemoryFileSystemBase
//{
//    public void Add(string folderPath, DateTime? lastWriteTime = null) => Add(new MemoryEntry(folderPath, lastWriteTime));

//    public void Add(string filePath, byte[] binaryContent, DateTime? lastWriteTime = null) => Add(new MemoryEntry(filePath, binaryContent, lastWriteTime));

//    public void Add(string filePath, string textContent, DateTime? lastWriteTime = null) => Add(new MemoryEntry(filePath, Encoding.UTF8.GetBytes(textContent), lastWriteTime));

//    public void Add(string filePath, MemoryZip zipFile, DateTime? lastWriteTime = null) => Add(new MemoryEntry(filePath, zipFile.GetBytes(), lastWriteTime));

//    public void Add(string filePath, Exception exception, DateTime? lastWriteTime = null) => Add(new MemoryEntry(filePath, exception, lastWriteTime));

//    public void Add(MemoryEntry entry) {
//        entry = entry with { Path = NormalizePath(entry.Path) };

//        if (Items.ContainsKey(entry.Path)) {
//            throw new InvalidOperationException($"Path '{entry.Path}' already exists.");
//        }

//        var path = GetParentPath(entry.Path);
//        if (path != null) {
//            VerifyParents(path, entry.LastWriteTime);
//        }

//        Items.TryAdd(entry.Path, entry);
//    }

//    public void AddRange(IEnumerable<MemoryEntry> entries) {
//        foreach (var entry in entries
//                              .Select(o => o with { Path = NormalizePath(o.Path) })
//                              .OrderBy(o => o.Path.Length)) {
//            Add(entry);
//        }
//    }


//    protected abstract string? GetParentPath(string path);

//    protected void VerifyParents(string folderPath, DateTime? lastWriteTime = null) {
//        lastWriteTime ??= MemoryEntry.DefaultLastWriteTime;
//        var paths = new Stack<string>();

//        var current = folderPath;
//        while (current is { Length: > 0 }) {
//            paths.Push(current);
//            current = Path.GetDirectoryName(current);
//        }

//        while (paths.Any()) {
//            var directoryPath = paths.Pop()!;
//            if (Items.TryGetValue(directoryPath, out var directoryEntry)) {
//                if (directoryEntry is not { IsDirectory: true }) {
//                    throw new InvalidOperationException($"Path '{directoryPath}' is a file, not a directory.");
//                }

//                Items[directoryPath] = directoryEntry with { LastWriteTime = lastWriteTime.Value };
//            } else {
//                Items.TryAdd(directoryPath, new MemoryEntry(directoryPath, lastWriteTime.Value));
//            }
//        }
//    }
//}
