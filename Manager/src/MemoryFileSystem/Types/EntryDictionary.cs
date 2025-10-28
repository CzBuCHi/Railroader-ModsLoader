using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using MemoryFileSystem.Internal;

namespace MemoryFileSystem.Types;

[DebuggerTypeProxy(typeof(EntryDictionaryProxy))]
public sealed class EntryDictionary() : ConcurrentDictionary<string, MemoryEntry>(StringComparer.OrdinalIgnoreCase);