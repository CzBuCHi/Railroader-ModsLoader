using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MemoryFileSystem2.Types;

namespace MemoryFileSystem2;

partial class MemoryFileSystem
{
    public void Add(string folderPath, DateTime? lastWriteTime = null) => Add(new MemoryEntry(folderPath, lastWriteTime));

    public void Add(string filePath, byte[] binaryContent, DateTime? lastWriteTime = null) => Add(new MemoryEntry(filePath, binaryContent, lastWriteTime));

    public void Add(string filePath, string textContent, DateTime? lastWriteTime = null) => Add(new MemoryEntry(filePath, Encoding.UTF8.GetBytes(textContent), lastWriteTime));

    public void Add(string filePath, MemoryZip zipFile, DateTime? lastWriteTime = null) => Add(new MemoryEntry(filePath, zipFile.GetBytes(), lastWriteTime));

    public void Add(string filePath, Exception exception, DateTime? lastWriteTime = null) => Add(new MemoryEntry(filePath, exception, lastWriteTime));

    public void Add(MemoryEntry entry) {
        entry = entry with { Path = NormalizePath(entry.Path) };

        if (Items.ContainsKey(entry.Path)) {
            throw new InvalidOperationException($"Path '{entry.Path}' already exists.");
        }

        var path = GetParentPath(entry.Path);
        if (path != null) {
            VerifyParents(path, entry.LastWriteTime);
        }

        Items.TryAdd(entry.Path, entry);
    }

    public void AddRange(IEnumerable<MemoryEntry> entries) {
        foreach (var entry in entries
                              .Select(o => o with { Path = NormalizePath(o.Path) })
                              .OrderBy(o => o.Path.Length)) {
            Add(entry);
        }
    }
}
