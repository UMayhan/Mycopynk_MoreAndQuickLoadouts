using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace MoreAndQuickLoadouts;

[HarmonyPatch(typeof(PlayerData.GearData), nameof(PlayerData.GearData.SaveLoadout))]
public class PlayerData_SaveLoadout_Patch
{
    private static int CustomMaxLoadoutSize => BasePlugin.LoadoutSize;
    
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_I4_3)
            {
                yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(PlayerData_SaveLoadout_Patch), nameof(CustomMaxLoadoutSize)));
            }
            else
            {
                yield return instruction;
            }
        }
    }
}

[HarmonyPatch(typeof(PlayerData.GearData), nameof(PlayerData.GearData.IncrementLoadoutIcon))]
public class PlayerData_IncrementLoadoutIcon_Patch
{
    private static int CustomMaxLoadoutSize => BasePlugin.LoadoutSize;
    
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_I4_3)
            {
                yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(PlayerData_SaveLoadout_Patch), nameof(CustomMaxLoadoutSize)));
            }
            else
            {
                yield return instruction;
            }
        }
    }
}