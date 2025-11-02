using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Shouldly;

namespace MemoryFileSystem.Tests.TestExtensions;

[DebuggerStepThrough]
[ShouldlyMethods]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ShouldBeDictionaryTestExtensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ShouldContainKeyWhereValue<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        TKey key,
        Action<TValue> valueAssertion,
        string? customMessage = null
    ) where TKey : notnull {
        dictionary.ShouldContainKey(key, customMessage);
        valueAssertion(dictionary[key]);
    }
}
