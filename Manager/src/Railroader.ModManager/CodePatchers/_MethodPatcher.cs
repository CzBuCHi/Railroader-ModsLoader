using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Railroader.ModManager.Services;
using Serilog;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Railroader.ModManager.CodePatchers;

public interface IMethodPatcher
{
    bool Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition);
}

public sealed class MethodPatcher<TMarker, TPluginPatcher> : IMethodPatcher
    where TMarker : class
{
    private readonly Type       _TargetBaseType;
    private readonly ILogger    _Logger;
    private readonly string     _TargetMethod;
    private readonly MethodInfo _InjectedMethod;

    public MethodPatcher(ILoggerFactory loggerFactory, Type targetBaseType, string targetMethod, string? injectorMethod = null) {
        _TargetBaseType = targetBaseType;
        _Logger = loggerFactory.GetLogger();
        _TargetMethod = targetMethod;

        var injectedMethod = typeof(TPluginPatcher).GetMethod(injectorMethod ?? targetMethod, BindingFlags.Public | BindingFlags.Static);
        if (injectedMethod == null || injectedMethod.ReturnType != typeof(void) || !injectedMethod.DeclaringType!.IsPublic) {
            throw new ArgumentException("Injected method must be public static void method on public class.", nameof(injectedMethod));
        }

        _InjectedMethod = injectedMethod;
    }

    public bool Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition) {
        var isBaseType = IsDerivedFromBaseType(typeDefinition);
        var hasMarker  = typeDefinition.Interfaces.Any(i => i.InterfaceType?.FullName == typeof(TMarker).FullName);
        if (!isBaseType || !hasMarker) {
            _Logger.Debug("Skipping patching for type {TypeName}: not derived from {BaseType} or does not implement {MarkerInterface}", typeDefinition.FullName, _TargetBaseType, typeof(TMarker));
            return false;
        }

        var module = assemblyDefinition.MainModule;
        var method = GetOrCreateOverride(typeDefinition, module);
        if (method == null) {
            return false;
        }

        // Import the patcher method
        var injectedMethodReference = module.ImportReference(_InjectedMethod);

        // Inject patcher call (for BOTH existing AND created methods)
        var ilProcessor  = method.Body.GetILProcessor();
        var instructions = method.Body.Instructions;
        var hasPatcherCall = instructions.Any(i => i.OpCode == OpCodes.Call &&
                                                    i.Operand is MethodReference mr &&
                                                    mr.FullName == injectedMethodReference.FullName);

        if (hasPatcherCall) {
            _Logger.Information("Skipping patch of {TypeName} as it already contain code for {PluginInterface}", typeDefinition.FullName, typeof(TMarker).FullName);
            return false;
        }

        // Insert at BEGINNING (before FIRST instruction)
        var firstInstruction = instructions[0]!;
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldarg_0));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, injectedMethodReference));

        _Logger.Information("Successfully patched {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(TMarker).FullName);
        return true;
    }

    private MethodDefinition? GetOrCreateOverride(TypeDefinition typeDefinition, ModuleDefinition module) =>
        typeDefinition.Methods.FirstOrDefault(m => m.Name == _TargetMethod) ?? CreateMethodOverride(typeDefinition, module);

    private bool IsDerivedFromBaseType(TypeDefinition typeDefinition) {
        Func<TypeReference, string?> getFullName =
            _TargetBaseType.IsGenericTypeDefinition
                ? reference => (reference as GenericInstanceType)?.ElementType?.FullName
                : reference => reference?.FullName;

        var currentType = typeDefinition;
        while (currentType != null) {
            var baseType = currentType.BaseType;

            var fullName = getFullName(baseType);
            if (fullName == _TargetBaseType.FullName) {
                return true;
            }

            currentType = baseType?.Resolve();
        }

        return false;
    }

    private MethodDefinition? CreateMethodOverride(TypeDefinition typeDefinition, ModuleDefinition module) {
        _Logger.Debug("{MethodName} method not found in {TypeName}, creating override", _TargetMethod, typeDefinition.FullName);

        var baseMethodDef = FindVirtualBaseMethod(typeDefinition.BaseType);
        if (baseMethodDef == null) {
            _Logger.Error("Virtual method '{MethodName}' not found in {TypeName} hierarchy!", _TargetMethod, typeDefinition.FullName);
            return null;
        }

        var baseMethodRef = module.ImportReference(baseMethodDef);

        var methodAttributes = (baseMethodDef.Attributes & ~(MethodAttributes.Final | MethodAttributes.NewSlot)) | MethodAttributes.HideBySig;

        var method = new MethodDefinition(_TargetMethod, methodAttributes, module.ImportReference(baseMethodDef.ReturnType));

        foreach (var param in baseMethodDef.Parameters) {
            method.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, module.ImportReference(param.ParameterType)));
        }
        
        var il = method.Body.GetILProcessor();
        il.Append(il.Create(OpCodes.Ldarg_0));

        for (var i = 0; i < baseMethodDef.Parameters.Count; i++) {

            var ilCode = i switch {
                0 => il.Create(OpCodes.Ldarg_1),
                1 => il.Create(OpCodes.Ldarg_2),
                2 => il.Create(OpCodes.Ldarg_3),
                _ => il.Create(OpCodes.Ldarg_S, method.Parameters[i]!),
            };
            il.Append(ilCode);
        }


        il.Append(il.Create(OpCodes.Call, baseMethodRef));
        il.Append(il.Create(OpCodes.Ret));
        
        typeDefinition.Methods.Add(method);
        _Logger.Debug("Created {MethodName} override with base call in {TypeName}", _TargetMethod, typeDefinition.FullName);
        return method;
    }

    private MethodDefinition? FindVirtualBaseMethod(TypeReference? baseTypeRef) {
        var current = baseTypeRef?.Resolve();
        while (current != null) {
            var method = current.Methods.FirstOrDefault(m => m.Name == _TargetMethod && m.IsVirtual);
            if (method != null) {
                return method;
            }

            current = current.BaseType?.Resolve();
        }

        return null;
    }
}
