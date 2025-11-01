using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MemoryFileSystem2.Types;

[DebuggerTypeProxy(typeof(EntryDictionaryProxy))]
public sealed class EntryDictionary() : ConcurrentDictionary<string, MemoryEntry>(StringComparer.OrdinalIgnoreCase);