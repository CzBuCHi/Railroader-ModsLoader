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

public delegate bool MethodPatcherDelegate(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition);

[PublicAPI]
public static class MethodPatcher
{
    private sealed record PatcherContext(
        ILogger Logger,
        Type MarkerType,
        Type TargetBaseType,
        string TargetMethod,
        MethodInfo InjectedMethod
    );

    [ExcludeFromCodeCoverage]
    public static MethodPatcherDelegate Factory<TMarker>(
        Type patcherType,
        Type targetBaseType,
        string targetMethod,
        string? injectorMethod = null
    ) =>
        Factory<TMarker>(Log.Logger.ForSourceContext(), patcherType, targetBaseType, targetMethod, injectorMethod);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static MethodPatcherDelegate Factory<TMarker>(
        ILogger logger,
        Type patcherType,
        Type targetBaseType,
        string targetMethod,
        string? injectorMethod = null
    ) {
        var injected = patcherType.GetMethod(injectorMethod ?? targetMethod, BindingFlags.Public | BindingFlags.Static);

        var errors = ValidateInjector<TMarker>(injected).ToList();
        if (injected == null || errors.Count > 0) {
            throw new ValidationException("Failed to resolve injected method. See errors for details.", errors);
        }

        var ctx = new PatcherContext(logger, typeof(TMarker), targetBaseType, targetMethod, injected);
        return (asm, type) => ctx.Execute(asm, type);
    }

    private static IEnumerable<string> ValidateInjector<TMarker>(MethodInfo? method) {
        if (method == null) {
            yield return "Injected method must be public and static.";
            yield break;
        }

        if (method.ReturnType != typeof(void)) {
            yield return "Injected method must bave void return type.";
        }

        if (!method.DeclaringType!.IsPublic) {
            yield return "Injected method declaring type must be public.";
        }

        var pars = method.GetParameters();
        if (pars.Length != 1 || !pars[0].ParameterType.IsAssignableFrom(typeof(TMarker))) {
            yield return $"Injected method must have single parameter assignable from {typeof(TMarker)}.";
        }
    }

    private static bool Execute(this PatcherContext ctx, AssemblyDefinition asm, TypeDefinition type) {
        if (!ctx.IsApplicable(type)) {
            return false;
        }

        var module = asm.MainModule;
        var method = type.Methods.FirstOrDefault(m => m.Name == ctx.TargetMethod) ?? ctx.CreateOverride(type, module);

        if (method == null) {
            return false;
        }

        var injectedRef = module.ImportReference(ctx.InjectedMethod);
        var il          = method.Body.GetILProcessor();

        if (il.HasCallTo(injectedRef)) {
            ctx.Logger.Information("Skipping patch of {TypeName} as it already contain code for {PluginInterface}",
                type.FullName, ctx.MarkerType);
            return false;
        }

        // inject at the very start
        var first = method.Body.Instructions[0]!;
        il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
        il.InsertBefore(first, il.Create(OpCodes.Call, injectedRef));

        ctx.Logger.Information("Successfully patched {TypeName} for {PluginInterface}", type.FullName, ctx.MarkerType);

        return true;
    }

    private static bool IsApplicable(this PatcherContext ctx, TypeDefinition type) {
        var derived = ctx.IsDerivedFromBase(type);
        var marker  = type.Interfaces.Any(i => i.InterfaceType?.FullName == ctx.MarkerType.FullName);

        if (!derived || !marker) {
            ctx.Logger.Debug(
                "Skipping patching for type {TypeName}: not derived from {BaseType} or does not implement {MarkerInterface}",
                type.FullName, ctx.TargetBaseType, ctx.MarkerType);
            return false;
        }

        return true;
    }

    private static bool IsDerivedFromBase(this PatcherContext ctx, TypeDefinition type) {
        Func<TypeReference?, string?> getName = ctx.TargetBaseType.IsGenericTypeDefinition
            ? r => (r as GenericInstanceType)?.ElementType?.FullName
            : r => r?.FullName;

        var cur = type;
        while (cur != null) {
            if (getName(cur.BaseType) == ctx.TargetBaseType.FullName) {
                return true;
            }

            cur = cur.BaseType?.Resolve();
        }

        return false;
    }

    private static MethodDefinition? CreateOverride(
        this PatcherContext ctx,
        TypeDefinition type,
        ModuleDefinition module
    ) {
        ctx.Logger.Debug("{MethodName} method not found in {TypeName}, creating override", ctx.TargetMethod,
            type.FullName);

        var baseMethod = ctx.FindVirtualBaseMethod(type.BaseType);
        if (baseMethod == null) {
            ctx.Logger.Error("Virtual method '{MethodName}' not found in {TypeName} hierarchy!", ctx.TargetMethod,
                type.FullName);
            return null;
        }

        var baseRef = module.ImportReference(baseMethod);
        var attrs = (baseMethod.Attributes & ~(MethodAttributes.Final | MethodAttributes.NewSlot)) |
                    MethodAttributes.HideBySig;

        var method = new MethodDefinition(ctx.TargetMethod, attrs, module.ImportReference(baseMethod.ReturnType));

        foreach (var p in baseMethod.Parameters) {
            method.Parameters.Add(
                new ParameterDefinition(p.Name, p.Attributes, module.ImportReference(p.ParameterType)));
        }

        var il = method.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);

        for (var i = 0; i < baseMethod.Parameters.Count; i++) {
            var ilCode = i switch {
                0 => il.Create(OpCodes.Ldarg_1),
                1 => il.Create(OpCodes.Ldarg_2),
                2 => il.Create(OpCodes.Ldarg_3),
                _ => il.Create(OpCodes.Ldarg_S, method.Parameters[i]!)
            };
            il.Append(ilCode);
        }

        il.Emit(OpCodes.Call, baseRef);
        il.Emit(OpCodes.Ret);

        type.Methods.Add(method);
        ctx.Logger.Debug("Created {MethodName} override with base call in {TypeName}", ctx.TargetMethod, type.FullName);
        return method;
    }

    private static MethodDefinition? FindVirtualBaseMethod(this PatcherContext ctx, TypeReference? baseRef) {
        var cur = baseRef?.Resolve();
        while (cur != null) {
            var m = cur.Methods.FirstOrDefault(m => m.Name == ctx.TargetMethod && m.IsVirtual);
            if (m != null) {
                return m;
            }

            cur = cur.BaseType?.Resolve();
        }

        return null;
    }


    private static bool HasCallTo(this ILProcessor il, MethodReference target) =>
        il.Body!.Instructions.Any(i =>
            i.OpCode == OpCodes.Call && i.Operand is MethodReference mr && mr.FullName == target.FullName);

}
