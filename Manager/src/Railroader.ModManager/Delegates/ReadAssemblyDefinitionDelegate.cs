using Mono.Cecil;

namespace Railroader.ModManager.Delegates;

/// <inheritdoc cref="AssemblyDefinition.ReadAssembly(string, ReaderParameters)"/>
internal delegate AssemblyDefinition? ReadAssemblyDefinitionDelegate(string fileName, ReaderParameters parameters);