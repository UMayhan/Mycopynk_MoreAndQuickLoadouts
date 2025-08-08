using System;
using System.Collections;
using HarmonyLib;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pigeon.Movement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MoreAndQuickLoadouts;

public class LoadoutData
{
    public string Label;
    public Sprite Icon;
    public PlayerData.GearData GearData;
    public int LoadoutIndex;
}

/// <summary>
/// Highlighter_Patch
/// </summary>
[HarmonyPatch]
public static class NotAnotherTentacleShit
{
    private static InputAction WeaponWheelToggle;
    private static InputAction GrenadeWheelToggle;
    private static InputAction EmployeeWheelToggle;
    private static bool _isLoadoutWheelMode;
    private static bool _initialized;
    
    private static float _lastLoadoutChangeTime = -999f;
    private static LoadoutType? _activeWheelType;


    private static bool IsTextChatOpen
    {
        get
        {
            try
            {
                return PlayerLook.Instance.IsTextChatOpen;
            }
            catch (Exception ex)
            {
                BasePlugin.Logger.LogError($"Error accessing IsTextChatOpen: {ex}");
                return false;
            }
        }
    }

    
    public static void InitializeInputActions()
    {
        if (_initialized) return;
        
        try
        {
            WeaponWheelToggle = new InputAction("WeaponWheelToggle", InputActionType.Button, PluginConfig.WeaponLoadoutBinding.Value);
            GrenadeWheelToggle = new InputAction("GrenadeWheelToggle", InputActionType.Button, PluginConfig.GrenadeLoadoutBinding.Value);
            EmployeeWheelToggle = new InputAction("EmployeeWheelToggle", InputActionType.Button, PluginConfig.EmployeeLoadoutBinding.Value);
            
            WeaponWheelToggle.Enable();
            GrenadeWheelToggle.Enable();
            EmployeeWheelToggle.Enable();
            
            WeaponWheelToggle.performed += OnWeaponWheelToggle;
            WeaponWheelToggle.canceled += OnWeaponWheelToggle;
            
            GrenadeWheelToggle.performed += OnGrenadeWheelToggle;
            GrenadeWheelToggle.canceled += OnGrenadeWheelToggle;
            
            EmployeeWheelToggle.performed += OnEmployeeWheelToggle;
            EmployeeWheelToggle.canceled += OnEmployeeWheelToggle;
            
            _initialized = true;
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Failed to initialize WeaponWheelToggle InputAction: {ex}");
        }
    }
    
    
    public static void CleanupInputActions()
    {
        if (!_initialized) return;
    
        try
        {
            if (WeaponWheelToggle != null)
            {
                WeaponWheelToggle.performed -= OnWeaponWheelToggle;
                WeaponWheelToggle.canceled -= OnWeaponWheelToggle;
            
                WeaponWheelToggle.Disable();
                WeaponWheelToggle.Dispose();
                WeaponWheelToggle = null;
            }
        
            if (GrenadeWheelToggle != null)
            {
                GrenadeWheelToggle.performed -= OnGrenadeWheelToggle;
                GrenadeWheelToggle.canceled -= OnGrenadeWheelToggle;
            
                GrenadeWheelToggle.Disable();
                GrenadeWheelToggle.Dispose();
                GrenadeWheelToggle = null;
            }
        
            if (EmployeeWheelToggle != null)
            {
                EmployeeWheelToggle.performed -= OnEmployeeWheelToggle;
                EmployeeWheelToggle.canceled -= OnEmployeeWheelToggle;
            
                EmployeeWheelToggle.Disable();
                EmployeeWheelToggle.Dispose();
                EmployeeWheelToggle = null;
            }
        
            _initialized = false;
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Failed to cleanup input actions: {ex}");
        }
    }
    
    [HarmonyPatch(typeof(Highlighter), "HandleQuipWheel")]
    [HarmonyPrefix]
    public static bool Prefix_HandleQuipWheel(Highlighter __instance)
    {
        try
        {
            if (Player.LocalPlayer == null || !Player.LocalPlayer.IsAlive)
            {
                return true; 
            }

            if (!_isLoadoutWheelMode) return true; 
            
            HandleWeaponLoadoutWheel(__instance);
            return false;

        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error in Prefix_HandleQuipWheel: {ex}");
            _isLoadoutWheelMode = false; 
            return true; 
        }
    }

    
    private static void OnWeaponWheelToggle(InputAction.CallbackContext ctx)
    {
        try
        {
            if (IsTextChatOpen) return;
            
            if (_activeWheelType.HasValue && _activeWheelType != LoadoutType.Weapon)
                return;

            WeaponLoadoutWheel(ctx is { performed: true }, LoadoutType.Weapon);
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error in OnWeaponWheelToggle: {ex}");
        }
    }
    
    private static void OnGrenadeWheelToggle(InputAction.CallbackContext ctx)
    {
        try
        {
            if (IsTextChatOpen) return;
            
            if (_activeWheelType.HasValue && _activeWheelType != LoadoutType.Grenade)
                return;

            WeaponLoadoutWheel(ctx is { performed: true }, LoadoutType.Grenade);
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error in OnGrenadeWheelToggle: {ex}");
        }
    }
    
    
    private static void OnEmployeeWheelToggle(InputAction.CallbackContext ctx)
    {
        try
        {
            if (IsTextChatOpen) return;
            
            if (_activeWheelType.HasValue && _activeWheelType != LoadoutType.Employee)
                return;

            WeaponLoadoutWheel(ctx is { performed: true }, LoadoutType.Employee);
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error in OnEmployeeWheelToggle: {ex}");
        }
    }
    
    private static void HandleWeaponLoadoutWheel(Highlighter highlighterInstance)
    {
        if (highlighterInstance == null)
        {
            BasePlugin.Logger.LogError("Highlighter instance is null");
            return;
        }

        try
        {
            var enableEquipWheelTime = Highlighter.Instance.enableEquipWheelTime;
            var enableQuipWheel = Highlighter.Instance.enableQuipWheel;
            var timeBeforeActivatingWheel = Highlighter.Instance.timeBeforeActivatingWheel;
            var quipSelectedColor = Highlighter.Instance.quipSelectedColor;
            var quipUnselectedColor = Highlighter.Instance.quipUnselectedColor;
            var quipSelectedScale = Highlighter.Instance.quipSelectedScale;
            
            var emoteBinding = Highlighter.Instance.emoteBinding;
            
            emoteBinding.text = "";
            
            if (Highlighter.Instance.quipWheel == null)
            {
                BasePlugin.Logger.LogError("quipWheel GameObject is null");
                return;
            }

            if (!enableQuipWheel || Time.unscaledTime - enableEquipWheelTime < timeBeforeActivatingWheel)
                return;

            if (!Highlighter.Instance.quipWheel.gameObject.activeSelf)
            {
                PlayerInput.EnableMenu();
                Highlighter.Instance.quipWheel.gameObject.SetActive(true);

                if (PlayerLook.Instance != null)
                {
                    ++PlayerLook.Instance.RotationLocksX;
                    ++PlayerLook.Instance.RotationLocksY;
                }

                if (Player.LocalPlayer != null)
                {
                    Player.LocalPlayer.LockFiring(true);
                }

                if (_currentLoadouts.Count > 0 && Highlighter.Instance.quipLabel != null)
                {
                    Highlighter.Instance.quipLabel.text = _currentLoadouts[Highlighter.Instance.selectedQuipIndex].Label;
                }
            }


            if (PlayerInput.Controls?.Menu.Point != null)
            {
                var vector2 = PlayerInput.Controls.Menu.Point.ReadValue<Vector2>();
                vector2.x -= (float)Screen.width * 0.5f;
                vector2.y -= (float)Screen.height * 0.5f;

                if (_currentLoadouts.Count > 0)
                {
                    var angleStep = 360f / (float)_currentLoadouts.Count;
                    var current = Mathf.Atan2(vector2.y, vector2.x) * 57.29578f;

                    if (current < 0.0)
                        current += 360f;
                    else if (current >= 360.0)
                        current -= 360f;

                    var newIndex = 0;
                    var minAngleDiff = float.MaxValue;

                    for (var i = 0; i < _currentLoadouts.Count; ++i)
                    {
                        var angleDiff = Mathf.Abs(Mathf.DeltaAngle(current, (float)(i * angleStep + angleStep * 0.5)));
                        if (angleDiff < minAngleDiff)
                        {
                            minAngleDiff = angleDiff;
                            newIndex = i;
                        }
                    }

                    if (Highlighter.Instance.quipSelector != null)
                    {
                        Highlighter.Instance.quipSelector.rectTransform.localEulerAngles = new Vector3(0.0f, 0.0f,
                            Mathf.LerpAngle(Highlighter.Instance.quipSelector.rectTransform.localEulerAngles.z,
                                (float)(newIndex * angleStep + angleStep + 90.0), 18f * Time.deltaTime));
                    }

                    if (Highlighter.Instance.quipIcons != null)
                    {
                        for (var i = 0; i < Mathf.Min(Highlighter.Instance.quipIcons.Count, _currentLoadouts.Count); ++i)
                        {
                            if (Highlighter.Instance.quipIcons[i] == null) continue;
                            
                            Highlighter.Instance.quipIcons[i].color = i == newIndex ? quipSelectedColor : quipUnselectedColor;
                            Highlighter.Instance.quipIcons[i].rectTransform.localScale = i == newIndex ?
                                new Vector3(quipSelectedScale, quipSelectedScale, quipSelectedScale) : Vector3.one;
                        }
                    }

                    if (Highlighter.Instance.quipLabel == null) return;
                    var remainingCooldown = GetRemainingCooldown();
                    var selectedLoadout = _currentLoadouts[Highlighter.Instance.selectedQuipIndex];
    
                    Highlighter.Instance.quipLabel.text = remainingCooldown > 0 
                        ? $"{selectedLoadout.Label}\n\n<size=32><color=white>On Cooldown\n{remainingCooldown:F1}s</size></color>" 
                        : $"{selectedLoadout.Label}\n\n<size=32><color=white>Ready!</size></color>";
                    if (Highlighter.Instance.selectedQuipIndex == newIndex) return;
                    Highlighter.Instance.selectedQuipIndex = newIndex;
                    
                    
                }
            }
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error in HandleWeaponLoadoutWheel: {ex}");
            throw;
        }
    }

    private enum LoadoutType
    {
        Weapon,
        Grenade,
        Employee
    }
    
    private static void WeaponLoadoutWheel(bool performed, LoadoutType type)
    {
        try
        {
            if (Player.LocalPlayer == null || !Player.LocalPlayer.IsAlive)
            {
                BasePlugin.Logger.LogDebug("Player is null or not alive, ignoring weapon wheel input");
                return;
            }

            var highlighterInstance = Highlighter.Instance;
            if (highlighterInstance == null)
            {
                BasePlugin.Logger.LogError("Could not find Highlighter instance");
                return;
            }

            if (performed)
            {

                var gear = type switch
                {
                    LoadoutType.Weapon => PlayerData.GetGearData(Player.LocalPlayer.SelectedGear),
                    LoadoutType.Grenade => PlayerData.GetGearData(PlayerData.Instance.grenadeID),
                    LoadoutType.Employee => PlayerData.GetGearData(Player.LocalPlayer.Character),
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };

                if (gear == null)
                {
                    BasePlugin.Logger.LogError("Could not get weapon gear data");
                    return;
                }

                var loadoutsObj = gear.loadouts;

                if (loadoutsObj is not Array { Length: > 0 })
                {
                    BasePlugin.Logger.LogWarning("No loadouts available for current weapon");
                    return;
                }

                var loadoutDataList = new List<LoadoutData>();

                for (var i = 0; i < loadoutsObj.Length; i++)
                {
                    var upgrades = loadoutsObj[i].upgrades;

                    if (upgrades is not IList { Count: > 0 }) continue;
                    
                    var loadoutData = new LoadoutData
                    {
                        Label = $"Loadout {i + 1}",
                        Icon = gear.GetLoadoutIcon(i),
                        GearData = gear,
                        LoadoutIndex = i
                    };
                    loadoutDataList.Add(loadoutData);
                }

                if (loadoutDataList.Count == 0)
                {
                    BasePlugin.Logger.LogWarning("No valid loadouts found, weapon wheel will not be activated");
                    return;
                }

                _isLoadoutWheelMode = true;
                _activeWheelType = type;


                Highlighter.Instance.emoteBinding.text = "";
                Highlighter.Instance.emoteLabel.text = gear.Gear.Info.Name;
                
                Highlighter.Instance.enableQuipWheel = true;
                Highlighter.Instance.enableEquipWheelTime = Time.unscaledTime;
                Highlighter.Instance.selectedQuipIndex = 0;


                SetupLoadouts(highlighterInstance, loadoutDataList);
            }
            else
            {
                _activeWheelType = null;

                var quipWheel = Highlighter.Instance.quipWheel;
                var selectedIndex = Highlighter.Instance.selectedQuipIndex;
                
                if (quipWheel != null && quipWheel.gameObject.activeSelf)
                {
                    PlayerInput.DisableMenu();
                    quipWheel.gameObject.SetActive(false);
                
                    if (PlayerLook.Instance != null)
                    {
                        --PlayerLook.Instance.RotationLocksX;
                        --PlayerLook.Instance.RotationLocksY;
                    }
                
                    if (Player.LocalPlayer != null)
                    {
                        Player.LocalPlayer.LockFiring(false);
                    }
                }

                Highlighter.Instance.enableQuipWheel = false;
                
                _isLoadoutWheelMode = false;

                ApplySelectedLoadout(selectedIndex);
            }
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error in WeaponLoadoutWheel: {ex}");
            _isLoadoutWheelMode = false;
            _activeWheelType = null;
        }
    }
    
    private static readonly List<LoadoutData> _currentLoadouts = [];
    
    private static void SetupLoadouts(Highlighter highlighterInstance, List<LoadoutData> loadouts)
    {
        try
        {
            _currentLoadouts.Clear();
            _currentLoadouts.AddRange(loadouts);

            var quipIcons = highlighterInstance.quipIcons;
            var quipWheel = highlighterInstance.quipWheel;
            var quipWheelRadius = highlighterInstance.quipWheelRadius;
            var quipIconSize = highlighterInstance.quipIconSize;
            var quipSelector = highlighterInstance.quipSelector;

            if (quipIcons == null || quipWheel == null)
            {
                BasePlugin.Logger.LogError("Required UI components are null");
                return;
            }

            var angleStep = 360f / loadouts.Count;

            for (var i = 0; i < loadouts.Count; i++)
            {
                if (i >= quipIcons.Count)
                {
                    var iconObject = new GameObject(loadouts[i].Label);
                    iconObject.transform.SetParent(quipWheel, false);
                    iconObject.AddComponent<CanvasRenderer>();
                    var imageComponent = iconObject.AddComponent<Image>();
                    imageComponent.raycastTarget = false;
                    quipIcons.Add(imageComponent);
                }
                else if (!quipIcons[i].gameObject.activeSelf)
                {
                    quipIcons[i].gameObject.SetActive(true);
                }

                if (quipIcons[i] != null)
                {
                    quipIcons[i].sprite = loadouts[i].Icon;

                    var angle = (angleStep * i + angleStep * 0.5f) * Mathf.Deg2Rad;
                    var position = new Vector2(
                        Mathf.Cos(angle) * quipWheelRadius,
                        Mathf.Sin(angle) * quipWheelRadius
                    );

                    quipIcons[i].rectTransform.anchoredPosition = position;
                    quipIcons[i].rectTransform.sizeDelta = new Vector2(quipIconSize, quipIconSize);
                }
            }

            for (var i = loadouts.Count; i < quipIcons.Count; i++)
            {
                if (quipIcons[i] != null && quipIcons[i].gameObject.activeSelf)
                    quipIcons[i].gameObject.SetActive(false);
            }

            if (quipSelector != null)
            {
                quipSelector.fillAmount = angleStep / 360f;
            }
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error in SetupLoadouts: {ex}");
        }
    }

    private static float GetRemainingCooldown()
    {
        var cooldownDuration = PluginConfig.LoadoutChangeCooldown.Value;
        var timeSinceLastChange = Time.unscaledTime - _lastLoadoutChangeTime;
        return Mathf.Max(0f, cooldownDuration - timeSinceLastChange);
    }

    private static void ApplySelectedLoadout(int selectedIndex)
    {
        try
        {
            if (selectedIndex < 0 || selectedIndex >= _currentLoadouts.Count)
            {
                BasePlugin.Logger.LogWarning($"Invalid loadout index: {selectedIndex}");
                return;
            }

            var remainingCooldown = GetRemainingCooldown();
            if (remainingCooldown > 0)
            {
                return;
            }

            var selectedLoadout = _currentLoadouts[selectedIndex];
            
            if (selectedLoadout.GearData != null)
            {
                var equippedUpgrades = selectedLoadout.GearData.equippedUpgrades;
                var loadouts = selectedLoadout.GearData.loadouts;
                var loadout = loadouts[selectedLoadout.LoadoutIndex];

                if (Comparing.AreUpgradeListsEqual(equippedUpgrades, loadout.upgrades)) return;
                
                EquipAndApply(selectedLoadout);
                DamagePlayerByPercentage(Player.LocalPlayer, PluginConfig.LoadoutChangeDMGPercent.Value);

                _lastLoadoutChangeTime = Time.unscaledTime;
            }
            else
            {
                BasePlugin.Logger.LogError("Selected loadout has null GearData");
            }
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error in ApplySelectedLoadout: {ex}");
        }
    }

    private static void EquipAndApply(LoadoutData selectedLoadout)
    {
        selectedLoadout.GearData.EquipLoadout(selectedLoadout.LoadoutIndex);
                
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
    }
    
    private static void DamagePlayerByPercentage(Player player, float percentage)
    {
        if (player == null || !player.IsAlive) return;
         
        var baseHealth = player.MaxHealth;
        var damageAmount = baseHealth * (percentage / 50f);
         
        var damageData = new DamageData
        {
            damage = damageAmount,
        };
         
        player.Damage(damageData, player, player.transform.position);
    }

    private static class Comparing
    { 
        internal static bool AreUpgradeListsEqual(IList a, IList b) 
        { 
            if (a == null && b == null) return true; 
            if (a == null || b == null || a.Count != b.Count) return false; 
            return !a.Cast<object>().Where((x, i) => !Equals(x, b[i])).Any();
        }
    }
}