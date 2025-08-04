using HarmonyLib;

namespace MoreAndQuickLoadouts;

[HarmonyPatch(typeof(GearDetailsWindow), MethodType.Normal)]
[HarmonyPatch("Awake")]

public class GearDetailsWindow_Awake_Patch
{
    [HarmonyPrefix]
    public static bool PreAwake(GearDetailsWindow __instance)
    {
        // BasePlugin.Logger.LogInfo("GearDetailsWindow_Awake_Patch");
        var injector = __instance.gameObject.AddComponent<LoadoutInjector>();
        injector.target = __instance;
        injector.targetCount = BasePlugin.LoadoutSize;
        injector.InjectLoadouts();
        
        return true;
    }
}