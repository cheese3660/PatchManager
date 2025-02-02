﻿using BepInEx.Logging;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace PatchManager.PreloadPatcher;

public static class Patcher
{
    internal static Func<string, Action<object>, Action<IList<object>>, bool> LoadAssetsDelegate { get; set; }

    [UsedImplicitly]
    public static IEnumerable<string> TargetDLLs { get; } = new[]
    {
        "Assembly-CSharp.dll"
    };

    [UsedImplicitly]
    public static void Patch(ref AssemblyDefinition assemblyDefinition)
    {
        var targetType = assemblyDefinition.MainModule.Types.Single(t => t.Name == "AssetProvider");
        var targetMethod = targetType.Methods.Single(m => m.Name == "LoadByLabel" && m.HasGenericParameters);

        var currentAssembly = AssemblyDefinition.ReadAssembly(typeof(Patcher).Assembly.Location);

        var loadDelegateInstruction = Instruction.Create(
            OpCodes.Ldsfld,
            currentAssembly.MainModule.ImportReference(typeof(Patcher).GetField("LoadAssetsDelegate"))
        );
        var loadPatchedAssetsInstruction = Instruction.Create(OpCodes.Ldarg_0);
        var loadCallbackInstruction = Instruction.Create(OpCodes.Ldarg_1);
        var loadResultCallbackInstruction = Instruction.Create(OpCodes.Ldarg_2);
        var callDelegateInstruction = Instruction.Create(
            OpCodes.Callvirt,
            currentAssembly.MainModule.ImportReference(
                typeof(Func<string, Action<object>, Action<IList<object>>, bool>
                ).GetMethod("Invoke")
            )
        );
        var notInstruction = Instruction.Create(OpCodes.Ldc_I4_0);
        var ifInstruction = Instruction.Create(OpCodes.Beq_S, targetMethod.Body.Instructions.First());
        var returnInstruction = Instruction.Create(OpCodes.Ret);

        targetMethod.Body.Instructions.Insert(0, loadDelegateInstruction);
        targetMethod.Body.Instructions.Insert(1, loadPatchedAssetsInstruction);
        targetMethod.Body.Instructions.Insert(2, loadCallbackInstruction);
        targetMethod.Body.Instructions.Insert(3, loadResultCallbackInstruction);
        targetMethod.Body.Instructions.Insert(4, callDelegateInstruction);
        targetMethod.Body.Instructions.Insert(5, notInstruction);
        targetMethod.Body.Instructions.Insert(6, ifInstruction);
        targetMethod.Body.Instructions.Insert(7, returnInstruction);

        targetMethod.Body.MaxStackSize += 1;


        Logger.CreateLogSource("Patch Manager Patcher").LogInfo("Prepatching complete!");
    }
}