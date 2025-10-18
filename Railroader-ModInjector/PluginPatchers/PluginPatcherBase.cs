using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.ModInjector.PluginPatchers;

/// <summary>
/// Provides a base implementation for patching plugin types by injecting a call to a static <c>OnIsEnabledChanged</c> method
/// into the <c>OnIsEnabledChanged</c> method of types implementing a specific marker interface.
/// </summary>
/// <typeparam name="TPlugin">The marker interface that identifies plugin types to patch.</typeparam>
/// <typeparam name="TPluginPatcher">The patcher type, used to resolve the static <c>OnIsEnabledChanged</c> method.</typeparam>
/// <param name="logger">The logger for diagnostic messages. Must not be null.</param>
public abstract class PluginPatcherBase<TPlugin, TPluginPatcher>(ILogger logger) : IPluginPatcher
    where TPlugin : class
    where TPluginPatcher : PluginPatcherBase<TPlugin, TPluginPatcher>
{
    public void Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition)
    {
        var isPluginBase = IsDerivedFromPluginBase(typeDefinition);
        var isTPlugin = typeDefinition.Interfaces?.Any(i => i.InterfaceType?.FullName == typeof(TPlugin).FullName) ?? false;
        if (!isPluginBase || !isTPlugin) {
            logger.Debug("Skipping patching for type {TypeName}: not derived from PluginBase or does not implement {PluginInterface}", typeDefinition.FullName, typeof(TPlugin).FullName);
            return;
        }

        var module = assemblyDefinition.MainModule!;

        // Find or create the OnIsEnabledChanged method
        var method = typeDefinition.Methods?.FirstOrDefault(m => m.Name == "OnIsEnabledChanged")
                     ?? CreateOnIsEnabledChangedOverride(typeDefinition, module);

        // Import the patcher method
        var onIsEnabledChangedMethodInfo = typeof(TPluginPatcher).GetMethod("OnIsEnabledChanged", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)!;
        var onIsEnabledChangedMethod = module.ImportReference(onIsEnabledChangedMethodInfo)!;

        // Inject patcher call (for BOTH existing AND created methods)
        InjectPatcherCall(method, onIsEnabledChangedMethod);

        logger.Information("Successfully patched OnIsEnabledChanged in {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(TPlugin).FullName);
    }

    private static void InjectPatcherCall(MethodDefinition method, MethodReference patcherMethod)
    {
        var ilProcessor = method.Body!.GetILProcessor()!;
        var instructions = method.Body.Instructions;
        var hasPatcherCall = instructions!.Any(i => i.OpCode == OpCodes.Call && i.Operand == patcherMethod);

        if (hasPatcherCall) {
            return;
        }

        var returnInstruction = instructions!.Last(i => i.OpCode == OpCodes.Ret);
        ilProcessor.InsertBefore(returnInstruction, ilProcessor.Create(OpCodes.Ldarg_0)!);
        ilProcessor.InsertBefore(returnInstruction, ilProcessor.Create(OpCodes.Call, patcherMethod)!);
    }

    private static bool IsDerivedFromPluginBase(TypeDefinition typeDefinition)
    {
        var currentType = typeDefinition;
        while (currentType != null) {
            var baseType = currentType.BaseType;
            if (baseType is GenericInstanceType genericInstanceType && 
                genericInstanceType.ElementType!.FullName == typeof(PluginBase<>).FullName!) {
                return true;
            }
            currentType = currentType.BaseType?.Resolve();
        }
        return false;
    }

    private MethodDefinition CreateOnIsEnabledChangedOverride(TypeDefinition typeDefinition, ModuleDefinition module) {
        logger.Debug("OnIsEnabledChanged method not found in {TypeName}, creating override", typeDefinition.FullName);

        var baseTypeRef   = (GenericInstanceType)typeDefinition.BaseType!;
        var baseTypeDef   = baseTypeRef.Resolve()!;
        var baseMethodDef = baseTypeDef.Methods!.First(m => m.Name == "OnIsEnabledChanged" && m.IsVirtual)!;

        // Bind to constructed type (PluginBase<FirstPlugin>)
        var boundBaseMethodRef = new MethodReference(baseMethodDef.Name!, module.TypeSystem!.Void!, baseTypeRef) {
            HasThis = true
        };

        foreach (var param in baseMethodDef.Parameters!) {
            boundBaseMethodRef.Parameters!.Add(new ParameterDefinition(param.ParameterType!));
        }

        var method = new MethodDefinition(
            "OnIsEnabledChanged",
            MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig,
            module.TypeSystem!.Void!
        );

        if (!typeDefinition.IsSealed) {
            method.Overrides!.Add(module.ImportReference(baseMethodDef));
        }

        var il = method.Body!.GetILProcessor()!;
        il.Append(il.Create(OpCodes.Ldarg_0)!);
        il.Append(il.Create(OpCodes.Call, module.ImportReference(boundBaseMethodRef)!)!);
        il.Append(il.Create(OpCodes.Ret)!);

        typeDefinition.Methods!.Add(method);

        logger.Debug("Created OnIsEnabledChanged override with base call in {TypeName}", typeDefinition.FullName);
        return method;
    }
}
