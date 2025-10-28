using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using MemoryFileSystem.Internal;

namespace MemoryFileSystem.Types;

[ExcludeFromCodeCoverage]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal sealed class EntryDictionaryProxy(EntryDictionary dictionary)
{
    public ICollection<string> Keys = dictionary.OrderBy(o => o.Key)
                                                .Select(o => $"[{(o.Value!.IsDirectory ? "D" : "F")};{o.Value.LastWriteTime:T}] {o.Key}")
                                                .ToArray();

    public ICollection<MemoryEntry> Values = dictionary.Values;
    public int                      Count => dictionary.Count;
}