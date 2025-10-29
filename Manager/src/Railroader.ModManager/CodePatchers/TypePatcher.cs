using System.Linq;
using Mono.Cecil;

namespace Railroader.ModManager.CodePatchers;

internal interface ITypePatcher
{
    bool Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition);
}

public abstract class TypePatcher : ITypePatcher
{
    private readonly IMethodPatcher[] _MethodPatchers;

    internal TypePatcher(IMethodPatcher[] methodPatchers) {
        _MethodPatchers = methodPatchers;
    }

    public bool Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition)
        => _MethodPatchers.Select(o => o.Patch(assemblyDefinition, typeDefinition)).Any(o => o);
}
