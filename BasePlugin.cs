using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace MoreAndQuickLoadouts;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class BasePlugin : BaseUnityPlugin
{
    private Harmony _harmony;
    
    internal new static ManualLogSource Logger;
    public static int LoadoutSize => PluginConfig.LoadoutSize.Value;

    private void Awake()
    {
        Logger = base.Logger;
        
        PluginConfig.Init(Config);
        // InputManager.Init();
        Highlighter_Patch.InitializeInputActions();

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        
        _harmony.PatchAll();
        
        Logger.LogInfo("quick_loadout loaded and harmony patch applied.");
    }

    private void OnDestroy()
    {
        // InputManager.Cleanup();
        Highlighter_Patch.CleanupInputActions();
        _harmony.UnpatchSelf();
    }
}