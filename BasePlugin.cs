using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace MoreAndQuickLoadouts;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[MycoMod(null, ModFlags.IsClientSide)]
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
        NotAnotherTentacleShit.InitializeInputActions();

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        
        _harmony.PatchAll();
        
        Logger.LogInfo("quick_loadout loaded and harmony patch applied.");
    }

    private void OnDestroy()
    {
        // InputManager.Cleanup();
        NotAnotherTentacleShit.CleanupInputActions();
        _harmony.UnpatchSelf();
    }
}