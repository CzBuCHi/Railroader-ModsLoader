using System.Diagnostics.CodeAnalysis;
using System.IO;
using Mono.CSharp;

namespace Railroader.ModInjector.Wrappers;

/// <summary> Wrapper for <see cref="CompilerCallableEntryPoint"/>. </summary>
internal interface ICompilerCallableEntryPoint
{
    /// <inheritdoc cref="Mono.CSharp.CompilerCallableEntryPoint.InvokeCompiler"/>
    bool InvokeCompiler(string[] args, TextWriter error);
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
internal sealed class CompilerCallableEntryPointWrapper : ICompilerCallableEntryPoint
{
    /// <inheritdoc />
    public bool InvokeCompiler(string[] args, TextWriter error) => CompilerCallableEntryPoint.InvokeCompiler(args, error);
}
