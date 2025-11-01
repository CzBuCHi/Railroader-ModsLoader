using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;

namespace MemoryFileSystem2.Types;

[ExcludeFromCodeCoverage]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class EntryDictionaryProxy(EntryDictionary dictionary)
{
    public ICollection<string> Keys = dictionary.OrderBy(o => o.Key)
                                                .Select(o => $"[{(o.Value!.IsDirectory ? "D" : "F")};{o.Value.LastWriteTime:T}] {o.Key}")
                                                .ToArray();

    public ICollection<MemoryEntry> Values = dictionary.Values;
    public int                      Count => dictionary.Count;
}