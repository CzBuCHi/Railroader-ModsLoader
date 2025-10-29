using System.IO;

namespace Railroader.ModManager.Delegates;

/// <inheritdoc cref="Mono.CSharp.CompilerCallableEntryPoint.InvokeCompiler"/>
internal delegate bool InvokeCompilerDelegate(string[] args, TextWriter error);