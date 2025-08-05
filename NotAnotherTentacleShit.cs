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
    
    // Cooldown tracking
    private static float _lastLoadoutChangeTime = -999f;
    private static LoadoutType? _activeWheelType;

    private static PropertyInfo _isTextChatOpenPlayerLook;

    private static bool IsTextChatOpen
    {
        get
        {
            try
            {
                if (_isTextChatOpenPlayerLook == null)
                {
                    _isTextChatOpenPlayerLook = AccessTools.Property(typeof(PlayerLook), "IsTextChatOpen");
                    if (_isTextChatOpenPlayerLook == null)
                    {
                        // Field not found, log warning and return false
                        BasePlugin.Logger.LogWarning("IsTextChatOpen field not found in PlayerLook type. Defaulting to false.");
                        return false;
                    }
                }

                var result = _isTextChatOpenPlayerLook.GetValue(PlayerLook.Instance);
                return result != null && (bool)result;
            }
            catch (Exception ex)
            {
                BasePlugin.Logger.LogError($"Error accessing IsTextChatOpen: {ex}");
                return false;
            }
        }
    }

    
    // Initialize InputActions directly, not through patches
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
            var quipWheelField = AccessTools.Field(typeof(Highlighter), "quipWheel");
            var quipWheel = quipWheelField?.GetValue(highlighterInstance) as RectTransform;

            if (quipWheel == null)
            {
                // Try to call the original method to see if it initializes anything
                var originalMethod = AccessTools.Method(typeof(Highlighter), "HandleQuipWheel");
                if (originalMethod != null)
                {
                    originalMethod.Invoke(highlighterInstance, null);

                    // Check again after calling original
                    quipWheel = quipWheelField?.GetValue(highlighterInstance) as RectTransform;
                }
            
                if (quipWheel == null)
                {
                    BasePlugin.Logger.LogError("quipWheel is still null even after calling original method");
                    return;
                }
            }

            
            
            var enableQuipWheelField = AccessTools.Field(typeof(Highlighter), "enableQuipWheel");
            var enableEquipWheelTimeField = AccessTools.Field(typeof(Highlighter), "enableEquipWheelTime");
            var timeBeforeActivatingWheelField = AccessTools.Field(typeof(Highlighter), "timeBeforeActivatingWheel");
            var selectedQuipIndexField = AccessTools.Field(typeof(Highlighter), "selectedQuipIndex");
            var quipLabelField = AccessTools.Field(typeof(Highlighter), "quipLabel");
            var quipIconsField = AccessTools.Field(typeof(Highlighter), "quipIcons");
            var quipSelectorField = AccessTools.Field(typeof(Highlighter), "quipSelector");
            var quipSelectedColorField = AccessTools.Field(typeof(Highlighter), "quipSelectedColor");
            var quipUnselectedColorField = AccessTools.Field(typeof(Highlighter), "quipUnselectedColor");
            var quipSelectedScaleField = AccessTools.Field(typeof(Highlighter), "quipSelectedScale");
            var emoteBindingField = AccessTools.Field(typeof(Highlighter), "emoteBinding");
            

            var enableQuipWheel = (bool)(enableQuipWheelField.GetValue(highlighterInstance) ?? false);
            var enableEquipWheelTime = (float)(enableEquipWheelTimeField?.GetValue(highlighterInstance) ?? 0f);
            var timeBeforeActivatingWheel = (float)(timeBeforeActivatingWheelField?.GetValue(highlighterInstance) ?? 0.05f);
            var selectedQuipIndex = (int)(selectedQuipIndexField?.GetValue(highlighterInstance) ?? 0);
            var quipLabel = quipLabelField?.GetValue(highlighterInstance) as TextMeshProUGUI;
            var quipIcons = quipIconsField?.GetValue(highlighterInstance) as List<Image>;
            var quipSelector = quipSelectorField?.GetValue(highlighterInstance) as Image;
            var quipSelectedColor = (Color)(quipSelectedColorField?.GetValue(highlighterInstance) ?? Color.white);
            var quipUnselectedColor = (Color)(quipUnselectedColorField?.GetValue(highlighterInstance) ?? Color.gray);
            var quipSelectedScale = (float)(quipSelectedScaleField?.GetValue(highlighterInstance) ?? 1.2f);
            var emoteBinding = emoteBindingField?.GetValue(highlighterInstance) as TextMeshPro;

            if (emoteBinding != null) emoteBinding.text = "";


            if (quipWheel == null)
            {
                BasePlugin.Logger.LogError("quipWheel GameObject is null");
                return;
            }

            if (!enableQuipWheel || Time.unscaledTime - enableEquipWheelTime < timeBeforeActivatingWheel)
                return;

            if (!quipWheel.gameObject.activeSelf)
            {
                PlayerInput.EnableMenu();
                quipWheel.gameObject.SetActive(true);
                
                // Null check for PlayerLook.Instance
                if (PlayerLook.Instance != null)
                {
                    ++PlayerLook.Instance.RotationLocksX;
                    ++PlayerLook.Instance.RotationLocksY;
                }
                
                // Null check for Player.LocalPlayer
                if (Player.LocalPlayer != null)
                {
                    Player.LocalPlayer.LockFiring(true);
                }

                // Set initial loadout label
                if (_currentLoadouts.Count > 0 && quipLabel != null)
                {
                    quipLabel.text = _currentLoadouts[selectedQuipIndex].Label;
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

                    // Update selector rotation
                    if (quipSelector != null)
                    {
                        quipSelector.rectTransform.localEulerAngles = new Vector3(0.0f, 0.0f,
                            Mathf.LerpAngle(quipSelector.rectTransform.localEulerAngles.z,
                                (float)(newIndex * angleStep + angleStep + 90.0), 18f * Time.deltaTime));
                    }

                    // Update icon colors and scales
                    if (quipIcons != null)
                    {
                        for (var i = 0; i < Mathf.Min(quipIcons.Count, _currentLoadouts.Count); ++i)
                        {
                            if (quipIcons[i] != null)
                            {
                                quipIcons[i].color = i == newIndex ? quipSelectedColor : quipUnselectedColor;
                                quipIcons[i].rectTransform.localScale = i == newIndex ?
                                    new Vector3(quipSelectedScale, quipSelectedScale, quipSelectedScale) : Vector3.one;
                            }
                        }
                    }

                    if (quipLabel == null) return;
                    var remainingCooldown = GetRemainingCooldown();
                    var selectedLoadout = _currentLoadouts[selectedQuipIndex];
    
                    quipLabel.text = remainingCooldown > 0 
                        ? $"{selectedLoadout.Label}\n\n<size=32><color=white>On Cooldown\n{remainingCooldown:F1}s</size></color>" 
                        : $"{selectedLoadout.Label}\n\n<size=32><color=white>Ready!</size></color>";
                    // Update label if selection changed
                    if (selectedQuipIndex == newIndex) return;
                    selectedQuipIndexField?.SetValue(highlighterInstance, newIndex);
                    
                    
                }
            }
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error in HandleWeaponLoadoutWheel: {ex}");
            throw; // Re-throw to be caught by the calling method
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

            var highlighterInstance = UnityEngine.Object.FindObjectOfType<Highlighter>();
            if (highlighterInstance == null)
            {
                BasePlugin.Logger.LogError("Could not find Highlighter instance");
                return;
            }

            // BasePlugin.Logger.LogInfo($"WeaponLoadoutWheel called with performed: {performed}");

            if (performed)
            {
                _activeWheelType = type;

                var quipWheelField = AccessTools.Field(typeof(Highlighter), "quipWheel");
                var quipWheel = quipWheelField?.GetValue(highlighterInstance) as RectTransform;
        
                // BasePlugin.Logger.LogInfo($"Before enabling mode - quipWheel: {quipWheel?.name ?? "null"}");
        
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

                var fieldRefLoadouts = AccessTools.FieldRefAccess<PlayerData.GearData, object>("loadouts");
                var loadoutsObj = fieldRefLoadouts(gear);

                if (loadoutsObj is not Array { Length: > 0 } loadouts)
                {
                    BasePlugin.Logger.LogWarning("No loadouts available for current weapon");
                    _activeWheelType = null;
                    return;
                }

                var loadoutDataList = new List<LoadoutData>();

                for (var i = 0; i < loadouts.Length; i++)
                {
                    var loadout = loadouts.GetValue(i);
                    var upgradesField = loadout.GetType().GetField("upgrades", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (upgradesField?.GetValue(loadout) is not IList { Count: > 0 }) continue;
        
                    var loadoutData = new LoadoutData
                    {
                        Label = $"Loadout {i + 1}",
                        Icon = gear.GetLoadoutIcon(i),
                        GearData = gear,
                        LoadoutIndex = i
                    };
                    loadoutDataList.Add(loadoutData);
                }

                // Check if we have any valid loadouts BEFORE enabling the wheel
                if (loadoutDataList.Count == 0)
                {
                    BasePlugin.Logger.LogWarning("No valid loadouts found, weapon wheel will not be activated");
                    return;
                }

                _isLoadoutWheelMode = true;
                
                var enableQuipWheelField = AccessTools.Field(typeof(Highlighter), "enableQuipWheel");
                var enableQuipWheelTimeField = AccessTools.Field(typeof(Highlighter), "enableEquipWheelTime");
                var selectedQuipIndexField = AccessTools.Field(typeof(Highlighter), "selectedQuipIndex");
                var emoteLabelField = AccessTools.Field(typeof(Highlighter), "emoteLabel");
                var emoteBindingField = AccessTools.Field(typeof(Highlighter), "emoteBinding");
                
                var emoteBinding = emoteBindingField?.GetValue(highlighterInstance) as TextMeshPro;
                var emoteLabel = emoteLabelField?.GetValue(highlighterInstance) as TextMeshProUGUI;

                if (emoteBinding != null) emoteBinding.text = "";
                if (emoteLabel != null) emoteLabel.text = gear.Gear.Info.Name;
                enableQuipWheelField?.SetValue(highlighterInstance, true);
                enableQuipWheelTimeField?.SetValue(highlighterInstance, Time.unscaledTime);
                selectedQuipIndexField?.SetValue(highlighterInstance, 0);

                // Call our custom setup method
                SetupLoadouts(highlighterInstance, loadoutDataList);

                // BasePlugin.Logger.LogInfo($"Setup weapon loadout wheel with {loadoutDataList.Count} valid loadouts}");
            }
            else
            {
                _activeWheelType = null;
                
                var quipWheelField = AccessTools.Field(typeof(Highlighter), "quipWheel");
                var selectedQuipIndexField = AccessTools.Field(typeof(Highlighter), "selectedQuipIndex");
                var quipWheel = quipWheelField?.GetValue(highlighterInstance) as RectTransform;
                var selectedIndex = (int)(selectedQuipIndexField?.GetValue(highlighterInstance) ?? 0);
                
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

                var enableQuipWheelField = AccessTools.Field(typeof(Highlighter), "enableQuipWheel");
                enableQuipWheelField?.SetValue(highlighterInstance, false);

                _isLoadoutWheelMode = false;

                // BasePlugin.Logger.LogInfo($"Applying weapon loadout at index: {selectedIndex}");

                // Apply the selected loadout using our stored data
                ApplySelectedLoadout(selectedIndex);
            }
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Error in WeaponLoadoutWheel: {ex}");
            _isLoadoutWheelMode = false; // Reset mode on error
            _activeWheelType = null;
        }
    }
    
    private static readonly List<LoadoutData> _currentLoadouts = [];
    
    private static void SetupLoadouts(Highlighter highlighterInstance, List<LoadoutData> loadouts)
    {
        try
        {
            // Store the current loadouts for later use
            _currentLoadouts.Clear();
            _currentLoadouts.AddRange(loadouts);

            // Get necessary fields from Highlighter
            var quipIconsField = AccessTools.Field(typeof(Highlighter), "quipIcons");
            var quipWheelField = AccessTools.Field(typeof(Highlighter), "quipWheel");
            var quipWheelRadiusField = AccessTools.Field(typeof(Highlighter), "quipWheelRadius");
            var quipIconSizeField = AccessTools.Field(typeof(Highlighter), "quipIconSize");
            var quipSelectorField = AccessTools.Field(typeof(Highlighter), "quipSelector");

            var quipIcons = quipIconsField?.GetValue(highlighterInstance) as List<Image>;
            var quipWheel = quipWheelField?.GetValue(highlighterInstance) as RectTransform;
            var quipWheelRadius = (float)(quipWheelRadiusField?.GetValue(highlighterInstance) ?? 100f);
            var quipIconSize = (float)(quipIconSizeField?.GetValue(highlighterInstance) ?? 50f);
            var quipSelector = quipSelectorField?.GetValue(highlighterInstance) as Image;

            if (quipIcons == null || quipWheel == null)
            {
                BasePlugin.Logger.LogError("Required UI components are null");
                return;
            }

            var angleStep = 360f / loadouts.Count;

            for (var i = 0; i < loadouts.Count; i++)
            {
                // Create new icon if needed
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

                // Set the icon sprite
                if (quipIcons[i] != null)
                {
                    quipIcons[i].sprite = loadouts[i].Icon;

                    // Position the icon in a circle
                    var angle = (angleStep * i + angleStep * 0.5f) * Mathf.Deg2Rad;
                    var position = new Vector2(
                        Mathf.Cos(angle) * quipWheelRadius,
                        Mathf.Sin(angle) * quipWheelRadius
                    );

                    quipIcons[i].rectTransform.anchoredPosition = position;
                    quipIcons[i].rectTransform.sizeDelta = new Vector2(quipIconSize, quipIconSize);
                }
            }

            // Hide unused icons
            for (var i = loadouts.Count; i < quipIcons.Count; i++)
            {
                if (quipIcons[i] != null && quipIcons[i].gameObject.activeSelf)
                    quipIcons[i].gameObject.SetActive(false);
            }

            // Update selector
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

            // Check cooldown
            var remainingCooldown = GetRemainingCooldown();
            if (remainingCooldown > 0)
            {
                // BasePlugin.Logger.LogWarning($"Loadout change on cooldown. {remainingCooldown:F1} seconds remaining.");
                return;
            }

            var selectedLoadout = _currentLoadouts[selectedIndex];
            
            if (selectedLoadout.GearData != null)
            {
                var eqField = selectedLoadout.GearData.GetType().GetField("equippedUpgrades", BindingFlags.Instance | BindingFlags.NonPublic);;
                var loadField = selectedLoadout.GearData.GetType().GetField("loadouts", BindingFlags.Instance | BindingFlags.NonPublic);
                
                var equippedUpgrades = eqField?.GetValue(selectedLoadout.GearData) as IList;
                var loadouts = loadField?.GetValue(selectedLoadout.GearData) as Array;
                var loadout = loadouts?.GetValue(selectedLoadout.LoadoutIndex);
                
                var upField = loadout?.GetType().GetField("upgrades", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var upgrades = upField?.GetValue(loadout) as IList;
                if (Comparing.AreUpgradeListsEqual(equippedUpgrades, upgrades)) return;
                
                EquipAndApply(selectedLoadout);
                DamagePlayerByPercentage(Player.LocalPlayer, PluginConfig.LoadoutChangeDMGPercent.Value);
                
                // Set cooldown
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