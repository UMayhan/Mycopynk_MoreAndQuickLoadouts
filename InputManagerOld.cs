using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using Pigeon.Movement;
using UnityEngine.InputSystem;

namespace MoreAndQuickLoadouts;

public static class InputManagerOld
{
    //weapons
    public static InputAction NextWeaponLoadout;
    public static InputAction PrevWeaponLoadout;
    
    //greandes
    public static InputAction NextGrenadeLoadout;
    public static InputAction PrevGrenadeLoadout;
    
    //employee
    public static InputAction NextEmployeeLoadout;
    public static InputAction PrevEmployeeLoadout;


    public static void Init()
    {
        try
        {
            NextWeaponLoadout = new InputAction("NextWeaponLoadout", InputActionType.Button);
            PrevWeaponLoadout = new InputAction("PrevWeaponLoadout", InputActionType.Button);

            NextGrenadeLoadout = new InputAction("NextGrenadeLoadout", InputActionType.Button);
            PrevGrenadeLoadout = new InputAction("PrevGrenadeLoadout", InputActionType.Button);

            NextEmployeeLoadout = new InputAction("NextEmployeeLoadout", InputActionType.Button);
            PrevEmployeeLoadout = new InputAction("PrevEmployeeLoadout", InputActionType.Button);

            WeaponLoadouts();
            GrenadeLoadouts();
            EmployeeLoadouts();

            BasePlugin.Logger.LogInfo("Input system initialized successfully");
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error initializing input system: {ex.Message}");
        }
    }
    
    // Helper method to get all the validated bindings at once
    private static (string next, string prev, string modifier) GetValidatedBindings(string modifierName = null)
    {
        var nextBinding = BindingUtils.ValidateBindingOrDefault(
            PluginConfig.NextLoadoutBinding.Value, 
            "<Keyboard>/x", 
            "NextLoadoutBinding");

        var prevBinding = BindingUtils.ValidateBindingOrDefault(
            PluginConfig.PreviousLoadoutBinding.Value, 
            "<Keyboard>/z", 
            "PreviousLoadoutBinding");

        string modifierBinding = null;
        if (modifierName == null) return (nextBinding, prevBinding, null);
        // Get the appropriate config entry based on the modifier name
        var modifierConfig = modifierName == "Grenade" 
            ? PluginConfig.GrenadeLoadoutModifier 
            : PluginConfig.EmployeeLoadoutModifier;

        var defaultModifier = modifierName == "Grenade" ? "<Keyboard>/alt" : "<Keyboard>/capsLock";

        modifierBinding = BindingUtils.ValidateBindingOrDefault(
            modifierConfig.Value,
            defaultModifier,
            $"{modifierName}LoadoutModifier");

        return (nextBinding, prevBinding, modifierBinding);
    }

    private static void WeaponLoadouts()
    {
        try
        {
            var (nextBinding, prevBinding, _) = GetValidatedBindings();

            NextWeaponLoadout.AddBinding(nextBinding);
            PrevWeaponLoadout.AddBinding(prevBinding);

            NextWeaponLoadout.performed += OnNextWeaponLoadout;
            PrevWeaponLoadout.performed += OnPrevWeaponLoadout;

            NextWeaponLoadout.Enable();
            PrevWeaponLoadout.Enable();

            BasePlugin.Logger.LogInfo($"Weapon loadout bindings configured: Next={nextBinding}, Prev={prevBinding}");
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error setting up weapon loadout bindings: {ex.Message}");
        }
    }
    
    private static void GrenadeLoadouts()
    {
        try
        {
            var (nextBinding, prevBinding, modifierBinding) = GetValidatedBindings("Grenade");

            NextGrenadeLoadout.AddCompositeBinding("ButtonWithModifier")
                .With("Modifier", modifierBinding)
                .With("Button", nextBinding);

            PrevGrenadeLoadout.AddCompositeBinding("ButtonWithModifier")
                .With("Modifier", modifierBinding)
                .With("Button", prevBinding);

            NextGrenadeLoadout.performed += OnNextGrenadeLoadout;
            PrevGrenadeLoadout.performed += OnPrevGrenadeLoadout;

            NextGrenadeLoadout.Enable();
            PrevGrenadeLoadout.Enable();

            BasePlugin.Logger.LogInfo($"Grenade loadout bindings configured: Modifier={modifierBinding}, Next={nextBinding}, Prev={prevBinding}");
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error setting up grenade loadout bindings: {ex.Message}");
        }
    }
    
    private static void EmployeeLoadouts() 
    {
        try
        {
            var (nextBinding, prevBinding, modifierBinding) = GetValidatedBindings("Employee");

            NextEmployeeLoadout.AddCompositeBinding("ButtonWithModifier")
                .With("Modifier", modifierBinding)
                .With("Button", nextBinding);

            PrevEmployeeLoadout.AddCompositeBinding("ButtonWithModifier")
                .With("Modifier", modifierBinding)
                .With("Button", prevBinding);

            NextEmployeeLoadout.performed += OnNextEmployeeLoadout;
            PrevEmployeeLoadout.performed += OnPrevEmployeeLoadout;

            NextEmployeeLoadout.Enable();
            PrevEmployeeLoadout.Enable();

            BasePlugin.Logger.LogInfo($"Employee loadout bindings configured: Modifier={modifierBinding}, Next={nextBinding}, Prev={prevBinding}");
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error setting up employee loadout bindings: {ex.Message}");
        }
    }

    // Weapon loadout handlers
    private static void OnNextWeaponLoadout(InputAction.CallbackContext ctx)
    {
        BasePlugin.Logger.LogInfo("Next weapon loadout selected");

        var gear = PlayerData.GetGearData(Player.LocalPlayer.LastSelectedGear);

        var loadouts = AccessTools.FieldRefAccess<PlayerData.GearData, object>("loadouts");
        var loadoutObj = loadouts(gear);
        
        var currentLoadoutIndex = FindMatchingLoadoutIndex(gear);

        if (loadoutObj is not Array loadoutArray || loadoutArray.Length == 0)
        {
            BasePlugin.Logger.LogWarning("No loadouts available.");
            return;
        }

        var startIndex = (currentLoadoutIndex + 1) % loadoutArray.Length;
        var checkedCount = 0;

        for (var i = startIndex; checkedCount < loadoutArray.Length; i = (i + 1) % loadoutArray.Length, checkedCount++)
        {
            var loadout = loadoutArray.GetValue(i);
            var upgradesField = loadout.GetType().GetField("upgrades", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (upgradesField?.GetValue(loadout) is not IList { Count: > 0 }) continue;
            gear.EquipLoadout(i);
            BasePlugin.Logger.LogInfo($"Switched to loadout index {i}");
            return;
        }

        BasePlugin.Logger.LogWarning("No valid loadouts with upgrades found.");
    }



    private static void OnPrevWeaponLoadout(InputAction.CallbackContext ctx)
    {
        BasePlugin.Logger.LogInfo("Previous weapon loadout selected");

        var gear = PlayerData.GetGearData(Player.LocalPlayer.LastSelectedGear);

        var loadouts = AccessTools.FieldRefAccess<PlayerData.GearData, object>("loadouts");
        var loadoutObj = loadouts(gear);

        var currentLoadoutIndex = FindMatchingLoadoutIndex(gear);

        if (loadoutObj is not Array loadoutArray || loadoutArray.Length == 0)
        {
            BasePlugin.Logger.LogWarning("No loadouts available.");
            return;
        }

        var total = loadoutArray.Length;
        var startIndex = (currentLoadoutIndex - 1 + total) % total;
        var checkedCount = 0;

        for (var i = startIndex; checkedCount < total; i = (i - 1 + total) % total, checkedCount++)
        {
            var loadout = loadoutArray.GetValue(i);
            var upgradesField = loadout.GetType().GetField("upgrades", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (upgradesField?.GetValue(loadout) is not IList { Count: > 0 }) continue;

            gear.EquipLoadout(i);
            BasePlugin.Logger.LogInfo($"Switched to loadout index {i}");
            return;
        }

        BasePlugin.Logger.LogWarning("No valid loadouts with upgrades found.");
    }


    // Grenade loadout handlers
    private static void OnNextGrenadeLoadout(InputAction.CallbackContext ctx)
    {
        BasePlugin.Logger.LogInfo("Next grenade loadout selected");
        var gear = PlayerData.GetGearData(PlayerData.Instance.grenadeID);

        var loadouts = AccessTools.FieldRefAccess<PlayerData.GearData, object>("loadouts");
        var loadoutObj = loadouts(gear);
        
        var currentLoadoutIndex = FindMatchingLoadoutIndex(gear);

        if (loadoutObj is not Array loadoutArray || loadoutArray.Length == 0)
        {
            BasePlugin.Logger.LogWarning("No loadouts available.");
            return;
        }

        var startIndex = (currentLoadoutIndex + 1) % loadoutArray.Length;
        var checkedCount = 0;

        for (var i = startIndex; checkedCount < loadoutArray.Length; i = (i + 1) % loadoutArray.Length, checkedCount++)
        {
            var loadout = loadoutArray.GetValue(i);
            var upgradesField = loadout.GetType().GetField("upgrades", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (upgradesField?.GetValue(loadout) is not IList { Count: > 0 }) continue;
            gear.EquipLoadout(i);
            BasePlugin.Logger.LogInfo($"Switched to loadout index {i}");
            return;
        }

        BasePlugin.Logger.LogWarning("No valid loadouts with upgrades found.");
    }

    private static void OnPrevGrenadeLoadout(InputAction.CallbackContext ctx)
    {
        BasePlugin.Logger.LogInfo("Previous grenade loadout selected");
        var gear = PlayerData.GetGearData(PlayerData.Instance.grenadeID);

        var loadouts = AccessTools.FieldRefAccess<PlayerData.GearData, object>("loadouts");
        var loadoutObj = loadouts(gear);

        var currentLoadoutIndex = FindMatchingLoadoutIndex(gear);

        if (loadoutObj is not Array loadoutArray || loadoutArray.Length == 0)
        {
            BasePlugin.Logger.LogWarning("No loadouts available.");
            return;
        }

        var total = loadoutArray.Length;
        var startIndex = (currentLoadoutIndex - 1 + total) % total;
        var checkedCount = 0;

        for (var i = startIndex; checkedCount < total; i = (i - 1 + total) % total, checkedCount++)
        {
            var loadout = loadoutArray.GetValue(i);
            var upgradesField = loadout.GetType().GetField("upgrades", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (upgradesField?.GetValue(loadout) is not IList { Count: > 0 }) continue;

            gear.EquipLoadout(i);
            BasePlugin.Logger.LogInfo($"Switched to loadout index {i}");
            return;
        }

        BasePlugin.Logger.LogWarning("No valid loadouts with upgrades found.");
    }

    // Employee loadout handlers
    private static void OnNextEmployeeLoadout(InputAction.CallbackContext ctx)
    {
        BasePlugin.Logger.LogInfo("Next employee loadout selected");
        var gear = PlayerData.GetGearData(Player.LocalPlayer.Character);

        var loadouts = AccessTools.FieldRefAccess<PlayerData.GearData, object>("loadouts");
        var loadoutObj = loadouts(gear);
        
        var currentLoadoutIndex = FindMatchingLoadoutIndex(gear);

        if (loadoutObj is not Array loadoutArray || loadoutArray.Length == 0)
        {
            BasePlugin.Logger.LogWarning("No loadouts available.");
            return;
        }

        var startIndex = (currentLoadoutIndex + 1) % loadoutArray.Length;
        var checkedCount = 0;

        for (var i = startIndex; checkedCount < loadoutArray.Length; i = (i + 1) % loadoutArray.Length, checkedCount++)
        {
            var loadout = loadoutArray.GetValue(i);
            var upgradesField = loadout.GetType().GetField("upgrades", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (upgradesField?.GetValue(loadout) is not IList { Count: > 0 }) continue;
            gear.EquipLoadout(i);
            BasePlugin.Logger.LogInfo($"Switched to loadout index {i}");
            return;
        }

        BasePlugin.Logger.LogWarning("No valid loadouts with upgrades found.");
    }

    private static void OnPrevEmployeeLoadout(InputAction.CallbackContext ctx)
    {
        BasePlugin.Logger.LogInfo("Previous employee loadout selected");
        var gear = PlayerData.GetGearData(Player.LocalPlayer.Character);

        var loadouts = AccessTools.FieldRefAccess<PlayerData.GearData, object>("loadouts");
        var loadoutObj = loadouts(gear);

        var currentLoadoutIndex = FindMatchingLoadoutIndex(gear);

        if (loadoutObj is not Array loadoutArray || loadoutArray.Length == 0)
        {
            BasePlugin.Logger.LogWarning("No loadouts available.");
            return;
        }

        var total = loadoutArray.Length;
        var startIndex = (currentLoadoutIndex - 1 + total) % total;
        var checkedCount = 0;

        for (var i = startIndex; checkedCount < total; i = (i - 1 + total) % total, checkedCount++)
        {
            var loadout = loadoutArray.GetValue(i);
            var upgradesField = loadout.GetType().GetField("upgrades", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (upgradesField?.GetValue(loadout) is not IList { Count: > 0 }) continue;

            gear.EquipLoadout(i);
            BasePlugin.Logger.LogInfo($"Switched to loadout index {i}");
            return;
        }

        BasePlugin.Logger.LogWarning("No valid loadouts with upgrades found.");
    }

    private static int FindMatchingLoadoutIndex(PlayerData.GearData gearDataInstance)
    {
        var gearType = gearDataInstance.GetType();

        var equippedUpgradesField = gearType.GetField("equippedUpgrades", BindingFlags.Instance | BindingFlags.NonPublic);
        var equippedUpgrades = equippedUpgradesField?.GetValue(gearDataInstance) as IList;

        var loadoutsField = gearType.GetField("loadouts", BindingFlags.Instance | BindingFlags.NonPublic);
        var loadoutsArray = loadoutsField?.GetValue(gearDataInstance) as Array;

        if (loadoutsArray == null || equippedUpgrades == null) return -1;

        // 3. Iterate and compare
        for (var i = 0; i < loadoutsArray.Length; i++)
        {
            var loadout = loadoutsArray.GetValue(i);

            var upgradesField = loadout.GetType().GetField("upgrades", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var upgrades = upgradesField?.GetValue(loadout) as IList;

            if (Comparing.AreUpgradeListsEqual(upgrades, equippedUpgrades))
                return i;
        }

        return -1; // no match found
    }


    public static void Cleanup()
    {
        try
        {
            // Unsubscribe all events (with null checks)
            if (NextWeaponLoadout != null) NextWeaponLoadout.performed -= OnNextWeaponLoadout;
            if (PrevWeaponLoadout != null) PrevWeaponLoadout.performed -= OnPrevWeaponLoadout;

            if (NextGrenadeLoadout != null) NextGrenadeLoadout.performed -= OnNextGrenadeLoadout;
            if (PrevGrenadeLoadout != null) PrevGrenadeLoadout.performed -= OnPrevGrenadeLoadout;

            if (NextEmployeeLoadout != null) NextEmployeeLoadout.performed -= OnNextEmployeeLoadout;
            if (PrevEmployeeLoadout != null) PrevEmployeeLoadout.performed -= OnPrevEmployeeLoadout;

            // Disable and dispose actions
            NextWeaponLoadout?.Disable();
            PrevWeaponLoadout?.Disable();
            NextGrenadeLoadout?.Disable();
            PrevGrenadeLoadout?.Disable();
            NextEmployeeLoadout?.Disable();
            PrevEmployeeLoadout?.Disable();

            BasePlugin.Logger.LogInfo("Input system cleaned up successfully");
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error during input system cleanup: {ex.Message}");
        }
    }


    public class Comparing
    {
        internal static bool AreUpgradeListsEqual(IList listA, IList listB)
        {
            if (listA == null || listB == null) return false;
            if (listA.Count != listB.Count) return false;

            return !listA.Cast<object>().Where((t, i) => !AreUpgradeItemsEqual(t, listB[i])).Any();
        }

        private static bool AreUpgradeItemsEqual(object a, object b)
        {
            if (a == null || b == null) return false;
            var type = a.GetType();

            if (type != b.GetType()) return false;

            return !(from field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) let valA = field.GetValue(a) let valB = field.GetValue(b) where !Equals(valA, valB) select valA).Any();
        }
    }
    private static class BindingUtils
    {
        public static string ValidateBindingOrDefault(string binding, string defaultBinding, string bindingName = "")
        {
            try
            {
                var test = new InputAction();
                test.AddBinding(binding);
                test.Dispose();
                return binding;
            }
            catch (Exception e)
            {
                BasePlugin.Logger.LogError($"Error validating binding {bindingName}: {e.Message}");
                return defaultBinding;
            }
        }
    }
    
}