using System.Linq;
using Mono.Cecil;

namespace Railroader.ModManager.Patchers;

internal interface ITypePatcher
{
    bool Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition);
}

public abstract class TypePatcher(IMethodPatcher[] methodPatchers) : ITypePatcher
{
    public bool Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition)
        => methodPatchers.Select(o => o.Patch(assemblyDefinition, typeDefinition)).Any(o => o);
}
