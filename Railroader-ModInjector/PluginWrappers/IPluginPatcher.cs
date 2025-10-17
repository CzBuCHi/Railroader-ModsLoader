using Mono.Cecil;
using Railroader.ModInterfaces;

namespace Railroader.ModInjector.PluginWrappers;

/// <summary> Defines a contract for patching plugin types in an assembly by modifying or creating the <c>OnIsEnabledChanged</c> method. </summary>
internal interface IPluginPatcher
{
    /// <summary> Patches the specified type in the assembly by injecting a call to a static method into <c>OnIsEnabledChanged</c>. </summary>
    /// <param name="assemblyDefinition">The assembly to patch. Must not be null.</param>
    /// <param name="typeDefinition">The type to patch, which must derive from <see cref="PluginBase"/> and implement the patcher's marker interface. Must not be null.</param>
    void Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition);
}
