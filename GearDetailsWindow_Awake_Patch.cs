using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace MoreAndQuickLoadouts;

[HarmonyPatch(typeof(GearDetailsWindow), MethodType.Normal)]

public class GearDetailsWindow_Awake_Patch
{
    
    [HarmonyPatch("Awake")]
    [HarmonyPrefix]
    public static bool PreAwake(GearDetailsWindow __instance)
    {
        var path = System.IO.Path.Combine(Paths.PluginPath, MyPluginInfo.PLUGIN_GUID, "moreicons");
        var bundle = AssetBundle.LoadFromFile(path);
        var icons = bundle.LoadAllAssets<Sprite>();
        var baseIcons = Global.Instance.LoadoutIcons;
        var combinedIcons = new Sprite[baseIcons.Length + icons.Length];
        baseIcons.CopyTo(combinedIcons, 0);
        icons.CopyTo(combinedIcons, baseIcons.Length);
        Global.Instance.LoadoutIcons = combinedIcons;
        
        var injector = __instance.gameObject.AddComponent<LoadoutInjector>();
        injector.target = __instance;
        injector.targetCount = BasePlugin.LoadoutSize;
        injector.Setup();
        return true;
    }

    [HarmonyPatch(typeof(GearDetailsWindow), nameof(GearDetailsWindow.OnClickUp))]
    [HarmonyPrefix]
    public static bool PreOnClickUp()
    {
        return !LoadoutInjector.Instance.selectIconWindow.gameObject.activeSelf;
    }
}