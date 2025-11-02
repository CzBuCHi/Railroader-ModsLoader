using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace MemoryFileSystem.Types;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed record MemoryEntry
{
    public static readonly DateTime DefaultLastWriteTime = new(2000, 1, 1);

    public MemoryEntry(string folderPath, DateTime? lastWriteTime = null, bool locked = false)
        : this(folderPath, true, lastWriteTime ?? DefaultLastWriteTime, null, null, locked) {
    }

    public MemoryEntry(string filePath, byte[] content, DateTime? lastWriteTime = null, bool locked = false)
        : this(filePath, false, lastWriteTime ?? DefaultLastWriteTime, content, null, locked) {
    }

    public MemoryEntry(string filePath, Exception exception, DateTime? lastWriteTime = null, bool locked = false)
        : this(filePath, false, lastWriteTime ?? DefaultLastWriteTime, null, exception, locked) {
    }

    [method: JsonConstructor]
    private MemoryEntry(string Path, bool IsDirectory, DateTime LastWriteTime, byte[]? Content, Exception? ReadException, bool Locked) {
        this.Path = Path;
        this.IsDirectory = IsDirectory;
        this.LastWriteTime = LastWriteTime;
        this.Content = Content;
        this.ReadException = ReadException;
        this.Locked = Locked;
    }
    
    public void CheckLock() {
        if (Locked) {
            throw new InvalidOperationException($"File '{Path}' is locked.");
        }
    }

    [JsonIgnore]
    [ExcludeFromCodeCoverage]
    private string DebuggerDisplay => $"[{(IsDirectory ? "D" : "F")}] {Path}";

    public string     Path          { get; init; }
    public bool       IsDirectory   { get; init; }
    public DateTime   LastWriteTime { get; init; }
    public byte[]?    Content       { get; init; }
    public Exception? ReadException { get; init; }
    public bool       Locked        { get; init; }
}
