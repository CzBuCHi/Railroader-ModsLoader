using Mono.Cecil;

namespace Railroader.ModManager.Delegates;

/// <inheritdoc cref="AssemblyDefinition.Write(string)"/>
internal delegate void WriteAssemblyDefinitionDelegate(AssemblyDefinition assemblyDefinition, string fileName);