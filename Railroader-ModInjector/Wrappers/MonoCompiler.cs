using System.Diagnostics.CodeAnalysis;
using System.IO;
using Mono.CSharp;

namespace Railroader.ModInjector.Wrappers;

// wrapper around mono compiler to simplify testing

public interface IMonoCompiler {
    bool Compile(string[] args, TextWriter error);
}

[ExcludeFromCodeCoverage]
public sealed class MonoCompiler : IMonoCompiler {
    public bool Compile(string[] args, TextWriter error) => CompilerCallableEntryPoint.InvokeCompiler(args, error);
}
