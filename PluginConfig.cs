using BepInEx.Configuration;

namespace MoreAndQuickLoadouts;
//TODO:change Bindings to better way of managing keys instead of strings might be tricky for some people to configurate
public static class PluginConfig
{
    public static ConfigEntry<int> LoadoutSize;
    public static ConfigEntry<int> LoadoutChangeCooldown;
    public static ConfigEntry<float> LoadoutChangeDMGPercent;
    public static ConfigEntry<string> NextLoadoutBinding;
    public static ConfigEntry<string> PreviousLoadoutBinding;
    public static ConfigEntry<string> GrenadeLoadoutModifier;
    public static ConfigEntry<string> EmployeeLoadoutModifier;

    public static void Init(ConfigFile config)
    {
        LoadoutSize = config.Bind("General", "LoadoutSize", 5, 
            new ConfigDescription("The number of total available loadouts. (including default ones)", new AcceptableValueRange<int>(3, 10)));
        LoadoutChangeCooldown = config.Bind("General", "LoadoutChangeCooldown", 45,
            new ConfigDescription("Cooldown for loadout change", new AcceptableValueRange<int>(0, 300))); 
        LoadoutChangeDMGPercent = config.Bind("General", "LoadoutChangeDMGPercent", 50f,
            new ConfigDescription("Cooldown for loadout change", new AcceptableValueRange<float>(0f, 100f)));
        
        NextLoadoutBinding = config.Bind("Bindings", "NextLoadoutBinding", "<Keyboard>/x", "The key to use to switch to the next loadout of your current holding weapon.");
        PreviousLoadoutBinding = config.Bind("Bindings", "PreviousLoadoutBinding", "<Keyboard>/z", "The key to use to switch to the previous loadout of your current holding weapon.");
        GrenadeLoadoutModifier = config.Bind("Bindings", "GrenadeLoadoutModifier", "<Keyboard>/alt", "The modifier to use when switching the grenade loadout instead of weapon.");
        EmployeeLoadoutModifier = config.Bind("Bindings", "EmployeeLoadoutModifier", "<Keyboard>/capslock", "The modifier to use when switching the employee loadout instead of weapon.");
    }
}