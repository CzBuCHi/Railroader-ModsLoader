using _AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using _ReaderParameters = Mono.Cecil.ReaderParameters;

namespace Railroader.ModManager.Delegates.Mono.Cecil;

/// <inheritdoc cref="_AssemblyDefinition.ReadAssembly(string, _ReaderParameters)"/>
/// /// <remarks> Wraps <see cref="_AssemblyDefinition.ReadAssembly(string, _ReaderParameters)"/> for testability. </remarks>
public delegate _AssemblyDefinition? ReadAssemblyDefinition(string fileName, _ReaderParameters parameters);

/// <inheritdoc cref="_AssemblyDefinition.Write(string)"/>
/// /// /// <remarks> Wraps <see cref="_AssemblyDefinition.Write(string)"/> for testability. </remarks>
public delegate void WriteAssemblyDefinition(_AssemblyDefinition assemblyDefinition, string fileName);
