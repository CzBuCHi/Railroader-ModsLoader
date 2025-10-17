using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Railroader.ModInterfaces;
using Serilog;

namespace Railroader.ModInjector.PluginWrappers;

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
    /// <inheritdoc />
    public void Patch(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition)
    {
        // Validate type compatibility using TypeDefinition metadata
        var isPluginBase = IsDerivedFromPluginBase(typeDefinition);
        var isTPlugin = typeDefinition.Interfaces?.Any(i => i.InterfaceType?.FullName == typeof(TPlugin).FullName) ?? false;
        if (!isPluginBase || typeDefinition.IsAbstract || !isTPlugin) {
            logger.Debug("Skipping patching for type {TypeName}: not derived from PluginBase or does not implement {PluginInterface}", typeDefinition.FullName, typeof(TPlugin).Name);
            return;
        }

        var module = assemblyDefinition.MainModule;
        if (module == null) {
            logger.Error("MainModule is null for assembly {AssemblyName}", assemblyDefinition.Name);
            return;
        }

        // Find or create the OnIsEnabledChanged method
        var method = typeDefinition.Methods?.FirstOrDefault(m => m.Name == "OnIsEnabledChanged");
        if (method == null) {
            logger.Debug("OnIsEnabledChanged method not found in {TypeName}, creating override", typeDefinition.FullName);
            method = CreateOnIsEnabledChangedOverride(typeDefinition, module);
        }

        // Import the target method reference
        var onIsEnabledChangedMethodInfo = typeof(TPluginPatcher).GetMethod(
            "OnIsEnabledChanged",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
        );
        if (onIsEnabledChangedMethodInfo == null) {
            logger.Error("Static OnIsEnabledChanged method not found in {PatcherType} for {TypeName}", typeof(TPluginPatcher).FullName, typeDefinition.FullName);
            return;
        }

        var onIsEnabledChangedMethod = module.ImportReference(onIsEnabledChangedMethodInfo);
        if (onIsEnabledChangedMethod == null) {
            logger.Error("Failed to import reference to {PatcherType}.OnIsEnabledChanged for {TypeName}", typeof(TPluginPatcher).FullName, typeDefinition.FullName);
            return;
        }

        // Get the ILProcessor for the method
        var ilProcessor = method.Body?.GetILProcessor();
        if (ilProcessor == null) {
            logger.Error("Failed to get ILProcessor for {TypeName}.OnIsEnabledChanged", typeDefinition.FullName);
            return;
        }

        // Check if the method already contains a call to the patcher's OnIsEnabledChanged
        var instructions = method.Body!.Instructions!;
        var hasPatcherCall = instructions.Any(i =>
            i.OpCode == OpCodes.Call &&
            i.Operand is MethodReference mr &&
            mr.FullName == onIsEnabledChangedMethod.FullName);

        if (hasPatcherCall) {
            logger.Debug("OnIsEnabledChanged in {TypeName} already contains call to {PatcherType}.OnIsEnabledChanged, skipping patch", typeDefinition.FullName, typeof(TPluginPatcher).FullName);
            return;
        }

        // Find the return instruction
        var returnInstruction = instructions?.LastOrDefault(i => i.OpCode == OpCodes.Ret);
        if (returnInstruction == null) {
            logger.Error("Return instruction not found in {TypeName}.OnIsEnabledChanged", typeDefinition.FullName);
            return;
        }

        // Insert instructions to call OnIsEnabledChanged(this)
        ilProcessor.InsertBefore(returnInstruction, ilProcessor.Create(OpCodes.Ldarg_0)!);
        ilProcessor.InsertBefore(returnInstruction, ilProcessor.Create(OpCodes.Call, onIsEnabledChangedMethod)!);

        logger.Information("Successfully patched OnIsEnabledChanged in {TypeName} for {PluginInterface}", typeDefinition.FullName, typeof(TPlugin).Name);
    }

    /// <summary>
    /// Checks if the type derives from <see cref="PluginBase"/> by inspecting its base type hierarchy.
    /// </summary>
    /// <param name="typeDefinition">The type to check. Must not be null.</param>
    /// <returns>True if the type derives from <see cref="PluginBase"/>, false otherwise.</returns>
    private bool IsDerivedFromPluginBase(TypeDefinition typeDefinition)
    {
        var currentType = typeDefinition;
        while (currentType != null)
        {
            if (currentType.FullName == typeof(PluginBase).FullName)
            {
                return true;
            }
            currentType = currentType.BaseType?.Resolve();
        }
        return false;
    }

    /// <summary>
    /// Creates an override for the <c>OnIsEnabledChanged</c> method that calls the base implementation.
    /// </summary>
    /// <param name="typeDefinition">The type to add the method to. Must not be null.</param>
    /// <param name="module">The module containing the type. Must not be null.</param>
    /// <returns>The created <see cref="MethodDefinition"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the base <c>OnIsEnabledChanged</c> method is not found in <see cref="PluginBase"/> or if the module's TypeSystem is null.</exception>
    private MethodDefinition CreateOnIsEnabledChangedOverride(TypeDefinition typeDefinition, ModuleDefinition module)
    {
        if (module == null)
        {
            logger.Error("Module is null for type {TypeName}", typeDefinition.FullName);
            throw new ArgumentNullException(nameof(module));
        }

        // Find the base class method to override
        var baseType = module.ImportReference(typeof(PluginBase));
        var baseMethod = baseType?.Resolve()?.Methods!.FirstOrDefault(m => m.Name == "OnIsEnabledChanged" && m.IsVirtual);
        if (baseMethod == null)
        {
            logger.Error("Base OnIsEnabledChanged method not found in PluginBase");
            throw new InvalidOperationException("Base OnIsEnabledChanged method not found in PluginBase");
        }

        // Create a new method
        var method = new MethodDefinition(
            "OnIsEnabledChanged",
            MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.ReuseSlot | MethodAttributes.HideBySig,
            module.TypeSystem?.Void ?? throw new InvalidOperationException("Module TypeSystem is null")
        );

        // Set the override
        method.Overrides!.Add(module.ImportReference(baseMethod));

        // Create the method body
        var ilProcessor = method.Body?.GetILProcessor() ?? throw new InvalidOperationException("Failed to create ILProcessor for new method");
        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0)!);                                   // Load 'this'
        ilProcessor.Append(ilProcessor.Create(OpCodes.Call, module.ImportReference(baseMethod)!)!); // Call base.OnIsEnabledChanged
        ilProcessor.Append(ilProcessor.Create(OpCodes.Nop)!);                                       // Match debug build behavior
        ilProcessor.Append(ilProcessor.Create(OpCodes.Ret)!);                                       // Return

        // Add the method to the type
        typeDefinition.Methods!.Add(method);

        logger.Debug("Created OnIsEnabledChanged override with base call in {TypeName}", typeDefinition.FullName);
        return method;
    }
}