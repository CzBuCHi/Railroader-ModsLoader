using System;
using System.Linq;
using System.Text;
using MemoryFileSystem2.Types;
using Newtonsoft.Json;

namespace MemoryFileSystem2;

public sealed class MemoryZip : MemoryFileSystem
{
    public MemoryZip() {
    }

    public MemoryZip(byte[] bytes) {
        var entries = GetEntries(bytes);

        foreach (var entry in entries) {
            Items.TryAdd(entry.Path, entry);
        }
    }

    public override string NormalizePath(string path) {
        if (string.IsNullOrEmpty(path)) {
            throw new ArgumentException("Path cannot be null or empty.");
        }

        if (path.Contains(':')) {
            throw new ArgumentException("Zip file do not support absolute paths.");
        }

        path = path.Replace('\\', '/');
        if (path.StartsWith("/")) {
            path = path.Substring(1); // Remove leading "/"
        }

        return path;
    }

    protected override string? GetParentPath(string path) {
        var index = path.LastIndexOf('/');
        return index == -1 ? null : path.Substring(0, index);
    }

    public byte[] GetBytes() => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Items.Values.ToArray()));

    private static MemoryEntry[] GetEntries(byte[] bytes) => JsonConvert.DeserializeObject<MemoryEntry[]>(Encoding.UTF8.GetString(bytes))!;
}