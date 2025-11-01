using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using FluentAssertions;
using FluentAssertions.Equivalency;
using JetBrains.Annotations;
using MemoryFileSystem.Types;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("Railroader.ModManager.Tests.MyTestFramework", "Railroader.ModManager.Tests")]

namespace Railroader.ModManager.Tests;

[UsedImplicitly]
[ExcludeFromCodeCoverage]
public sealed class MyTestFramework : XunitTestFramework
{
    public MyTestFramework(IMessageSink messageSink)
        : base(messageSink) {
        Expression<Func<IMemberInfo, bool>> excluding = p =>
            // MemoryEntry.ExistingContent throws is used on directory on file with no content
            p.DeclaringType == typeof(MemoryEntry) && p.Name == nameof(MemoryEntry.ExistingContent);

        AssertionConfiguration.Current.Equivalency.Modify(o => o.WithStrictOrdering().Excluding(excluding));
        //AssertionOptions.AssertEquivalencyUsing(o => o.WithStrictOrdering().Excluding(excluding));
    }
}
