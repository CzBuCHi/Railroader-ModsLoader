using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace Railroader.ModManager.Wrappers;

/// <summary> Wrapper for <see cref="AssemblyDefinition"/>. </summary>
internal interface IAssemblyDefinitionWrapper
{
    /// <inheritdoc cref="AssemblyDefinition.ReadAssembly(string, ReaderParameters)"/>
    AssemblyDefinition? ReadAssembly(string fileName, ReaderParameters parameters);

    /// <inheritdoc cref="AssemblyDefinition.Write(string)"/>
    void Write(AssemblyDefinition assemblyDefinition, string fileName);
}

/// <inheritdoc />
[ExcludeFromCodeCoverage]
internal sealed class AssemblyDefinitionWrapper : IAssemblyDefinitionWrapper
{
    /// <inheritdoc />
    public AssemblyDefinition? ReadAssembly(string fileName, ReaderParameters parameters) => AssemblyDefinition.ReadAssembly(fileName, parameters);

    /// <inheritdoc />
    public void Write(AssemblyDefinition assemblyDefinition, string fileName) => assemblyDefinition.Write(fileName);
}
