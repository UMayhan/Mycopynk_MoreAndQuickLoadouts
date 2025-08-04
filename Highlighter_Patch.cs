using System;
using HarmonyLib;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Pigeon.Movement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MoreAndQuickLoadouts;

public struct LoadoutData
{
    public string Label;
    public Sprite Icon;
    public PlayerData.GearData GearData;
    public int LoadoutIndex;
}

[HarmonyPatch]
public static class Highlighter_Patch
{
    public static InputAction WeaponWheelToggle;
    private static bool _isWeaponWheelMode;
    
    private static bool _initialized = false;
    
    // Initialize InputActions directly, not through patches
    public static void InitializeInputActions()
    {
        if (_initialized) return;
        
        try
        {
            WeaponWheelToggle = new InputAction("WeaponWheelToggle", InputActionType.Button, "<keyboard>/l");
            WeaponWheelToggle.Enable();
            
            WeaponWheelToggle.performed += OnWeaponWheelToggle;
            WeaponWheelToggle.canceled += OnWeaponWheelToggle;
            
            _initialized = true;
            BasePlugin.Logger.LogInfo("WeaponWheelToggle InputAction initialized successfully");
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
            _initialized = false;
            BasePlugin.Logger.LogInfo("WeaponWheelToggle InputAction cleaned up");
        }
        catch (Exception ex)
        {
            BasePlugin.Logger.LogError($"Failed to cleanup WeaponWheelToggle InputAction: {ex}");
        }
    }
    
    [HarmonyPatch(typeof(Highlighter), "HandleQuipWheel")]
    [HarmonyPrefix]
    public static bool Prefix_HandleQuipWheel(Highlighter __instance)
    {
        // If we're in weapon wheel mode, handle it with our custom logic
        if (!_isWeaponWheelMode) return true; // Execute original method for normal quip wheel
        HandleWeaponLoadoutWheel(__instance);
        return false; // Skip original method

    }
    
    private static void OnWeaponWheelToggle(InputAction.CallbackContext ctx)
    {
        BasePlugin.Logger.LogInfo("ToggleQuipWheel called from our custom InputAction");
        WeaponLoadoutWheel(ctx is { performed: true });
    }
    
    
    private static void HandleWeaponLoadoutWheel(Highlighter highlighterInstance)
    {
        var enableQuipWheelField = AccessTools.Field(typeof(Highlighter), "enableQuipWheel");
        var enableQuipWheelTimeField = AccessTools.Field(typeof(Highlighter), "enableQuipWheelTime");
        var timeBeforeActivatingWheelField = AccessTools.Field(typeof(Highlighter), "timeBeforeActivatingWheel");
        var quipWheelField = AccessTools.Field(typeof(Highlighter), "quipWheel");
        var selectedQuipIndexField = AccessTools.Field(typeof(Highlighter), "selectedQuipIndex");
        var quipLabelField = AccessTools.Field(typeof(Highlighter), "quipLabel");
        var quipIconsField = AccessTools.Field(typeof(Highlighter), "quipIcons");
        var quipSelectorField = AccessTools.Field(typeof(Highlighter), "quipSelector");
        var quipSelectedColorField = AccessTools.Field(typeof(Highlighter), "quipSelectedColor");
        var quipUnselectedColorField = AccessTools.Field(typeof(Highlighter), "quipUnselectedColor");
        var quipSelectedScaleField = AccessTools.Field(typeof(Highlighter), "quipSelectedScale");
        
        var enableQuipWheel = (bool)(enableQuipWheelField?.GetValue(highlighterInstance) ?? false);
        var enableQuipWheelTime = (float)(enableQuipWheelTimeField?.GetValue(highlighterInstance) ?? 0f);
        var timeBeforeActivatingWheel = (float)(timeBeforeActivatingWheelField?.GetValue(highlighterInstance) ?? 0.1f);
        var quipWheel = quipWheelField?.GetValue(highlighterInstance) as GameObject;
        var selectedQuipIndex = (int)(selectedQuipIndexField?.GetValue(highlighterInstance) ?? 0);
        var quipLabel = quipLabelField?.GetValue(highlighterInstance) as TextMeshProUGUI;
        var quipIcons = quipIconsField?.GetValue(highlighterInstance) as List<Image>;
        var quipSelector = quipSelectorField?.GetValue(highlighterInstance) as Image;
        var quipSelectedColor = (Color)(quipSelectedColorField?.GetValue(highlighterInstance) ?? Color.white);
        var quipUnselectedColor = (Color)(quipUnselectedColorField?.GetValue(highlighterInstance) ?? Color.gray);
        var quipSelectedScale = (float)(quipSelectedScaleField?.GetValue(highlighterInstance) ?? 1.2f);
        
        if (!enableQuipWheel || Time.unscaledTime - enableQuipWheelTime < timeBeforeActivatingWheel)
            return;
            
        if (!quipWheel.activeSelf)
        {
            PlayerInput.EnableMenu();
            quipWheel.SetActive(true);
            ++PlayerLook.Instance.RotationLocksX;
            ++PlayerLook.Instance.RotationLocksY;
            Player.LocalPlayer.LockFiring(true);
            
            // Set initial loadout label
            if (_currentLoadouts.Count > 0 && quipLabel != null)
            {
                quipLabel.text = _currentLoadouts[selectedQuipIndex].Label;
            }
        }
        
        // Handle input for selection (similar to original but using our loadouts)
        Vector2 vector2 = PlayerInput.Controls.Menu.Point.ReadValue<Vector2>();
        vector2.x -= (float)Screen.width * 0.5f;
        vector2.y -= (float)Screen.height * 0.5f;
        
        if (_currentLoadouts.Count > 0)
        {
            float angleStep = 360f / (float)_currentLoadouts.Count;
            float current = Mathf.Atan2(vector2.y, vector2.x) * 57.29578f;
            
            if (current < 0.0)
                current += 360f;
            else if (current >= 360.0)
                current -= 360f;
                
            int newIndex = 0;
            float minAngleDiff = float.MaxValue;
            
            for (int i = 0; i < _currentLoadouts.Count; ++i)
            {
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(current, (float)(i * angleStep + angleStep * 0.5)));
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
                for (int i = 0; i < Mathf.Min(quipIcons.Count, _currentLoadouts.Count); ++i)
                {
                    quipIcons[i].color = i == newIndex ? quipSelectedColor : quipUnselectedColor;
                    quipIcons[i].rectTransform.localScale = i == newIndex ? 
                        new Vector3(quipSelectedScale, quipSelectedScale, quipSelectedScale) : Vector3.one;
                }
            }
            
            // Update label if selection changed
            if (selectedQuipIndex != newIndex)
            {
                selectedQuipIndexField?.SetValue(highlighterInstance, newIndex);
                if (quipLabel != null)
                {
                    quipLabel.text = _currentLoadouts[newIndex].Label;
                }
            }
        }
    }
    
    private static void WeaponLoadoutWheel(bool performed)
    {
        var highlighterInstance = UnityEngine.Object.FindObjectOfType<Highlighter>();
        if (highlighterInstance == null) return;

        if (performed)
        {
            _isWeaponWheelMode = true;
            
            var enableQuipWheelField = AccessTools.Field(typeof(Highlighter), "enableQuipWheel");
            var enableQuipWheelTimeField = AccessTools.Field(typeof(Highlighter), "enableQuipWheelTime");
            var selectedQuipIndexField = AccessTools.Field(typeof(Highlighter), "selectedQuipIndex");
            
            enableQuipWheelField?.SetValue(highlighterInstance, true);
            enableQuipWheelTimeField?.SetValue(highlighterInstance, Time.unscaledTime);
            selectedQuipIndexField?.SetValue(highlighterInstance, 0);
            
            BasePlugin.Logger.LogInfo("Enabling weapon loadout wheel");
            
            var weapon = PlayerData.GetGearData(Player.LocalPlayer.SelectedGear);
            var fieldRefLoadouts = AccessTools.FieldRefAccess<PlayerData.GearData, object>("loadouts");

            if (fieldRefLoadouts(weapon) is not Array { Length: > 0 } loadouts) return;
            
            var loadoutDataList = new List<LoadoutData>();
                
            for (var i = 0; i < loadouts.Length; i++)
            {
                var loadoutData = new LoadoutData
                {
                    Label = $"Loadout {i + 1}",
                    Icon = weapon.GetLoadoutIcon(i),
                    GearData = weapon,
                    LoadoutIndex = i
                };
                loadoutDataList.Add(loadoutData);
            }
                
            // Call our custom setup method
            SetupLoadouts(highlighterInstance, loadoutDataList);
                
            BasePlugin.Logger.LogInfo($"Setup weapon loadout wheel with {loadouts.Length} loadouts");
        }
        else
        {
            var quipWheelField = AccessTools.Field(typeof(Highlighter), "quipWheel");
            var selectedQuipIndexField = AccessTools.Field(typeof(Highlighter), "selectedQuipIndex");
            var quipWheel = quipWheelField?.GetValue(highlighterInstance) as GameObject;
            var selectedIndex = (int)(selectedQuipIndexField?.GetValue(highlighterInstance) ?? 0);
            
            if (quipWheel != null && quipWheel.activeSelf)
            {
                PlayerInput.DisableMenu();
                quipWheel.SetActive(false);
                --PlayerLook.Instance.RotationLocksX;
                --PlayerLook.Instance.RotationLocksY;
                Player.LocalPlayer.LockFiring(false);
            }
            
            var enableQuipWheelField = AccessTools.Field(typeof(Highlighter), "enableQuipWheel");
            enableQuipWheelField?.SetValue(highlighterInstance, false);
            
            _isWeaponWheelMode = false;
            
            BasePlugin.Logger.LogInfo($"Applying weapon loadout at index: {selectedIndex}");
            
            // Apply the selected loadout using our stored data
            ApplySelectedLoadout(selectedIndex);
        }
    }
    
    private static readonly List<LoadoutData> _currentLoadouts = [];
    
    private static void SetupLoadouts(Highlighter highlighterInstance, List<LoadoutData> loadouts)
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
        
        if (quipIcons == null || quipWheel == null) return;
        
        var angleStep = 360f / loadouts.Count;
        
        for (var i = 0; i < loadouts.Count; i++)
        {
            // Create new icon if needed
            if (i >= quipIcons.Count)
            {
                GameObject iconObject = new GameObject(loadouts[i].Label);
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
        
        // Hide unused icons
        for (var i = loadouts.Count; i < quipIcons.Count; i++)
        {
            if (quipIcons[i].gameObject.activeSelf)
                quipIcons[i].gameObject.SetActive(false);
        }
        
        // Update selector
        if (quipSelector != null)
        {
            quipSelector.fillAmount = angleStep / 360f;
        }
    }
    
    private static void ApplySelectedLoadout(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= _currentLoadouts.Count) return;
        
        var selectedLoadout = _currentLoadouts[selectedIndex];
        selectedLoadout.GearData.EquipLoadout(selectedLoadout.LoadoutIndex);
        BasePlugin.Logger.LogInfo($"Applied {selectedLoadout.Label} for gear");
    }
}