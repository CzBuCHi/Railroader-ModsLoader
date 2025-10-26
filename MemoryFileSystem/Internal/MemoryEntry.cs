using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;

namespace MemoryFileSystem.Internal;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[method: JsonConstructor]
public sealed record MemoryEntry(string Path, bool IsDirectory, DateTime LastWriteTime, byte[]? Content, Exception? ReadException, bool Locked)
{
    public static readonly DateTime DefaultLastWriteTime = new(2000, 1, 1);

    public MemoryEntry(string folderPath, DateTime? lastWriteTime = null)
        : this(folderPath, true, lastWriteTime ?? DefaultLastWriteTime, null, null, false) {
    }

    public MemoryEntry(string filePath, byte[] content, DateTime? lastWriteTime = null)
        : this(filePath, false, lastWriteTime ?? DefaultLastWriteTime, content, null, false) {
    }

    public MemoryEntry(string filePath, Exception exception, DateTime? lastWriteTime = null)
        : this(filePath, false, lastWriteTime ?? DefaultLastWriteTime, null, exception, false) {
    }

    [JsonIgnore]
    public byte[] ExistingContent => IsDirectory || Content == null
        ? throw new InvalidDataException($"Entry at '{Path}' is directory or its content is missing.")
        : Content;

    public void CheckLock() {
        if (Locked) {
            throw new InvalidOperationException($"File '{Path}' is locked.");
        }
    }

    [JsonIgnore]
    [ExcludeFromCodeCoverage]
    private string DebuggerDisplay => $"[{(IsDirectory ? "D" : "F")}] {Path}";
}
