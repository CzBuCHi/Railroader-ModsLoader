using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Collections.Generic;
using ReaderParameters = Mono.Cecil.ReaderParameters;

namespace Railroader.ModManagerInstaller.Abstractions;

public interface IModuleDefinitionStatic
{
    IModuleDefinition ReadModule(string fileName, ReaderParameters parameters);
}

public interface IModuleDefinition
{
    AssemblyDefinition Assembly { get; }
    Collection<AssemblyNameReference> AssemblyReferences { get; }
    ITypeSystem TypeSystem { get; }
    TypeDefinition? GetType(string fullName);
    MethodReference ImportReference(MethodReference method);
    void Write(string fileName);
}

public interface ITypeSystem
{
    TypeReference Void { get; }
}

[ExcludeFromCodeCoverage]
public sealed class ModuleDefinitionStatic : IModuleDefinitionStatic
{
    public IModuleDefinition ReadModule(string fileName, ReaderParameters parameters) {
        try {
            return new ModuleDefinitionWrapper(ModuleDefinition.ReadModule(fileName, parameters)!);
        } catch (Exception exc) {
            throw new InstallerException("Could not load module", exc);
        }
    }
}

[ExcludeFromCodeCoverage]
public sealed class ModuleDefinitionWrapper(ModuleDefinition moduleDefinition) : IModuleDefinition
{
    public AssemblyDefinition Assembly => moduleDefinition.Assembly;
    public Collection<AssemblyNameReference> AssemblyReferences => moduleDefinition.AssemblyReferences;
    public ITypeSystem TypeSystem => new TypeSystemWrapper(moduleDefinition.TypeSystem);
    public TypeDefinition? GetType(string fullName) => moduleDefinition.GetType(fullName);
    public MethodReference ImportReference(MethodReference method) => moduleDefinition.ImportReference(method);
    public void Write(string fileName) => moduleDefinition.Write(fileName);
}

[ExcludeFromCodeCoverage]
public sealed class TypeSystemWrapper(TypeSystem typeSystem) : ITypeSystem
{
    public TypeReference Void => typeSystem.Void;
}