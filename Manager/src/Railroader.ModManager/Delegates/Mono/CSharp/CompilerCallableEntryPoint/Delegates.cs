// resharper disable all

using _CompilerCallableEntryPoint = Mono.CSharp.CompilerCallableEntryPoint;
using _TextWriter = System.IO.TextWriter;

namespace Railroader.ModManager.Delegates.Mono.CSharp.CompilerCallableEntryPoint;

/// <inheritdoc cref="_CompilerCallableEntryPoint.InvokeCompiler(string[], _TextWriter)"/>
/// <remarks> Wraps <see cref="_CompilerCallableEntryPoint.InvokeCompiler(string[],_TextWriter)"/> for testability. </remarks>
public delegate bool InvokeCompiler(string[] args, _TextWriter error);
