using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Railroader.ModManager.Exceptions;
using Railroader.ModManager.Extensions;
using Serilog;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Railroader.ModManager.Features.CodePatchers;

internal delegate bool MethodPatcherDelegate(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition);

[PublicAPI]
internal static class MethodPatcher
{
    private sealed record MethodPatcherContext(ILogger Logger, Type MarkerType, Type TargetBaseType, string TargetMethod, MethodInfo InjectedMethod);

    [ExcludeFromCodeCoverage]
    public static MethodPatcherDelegate Factory<TMarker>(Type patcherType, Type targetBaseType, string targetMethod, string? injectorMethod = null) =>
        Factory<TMarker>(Log.Logger.ForSourceContext(), patcherType, targetBaseType, targetMethod, injectorMethod);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static MethodPatcherDelegate Factory<TMarker>(ILogger logger, Type patcherType, Type targetBaseType, string targetMethod, string? injectorMethod = null) {
        var injectedMethod = patcherType.GetMethod(injectorMethod ?? targetMethod, BindingFlags.Public | BindingFlags.Static);

        List<string> errors = new();
        if (injectedMethod == null) {
            errors.Add("Injected method must be public and static.");
        } else {
            if (injectedMethod.ReturnType != typeof(void)) {
                errors.Add("Injected method must bave void return type.");
            }

            if (!injectedMethod.DeclaringType!.IsPublic) {
                errors.Add("Injected method declaring type must be public.");
            }

            var parameters = injectedMethod.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType.IsAssignableFrom(typeof(TMarker))) {
                errors.Add($"Injected method must have single parameter assignable from {typeof(TMarker)}.");
            }
        }

        if (injectedMethod == null || errors.Count > 0) {
            throw new ValidationException("Failed to resolve injected method. See errors for details.", errors);
        }

        var context = new MethodPatcherContext(logger, typeof(TMarker), targetBaseType, targetMethod, injectedMethod);
        return (assemblyDefinition, typeDefinition) => context.Execute(assemblyDefinition, typeDefinition);
    }

    private static bool Execute(this MethodPatcherContext context, AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition) {
        var isBaseType = context.IsDerivedFromBaseType(typeDefinition);
        var hasMarker  = typeDefinition.Interfaces.Any(i => i.InterfaceType?.FullName == context.MarkerType.FullName);
        if (!isBaseType || !hasMarker) {
            context.Logger.Debug("Skipping patching for type {TypeName}: not derived from {BaseType} or does not implement {MarkerInterface}", typeDefinition.FullName, context.TargetBaseType, context.MarkerType);
            return false;
        }

        var module = assemblyDefinition.MainModule;
        var method = typeDefinition.Methods.FirstOrDefault(m => m.Name == context.TargetMethod) ?? context.CreateMethodOverride(typeDefinition, module);
        if (method == null) {
            return false;
        }

        // Import the patcher method
        var injectedMethodReference = module.ImportReference(context.InjectedMethod);

        // Inject patcher call (for BOTH existing AND created methods)
        var ilProcessor  = method.Body.GetILProcessor();
        var instructions = method.Body.Instructions;
        var hasPatcherCall = instructions.Any(i => i.OpCode == OpCodes.Call &&
                                                   i.Operand is MethodReference mr &&
                                                   mr.FullName == injectedMethodReference.FullName);

        if (hasPatcherCall) {
            context.Logger.Information("Skipping patch of {TypeName} as it already contain code for {PluginInterface}", typeDefinition.FullName, context.MarkerType);
            return false;
        }

        // Insert at BEGINNING (before FIRST instruction)
        var firstInstruction = instructions[0]!;
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldarg_0));
        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, injectedMethodReference));

        context.Logger.Information("Successfully patched {TypeName} for {PluginInterface}", typeDefinition.FullName, context.MarkerType);
        return true;
    }

    private static bool IsDerivedFromBaseType(this MethodPatcherContext context, TypeDefinition typeDefinition) {
        Func<TypeReference, string?> getFullName =
            context.TargetBaseType.IsGenericTypeDefinition
                ? reference => (reference as GenericInstanceType)?.ElementType?.FullName
                : reference => reference?.FullName;

        var currentType = typeDefinition;
        while (currentType != null) {
            var baseType = currentType.BaseType;

            var fullName = getFullName(baseType);
            if (fullName == context.TargetBaseType.FullName) {
                return true;
            }

            currentType = baseType?.Resolve();
        }

        return false;
    }

    private static MethodDefinition? CreateMethodOverride(this MethodPatcherContext context, TypeDefinition typeDefinition, ModuleDefinition module) {
        context.Logger.Debug("{MethodName} method not found in {TypeName}, creating override", context.TargetMethod, typeDefinition.FullName);

        var baseMethodDef = context.FindVirtualBaseMethod(typeDefinition.BaseType);
        if (baseMethodDef == null) {
            context.Logger.Error("Virtual method '{MethodName}' not found in {TypeName} hierarchy!", context.TargetMethod, typeDefinition.FullName);
            return null;
        }

        var baseMethodRef = module.ImportReference(baseMethodDef);

        var methodAttributes = baseMethodDef.Attributes & ~(MethodAttributes.Final | MethodAttributes.NewSlot) | MethodAttributes.HideBySig;

        var method = new MethodDefinition(context.TargetMethod, methodAttributes, module.ImportReference(baseMethodDef.ReturnType));

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
                _ => il.Create(OpCodes.Ldarg_S, method.Parameters[i]!)
            };
            il.Append(ilCode);
        }


        il.Append(il.Create(OpCodes.Call, baseMethodRef));
        il.Append(il.Create(OpCodes.Ret));

        typeDefinition.Methods.Add(method);
        context.Logger.Debug("Created {MethodName} override with base call in {TypeName}", context.TargetMethod, typeDefinition.FullName);
        return method;
    }

    private static MethodDefinition? FindVirtualBaseMethod(this MethodPatcherContext context, TypeReference? baseTypeRef) {
        var current = baseTypeRef?.Resolve();
        while (current != null) {
            var method = current.Methods.FirstOrDefault(m => m.Name == context.TargetMethod && m.IsVirtual);
            if (method != null) {
                return method;
            }

            current = current.BaseType?.Resolve();
        }

        return null;
    }
}
