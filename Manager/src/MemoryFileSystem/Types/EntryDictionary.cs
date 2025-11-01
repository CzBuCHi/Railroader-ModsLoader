using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MemoryFileSystem.Types;

[DebuggerTypeProxy(typeof(EntryDictionaryProxy))]
public sealed class EntryDictionary() : ConcurrentDictionary<string, MemoryEntry>(StringComparer.OrdinalIgnoreCase);