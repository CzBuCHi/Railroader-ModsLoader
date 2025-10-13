using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Serilog;

namespace Railroader.ModInjector.Services;

public interface IHarmonyExporter
{
    void ExportPatchedAssembly(string originalDllPath, Harmony harmony, string outputPath);
}

public class HarmonyExporter(ILogger logger) : IHarmonyExporter
{
    [ExcludeFromCodeCoverage]
    public HarmonyExporter() : this(Log.ForContext("SourceContext", "Railroader.ModInjector")){
        
    }

    public void ExportPatchedAssembly(string originalDllPath, Harmony harmony, string outputPath) {
        try {
            // Step 1: Ensure Harmony debug logging is enabled
            Harmony.DEBUG = true; // Writes to harmony.log.txt for verification
            logger.Information($"Starting export of patched assembly from {originalDllPath} to {outputPath}");

            // Step 2: Load original assembly with Mono.Cecil
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(originalDllPath)!);
            var readerParameters = new ReaderParameters { AssemblyResolver = resolver };
            var assemblyDef      = AssemblyDefinition.ReadAssembly(originalDllPath, readerParameters);
            logger.Information($"Loaded original assembly: {assemblyDef!.FullName}");

            // Step 3: Get all patched methods from Harmony
            var patchedMethods = harmony.GetPatchedMethods()!.ToList();
            logger.Information($"Found {patchedMethods.Count} patched methods");

            if (patchedMethods.Count == 0) {
                logger.Warning("No patched methods found. Output assembly will be identical to the input.");
            }

            // Step 4: Update each patched method in the Cecil assembly
            foreach (var methodInfo in patchedMethods) {
                // Find the corresponding Cecil method
                var declaringType = methodInfo.DeclaringType;
                if (declaringType == null) {
                    logger.Error($"Method {methodInfo.Name} has no declaring type");
                    continue;
                }

                var cecilType = assemblyDef.MainModule!.GetType(declaringType.FullName!.Replace("+", "/")); // Handle nested types
                if (cecilType == null) {
                    logger.Error($"Type {declaringType.FullName} not found in assembly");
                    continue;
                }

                // Use method name and signature to find the exact method
                var methodName  = methodInfo.Name;
                var cecilMethod = cecilType.Methods!.FirstOrDefault(m => IsMatchingMethod(m, methodInfo));
                if (cecilMethod == null) {
                    logger.Error($"Method {methodName} in type {cecilType.FullName} not found");
                    continue;
                }

                // Get patched IL instructions from Harmony
                var patchedInstructions = PatchProcessor.GetCurrentInstructions(methodInfo);
                if (patchedInstructions == null) {
                    logger.Error($"Failed to get patched instructions for {methodInfo}");
                    continue;
                }

                // Clear original method body
                var ilProcessor = cecilMethod.Body!.GetILProcessor()!;
                cecilMethod.Body.Instructions!.Clear();
                cecilMethod.Body.Variables!.Clear();
                cecilMethod.Body.ExceptionHandlers!.Clear();

                // Copy local variables (if any)
                var methodBody = methodInfo.GetMethodBody();
                if (methodBody?.LocalVariables != null) {
                    foreach (var local in methodBody.LocalVariables) {
                        var variableType = assemblyDef.MainModule.ImportReference(local.LocalType!);
                        cecilMethod.Body.Variables.Add(new VariableDefinition(variableType!));
                    }

                    cecilMethod.Body.InitLocals = methodBody.InitLocals;
                }

                // Copy patched instructions to Cecil
                foreach (var instruction in patchedInstructions) {
                    var cecilOpCode = ConvertOpCode(instruction.opcode);
                    if (cecilOpCode == null) {
                        logger.Warning($"Skipping instruction with unmapped opcode: {instruction.opcode}");
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Nop)!);
                        continue;
                    }

                    Instruction cecilInstruction;
                    if (instruction.operand != null) {
                        var operand = ConvertOperand(instruction.operand, assemblyDef.MainModule, methodInfo.Module);
                        cecilInstruction = CreateInstruction(ilProcessor, cecilOpCode.Value, operand)!;
                    } else {
                        cecilInstruction = ilProcessor.Create(cecilOpCode.Value)!;
                    }

                    ilProcessor.Append(cecilInstruction);
                }

                logger.Information($"Patched method {cecilMethod.FullName} in output assembly");
            }

            // Step 5: Save the modified assembly
            assemblyDef.Write(outputPath);
            logger.Information($"Patched assembly saved to {outputPath}");
        } catch (Exception ex) {
            logger.Error($"Failed to save patched assembly: {ex}");
        }
    }

    // Helper to match Cecil method with MethodBase (handles signature matching)
    private bool IsMatchingMethod(MethodDefinition cecilMethod, MethodBase methodInfo) {
        if (cecilMethod.Name != methodInfo.Name) {
            return false;
        }

        var cecilParams  = cecilMethod.Parameters;
        var methodParams = methodInfo.GetParameters();
        if (cecilParams!.Count != methodParams.Length) {
            return false;
        }

        for (var i = 0; i < cecilParams.Count; i++) {
            if (cecilParams[i]!.ParameterType!.FullName != methodParams[i].ParameterType.FullName) {
                return false;
            }
        }

        return true;
    }

    // Helper to convert System.Reflection.Emit.OpCode to Mono.Cecil.Cil.OpCode
    private OpCode? ConvertOpCode(System.Reflection.Emit.OpCode harmonyOpCode) {
        var cecilOpCodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                                          .Where(f => f.FieldType == typeof(OpCode))
                                          .Select(f => (OpCode)f.GetValue(null!))
                                          .ToDictionary(op => op.Name, op => op);

        if (cecilOpCodes.TryGetValue(harmonyOpCode.Name!, out var cecilOpCode)) {
            return cecilOpCode;
        }

        logger.Warning($"No matching Cecil opcode for {harmonyOpCode.Name}");
        return null;
    }

    // Helper to create Cecil instruction with proper operand type
    private Instruction? CreateInstruction(ILProcessor ilProcessor, OpCode cecilOpCode, object? operand) {
        if (operand == null) {
            return ilProcessor.Create(cecilOpCode);
        }

        switch (operand) {
            case string str:
                return ilProcessor.Create(cecilOpCode, str);
            case int i:
                return ilProcessor.Create(cecilOpCode, i);
            case float f:
                return ilProcessor.Create(cecilOpCode, f);
            case double d:
                return ilProcessor.Create(cecilOpCode, d);
            case byte b:
                return ilProcessor.Create(cecilOpCode, b);
            case sbyte sb:
                return ilProcessor.Create(cecilOpCode, sb);
            case long l:
                return ilProcessor.Create(cecilOpCode, l);
            case MethodReference methodRef:
                return ilProcessor.Create(cecilOpCode, methodRef);
            case FieldReference fieldRef:
                return ilProcessor.Create(cecilOpCode, fieldRef);
            case TypeReference typeRef:
                return ilProcessor.Create(cecilOpCode, typeRef);
            case ParameterDefinition param:
                return ilProcessor.Create(cecilOpCode, param);
            case VariableDefinition var:
                return ilProcessor.Create(cecilOpCode, var);
            case Instruction target:
                return ilProcessor.Create(cecilOpCode, target);
            case Instruction[] targets:
                return ilProcessor.Create(cecilOpCode, targets);
            default:
                logger.Error($"Unsupported operand type for {cecilOpCode}: {operand.GetType()}");
                return ilProcessor.Create(OpCodes.Nop);
        }
    }

    // Helper to convert operands (e.g., method references, types)
    private object? ConvertOperand(object harmonyOperand, ModuleDefinition targetModule, Module runtimeModule) {
        try {
            switch (harmonyOperand) {
                case MethodInfo methodInfo:
                    targetModule.ImportReference(runtimeModule.ResolveType(methodInfo.DeclaringType!.MetadataToken));
                    return targetModule.ImportReference(methodInfo);
                case FieldInfo fieldInfo:
                    targetModule.ImportReference(runtimeModule.ResolveType(fieldInfo.DeclaringType!.MetadataToken));
                    return targetModule.ImportReference(fieldInfo);
                case Type type:
                    return targetModule.ImportReference(type);
                case string str:
                    return str;
                case int i:
                    return i;
                case float f:
                    return f;
                case double d:
                    return d;
                case byte b:
                    return b;
                case sbyte sb:
                    return sb;
                case long l:
                    return l;
                default:
                    logger.Warning($"Unsupported operand type: {harmonyOperand.GetType()}");
                    return harmonyOperand;
            }
        } catch (Exception ex) {
            logger.Error($"Failed to convert operand {harmonyOperand}: {ex}");
            return harmonyOperand;
        }
    }
}
