using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Collections.Generic;
using ReaderParameters = Mono.Cecil.ReaderParameters;

namespace Railroader.ModManagerInstaller.Abstractions;

public interface IModuleDefinitionStatic
{
    IModuleDefinition? ReadModule(string fileName, ReaderParameters parameters);
}

public interface IModuleDefinition
{
    AssemblyDefinition Assembly { get; }
    Collection<AssemblyNameReference> AssemblyReferences { get; }
    TypeSystem TypeSystem { get; }
    TypeDefinition? GetType(string fullName);
    MethodReference ImportReference(MethodReference method);
    void Write(string fileName);
}

[ExcludeFromCodeCoverage]
public sealed class ModuleDefinitionStatic : IModuleDefinitionStatic
{
    private static IModuleDefinition? CreateWrapper(ModuleDefinition? moduleDefinition) => moduleDefinition != null ? new ModuleDefinitionWrapper(moduleDefinition) : null;

    public IModuleDefinition? ReadModule(string fileName, ReaderParameters parameters) => CreateWrapper(ModuleDefinition.ReadModule(fileName, parameters));
}

[ExcludeFromCodeCoverage]
public sealed class ModuleDefinitionWrapper(ModuleDefinition moduleDefinition) : IModuleDefinition
{
    public AssemblyDefinition Assembly => moduleDefinition.Assembly;
    public Collection<AssemblyNameReference> AssemblyReferences => moduleDefinition.AssemblyReferences;
    public TypeSystem TypeSystem => moduleDefinition.TypeSystem;
    public TypeDefinition? GetType(string fullName) => moduleDefinition.GetType(fullName);
    public MethodReference ImportReference(MethodReference method) => moduleDefinition.ImportReference(method);
    public void Write(string fileName) => moduleDefinition.Write(fileName);
}