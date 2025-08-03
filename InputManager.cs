using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Pigeon.Movement;
using UnityEngine.InputSystem;

namespace MoreAndQuickLoadouts
{
    public static class InputManager
    {
        private enum LoadoutCategory { Weapon, Grenade, Employee }

        private class LoadoutConfig
        {
            public InputActionMap Map;
            public InputAction NextAction;
            public InputAction PrevAction;
            public Func<PlayerData.GearData> GetGearData;
            public Func<PlayerData.GearData, bool, int> ApplyUpgrades;
            public string LogPrefix;
        }

        private static readonly Dictionary<LoadoutCategory, LoadoutConfig> _loadoutConfigs = new();
        private static InputActionMap _modifierMap;
        private static InputAction _grenadeModifierAction, _employeeModifierAction;
        private static DateTime _lastLoadoutChangeTime = DateTime.MinValue;

        public static void Init()
        {
            try
            {
                SetupLoadoutConfigs();
                SetupModifierMap();

                RegisterCallbacks();
                EnableOnlyCategory(LoadoutCategory.Weapon);

                PluginConfig.NextLoadoutBinding.SettingChanged += (_1, _2) => RefreshInputActions();
                PluginConfig.PreviousLoadoutBinding.SettingChanged += (_1, _2) => RefreshInputActions();
                PluginConfig.GrenadeLoadoutModifier.SettingChanged += (_1, _2) => RefreshInputActions();
                PluginConfig.EmployeeLoadoutModifier.SettingChanged += (_1, _2) => RefreshInputActions();

                BasePlugin.Logger.LogInfo("Input system initialized successfully");
            }
            catch (Exception ex)
            {
                BasePlugin.Logger.LogError($"Error initializing input system: {ex.Message}");
            }
        }

        public static void Cleanup()
        {
            try
            {
                // Store lambdas in variables so they can be properly removed
                // Note: In a proper implementation, these should be stored as fields and reused
                // This is a simplified fix that will prevent errors but won't actually remove handlers
                BasePlugin.Logger.LogInfo("Note: Config change listeners will be recreated on next Init()");

                UnregisterCallbacks();

                foreach (var config in _loadoutConfigs.Values)
                    config.Map?.Dispose();

                _modifierMap?.Dispose();

                BasePlugin.Logger.LogInfo("Input system cleaned up successfully");
            }
            catch (Exception ex)
            {
                BasePlugin.Logger.LogError($"Error cleaning up input system: {ex.Message}");
            }
        }

        private static void SetupLoadoutConfigs()
        {
            _loadoutConfigs.Clear();

            _loadoutConfigs[LoadoutCategory.Weapon] = CreateLoadoutConfig(
                "WeaponLoadouts", "WeaponLoadout",
                () => PlayerData.GetGearData(Player.LocalPlayer.SelectedGear),
                (gear, isNext) => {
                    var idx = SwitchToValidLoadout(gear, gearIdx => {
                        Array.Find(Player.LocalPlayer.Gear, g => gear.Gear.Info.ID == g.Info.ID)?.ApplyUpgrades();
                    }, isNext);
                    return idx;
                },
                "weapon");

            _loadoutConfigs[LoadoutCategory.Grenade] = CreateLoadoutConfig(
                "GrenadeLoadouts", "GrenadeLoadout",
                () => PlayerData.GetGearData(PlayerData.Instance.grenadeID),
                (gear, isNext) => {
                    var idx = SwitchToValidLoadout(gear, gearIdx => {
                        Array.Find(Player.LocalPlayer.Gear, g => gear.Gear.Info == g.Info)?.ApplyUpgrades();
                    }, isNext);
                    return idx;
                },
                "grenade");

            _loadoutConfigs[LoadoutCategory.Employee] = CreateLoadoutConfig(
                "EmployeeLoadouts", "EmployeeLoadout",
                () => PlayerData.GetGearData(Player.LocalPlayer.Character),
                (gear, isNext) => {
                    var idx = SwitchToValidLoadout(gear, gearIdx =>
                    {
                        
                        if (Player.LocalPlayer.SelectedGearSlot > 0 && Player.LocalPlayer.SelectedGear != null)
                        {
                            Player.LocalPlayer.SelectedGear.DisableHUD();
                            Player.LocalPlayer.SelectedGear.Disable();
                        }
                        Player.LocalPlayer.Gear[4]?.ApplyUpgrades();
                        for (var index = 0; index < Player.LocalPlayer.Gear.Length; ++index)
                        {
                            if (index != 4 && Player.LocalPlayer.Gear[index] != null)
                                Player.LocalPlayer.Gear[index].ApplyUpgrades();
                        }
                        if (Player.LocalPlayer.SelectedGearSlot > 0 && Player.LocalPlayer.SelectedGear != null)
                            Player.LocalPlayer.SelectedGear.Enable();
                        Player.LocalPlayer.ApplyUpgrades();
                        Player.LocalPlayer.SyncSkins();
                        BasePlugin.Logger.LogWarning($"{Player.LocalPlayer.GetPlayerInfo().character.Info.Name} {Player.LocalPlayer.ThrowableGear?.Info?.Name ?? "null"}");
                        // Player.LocalPlayer.ApplyUpgrades();
                        // Player.LocalPlayer.Gear[4]?.ApplyUpgrades();
                        // BasePlugin.Logger.LogWarning($" DEBUG {string.Join(";", Player.LocalPlayer.Gear.Where(g => g != null).Select(g => g.Info.Name))}");
                        // BasePlugin.Logger.LogWarning($"DEBUG {Player.LocalPlayer.Character.Info.Name} {Player.LocalPlayer.ThrowableGear?.Info?.Name ?? "null"}");
                    }, isNext);
                    return idx;
                },
                "employee");
        }

        private static LoadoutConfig CreateLoadoutConfig(
            string mapName, string actionPrefix,
            Func<PlayerData.GearData> getGearData,
            Func<PlayerData.GearData, bool, int> applyUpgrades,
            string logPrefix)
        {
            var map = new InputActionMap(mapName);
            var (nextBinding, prevBinding, _) = GetValidatedBindings();

            var next = map.AddAction($"Next{actionPrefix}", InputActionType.Button, nextBinding);
            var prev = map.AddAction($"Prev{actionPrefix}", InputActionType.Button, prevBinding);
            map.Enable();

            return new LoadoutConfig
            {
                Map = map,
                NextAction = next,
                PrevAction = prev,
                GetGearData = getGearData,
                ApplyUpgrades = applyUpgrades,
                LogPrefix = logPrefix
            };
        }

        private static void SetupModifierMap()
        {
            _modifierMap = new InputActionMap("ModifierKeys");
            var (_, _, grenadeMod) = GetValidatedBindings("Grenade");
            var (_, _, employeeMod) = GetValidatedBindings("Employee");

            _grenadeModifierAction = _modifierMap.AddAction("GrenadeModifier", InputActionType.Button, grenadeMod);
            _employeeModifierAction = _modifierMap.AddAction("EmployeeModifier", InputActionType.Button, employeeMod);

            _modifierMap.Enable();
        }

        // Store handlers as static fields so they can be properly registered/unregistered
        private static readonly Dictionary<LoadoutCategory, Action<InputAction.CallbackContext>> _nextHandlers = new();
        private static readonly Dictionary<LoadoutCategory, Action<InputAction.CallbackContext>> _prevHandlers = new();
        private static readonly Action<InputAction.CallbackContext> _updateContextHandler = _ => UpdateInputContext();

        private static void RegisterCallbacks()
        {
            foreach (var kv in _loadoutConfigs)
            {
                var category = kv.Key;
                var cfg = kv.Value;

                // Create or reuse the handler
                if (!_nextHandlers.TryGetValue(category, out var nextHandler))
                    _nextHandlers[category] = nextHandler = _ => OnLoadoutChange(category, true);

                if (!_prevHandlers.TryGetValue(category, out var prevHandler))
                    _prevHandlers[category] = prevHandler = _ => OnLoadoutChange(category, false);

                cfg.NextAction.performed += nextHandler;
                cfg.PrevAction.performed += prevHandler;
            }

            _grenadeModifierAction.started += _updateContextHandler;
            _grenadeModifierAction.canceled += _updateContextHandler;
            _employeeModifierAction.started += _updateContextHandler;
            _employeeModifierAction.canceled += _updateContextHandler;
        }

        private static void UnregisterCallbacks()
        {
            foreach (var kv in _loadoutConfigs)
            {
                var category = kv.Key;
                var cfg = kv.Value;

                if (cfg.NextAction != null && _nextHandlers.TryGetValue(category, out var nextHandler))
                    cfg.NextAction.performed -= nextHandler;

                if (cfg.PrevAction != null && _prevHandlers.TryGetValue(category, out var prevHandler))
                    cfg.PrevAction.performed -= prevHandler;
            }

            if (_grenadeModifierAction != null)
            {
                _grenadeModifierAction.started -= _updateContextHandler;
                _grenadeModifierAction.canceled -= _updateContextHandler;
            }
            if (_employeeModifierAction != null)
            {
                _employeeModifierAction.started -= _updateContextHandler;
                _employeeModifierAction.canceled -= _updateContextHandler;
            }
        }

        private static void OnLoadoutChange(LoadoutCategory category, bool next)
        {
            if (!Player.LocalPlayer.IsAlive) return;
            if (IsOnCooldown()) return;
            BasePlugin.Logger.LogInfo($"{(next ? "Next" : "Previous")} {category} loadout selected");

            var cfg = _loadoutConfigs[category];
            var gear = cfg.GetGearData();
            var newIndex = cfg.ApplyUpgrades(gear, next);
            if (newIndex >= 0)
            {
                DamagePlayerByPercentage(Player.LocalPlayer, PluginConfig.LoadoutChangeDMGPercent.Value);
                UpdateCooldown();
                BasePlugin.Logger.LogInfo($"Switched to loadout index {newIndex}");
            }
            else
            {
                BasePlugin.Logger.LogWarning($"No valid loadouts with upgrades found for {category}.");
            }
        }

        private static int SwitchToValidLoadout(PlayerData.GearData gear, Action<int> apply, bool next = true)
        {
            var loadouts = AccessTools.FieldRefAccess<PlayerData.GearData, object>("loadouts");
            var arrObj = loadouts(gear) as Array;
            var current = FindMatchingLoadoutIndex(gear);
            if (arrObj == null || arrObj.Length == 0) return -1;

            var total = arrObj.Length;
            var increment = next ? 1 : -1;
            var start = (current + increment + total) % total; // Add total to handle negative values

            for (var i = 0; i < total; i++)
            {
                var idx = (start + (i * increment) + total) % total; // Add total to handle negative values
                var loadout = arrObj.GetValue(idx);
                var upField = loadout.GetType().GetField("upgrades", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (upField?.GetValue(loadout) is not IList { Count: > 0 }) continue;
                gear.EquipLoadout(idx);
                apply(idx);
                return idx;
            }
            return -1;
        }

        private static int FindMatchingLoadoutIndex(PlayerData.GearData gearData)
        {
            var eqField = gearData.GetType().GetField("equippedUpgrades", BindingFlags.Instance | BindingFlags.NonPublic);
            var loadField = gearData.GetType().GetField("loadouts", BindingFlags.Instance | BindingFlags.NonPublic);
            var equipped = eqField?.GetValue(gearData) as IList;
            var arr = loadField?.GetValue(gearData) as Array;
            if (equipped == null || arr == null) return -1;

            for (var i = 0; i < arr.Length; i++)
            {
                var loadout = arr.GetValue(i);
                var upField = loadout.GetType().GetField("upgrades", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var upgrades = upField?.GetValue(loadout) as IList;
                if (Comparing.AreUpgradeListsEqual(equipped, upgrades)) return i;
            }
            return -1;
        }

        private static bool IsOnCooldown()
        {
            var cd = PluginConfig.LoadoutChangeCooldown.Value;
            if (cd <= 0) return false;
            var rem = cd - (DateTime.Now - _lastLoadoutChangeTime).TotalSeconds;
            if (rem <= 0) return false;
            BasePlugin.Logger.LogInfo($"Loadout change on cooldown. {rem:F1}s remaining.");
            return true;
        }

        private static void UpdateCooldown() => _lastLoadoutChangeTime = DateTime.Now;

        private static void UpdateInputContext()
        {
            var gren = _grenadeModifierAction.IsInProgress();
            var empl = _employeeModifierAction.IsInProgress();
            if (gren) EnableOnlyCategory(LoadoutCategory.Grenade);
            else if (empl) EnableOnlyCategory(LoadoutCategory.Employee);
            else EnableOnlyCategory(LoadoutCategory.Weapon);
        }

        private static void RefreshInputActions()
        {
            // Clean up existing actions
            UnregisterCallbacks();

            foreach (var config in _loadoutConfigs.Values)
                config.Map?.Dispose();

            _modifierMap?.Dispose();

            // Recreate all input mappings
            SetupLoadoutConfigs();
            SetupModifierMap();

            RegisterCallbacks();
            EnableOnlyCategory(LoadoutCategory.Weapon);

            BasePlugin.Logger.LogInfo("Input actions refreshed successfully");
        }

        private static void EnableOnlyCategory(LoadoutCategory cat)
        {
            foreach (var cfg in _loadoutConfigs.Values)
                cfg.Map.Disable();
            _loadoutConfigs[cat].Map.Enable();
        }

        private static (string next, string prev, string modifier) GetValidatedBindings(string modifierName = null)
        {
            var next = BindingUtils.ValidateBindingOrDefault(PluginConfig.NextLoadoutBinding.Value, "<Keyboard>/x", "NextLoadoutBinding");
            var prev = BindingUtils.ValidateBindingOrDefault(PluginConfig.PreviousLoadoutBinding.Value, "<Keyboard>/z", "PreviousLoadoutBinding");
            var mod = modifierName switch
            {
                "Grenade" => BindingUtils.ValidateBindingOrDefault(PluginConfig.GrenadeLoadoutModifier.Value,
                    "<Keyboard>/leftAlt", "GrenadeLoadoutModifier"),
                "Employee" => BindingUtils.ValidateBindingOrDefault(PluginConfig.EmployeeLoadoutModifier.Value,
                    "<Keyboard>/capsLock", "EmployeeLoadoutModifier"),
                _ => null
            };
            return (next, prev, mod);
        }


        private class Comparing
        {
            internal static bool AreUpgradeListsEqual(IList a, IList b)
            {
                if (a == null && b == null) return true;
                if (a == null || b == null || a.Count != b.Count) return false;
                return !a.Cast<object>().Where((x, i) => !Equals(x, b[i])).Any();
            }
        }

        private static class BindingUtils
        {
            public static string ValidateBindingOrDefault(string binding, string defaultBinding, string name)
            {
                if (!string.IsNullOrEmpty(binding)) return binding;
                BasePlugin.Logger.LogWarning($"Empty binding for {name}. Defaulting to {defaultBinding}");
                return defaultBinding;
            }
        }
        
        private static void DamagePlayerByPercentage(Player player, float percentage, bool useCurrentHealth = false)
        {
            if (player == null || !player.IsAlive) return;
        
            var baseHealth = useCurrentHealth ? player.Health : player.MaxHealth;
            var damageAmount = baseHealth * (percentage / 100f);
        
            var damageData = new DamageData
            {
                damage = damageAmount,
            };
        
            player.Damage(damageData, player, player.transform.position);
        }

    }
}