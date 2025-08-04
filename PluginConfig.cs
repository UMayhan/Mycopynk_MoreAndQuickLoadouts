using BepInEx.Configuration;

namespace MoreAndQuickLoadouts;
//TODO:change Bindings to better way of managing keys instead of strings might be tricky for some people to configurate
public static class PluginConfig
{
    public static ConfigEntry<int> LoadoutSize;
    public static ConfigEntry<int> LoadoutChangeCooldown;
    public static ConfigEntry<float> LoadoutChangeDMGPercent;
    public static ConfigEntry<string> WeaponLoadoutBinding;
    public static ConfigEntry<string> GrenadeLoadoutBinding;
    public static ConfigEntry<string> EmployeeLoadoutBinding;

    public static void Init(ConfigFile config)
    {
        LoadoutSize = config.Bind("General", "LoadoutSize", 5, 
            new ConfigDescription("The number of total available loadouts. (including default ones)", new AcceptableValueRange<int>(3, 10)));
        LoadoutChangeCooldown = config.Bind("General", "LoadoutChangeCooldown", 45,
            new ConfigDescription("Cooldown for loadout change", new AcceptableValueRange<int>(0, 300))); 
        LoadoutChangeDMGPercent = config.Bind("General", "LoadoutChangeDMGPercent", 50f,
            new ConfigDescription("Cooldown for loadout change", new AcceptableValueRange<float>(0f, 100f)));

        const string bindingString = $"Use Unity Input System control path format. See: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.5/manual/ActionBindings.html#binding-syntax\"";
        
        WeaponLoadoutBinding = config.Bind("Bindings", "WeaponLoadoutBinding", "<Keyboard>/h", $"Toggle weapon loadout wheel.\n {bindingString}");
        GrenadeLoadoutBinding = config.Bind("Bindings", "GrenadeLoadoutBinding", "<Keyboard>/j", $"Toggle grenade loadout wheel. \n{bindingString}.");
        EmployeeLoadoutBinding = config.Bind("Bindings", "EmployeeLoadoutBinding", "<Keyboard>/k", $"Toggle employee loadout wheel. \n{bindingString}");
    }
}