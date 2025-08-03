using HarmonyLib;
using Pigeon.UI;
using UnityEngine;

namespace MoreAndQuickLoadouts;

public class LoadoutInjector : MonoBehaviour
{
    public GearDetailsWindow target;
    public Transform targetTransform => target.transform;

    public int targetCount = 5;
    private int spacingY => 52;
    
    public void InjectLoadouts()
    {
        // BasePlugin.Logger.LogInfo("InjectLoadouts");

        Transform lastIconSwitch = null;
        
        var originalButtons = AccessTools.FieldRefAccess<GearDetailsWindow, LoadoutHoverInfo[]>("loadoutButtons");
        var buttons = originalButtons(target);
        var existingCount = buttons.Length;
        
        // Create a new array with the target size
        var newButtons = new LoadoutHoverInfo[targetCount];
        
        // Copy existing buttons to the new array
        for (var j = 0; j < existingCount && j < targetCount; j++)
        {
            newButtons[j] = buttons[j];
        }
        
        var lastLoadout = buttons.Length > 0 ? buttons[buttons.Length - 1].transform : null;
        
        for (var i = targetTransform.childCount - 1; i >= 0; i--)
        {
            var child = targetTransform.GetChild(i);
            
            if (lastIconSwitch == null && child.name.StartsWith("IconB"))
                lastIconSwitch = child;
        }
        //last icon onMouseUp event
        
        if (lastLoadout == null || lastIconSwitch == null)
        {
            // Debug.LogError("[Mod] Could not find original Loadout or IconB to clone.");
            return;
        }

        // Debug.Log($"[Mod] Found {existingCount} loadouts. Expanding to {targetCount}.");
        
        for (var i = existingCount; i < targetCount; i++)
        {
            var loadoutName = $"Loadout{(char)('A' + i)}";
            var iconName = $"IconB_{(char)('A' + i)}";

            var loadout = Instantiate(lastLoadout.gameObject, targetTransform);
            loadout.name = loadoutName;
            var loadoutRT = loadout.GetComponent<RectTransform>();
            loadoutRT.anchoredPosition = lastLoadout.GetComponent<RectTransform>().anchoredPosition + new Vector2(0, -spacingY * (i - existingCount + 1));
            var loadoutHoverInfo = loadout.GetComponent<LoadoutHoverInfo>();
            
            // Clone icon
            var icon = Instantiate(lastIconSwitch.gameObject, targetTransform);
            icon.name = iconName;
            var iconRT = icon.GetComponent<RectTransform>();
            iconRT.anchoredPosition = lastIconSwitch.GetComponent<RectTransform>().anchoredPosition + new Vector2(0, -spacingY * (i - existingCount + 1));

            // Add listener
            var iconBtn = icon.GetComponent<Button>();
            if (iconBtn != null)
            {
                // Log current listener count before removal
                var persistentEventCount = iconBtn.OnClickUp.GetPersistentEventCount();
                // BasePlugin.Logger.LogInfo($"Removing listeners from {iconName}. Persistent events: {persistentEventCount}");
                
                // Remove all listeners (both runtime and persistent)
                iconBtn.OnClickUp.RemoveAllListeners();
                
                // Alternative approach if the above doesn't work:
                // Get the persistent event count and remove them individually
                for (int j = persistentEventCount - 1; j >= 0; j--)
                {
                    var targetName = iconBtn.OnClickUp.GetPersistentTarget(j)?.name ?? "null";
                    var methodName = iconBtn.OnClickUp.GetPersistentMethodName(j);
                    // BasePlugin.Logger.LogInfo($"Disabling persistent listener {j}: Target={targetName}, Method={methodName}");
                    iconBtn.OnClickUp.SetPersistentListenerState(j, UnityEngine.Events.UnityEventCallState.Off);
                }
                
                // BasePlugin.Logger.LogInfo($"Adding new listener for {iconName}");
                iconBtn.OnClickUp.AddListener(() =>
                {
                    target.IncrementLoadoutIcon(loadoutHoverInfo);
                });
            }

            newButtons[i] = loadoutHoverInfo;
        }
        
        // Update the field with the new array
        var loadoutButtonsField = AccessTools.Field(typeof(GearDetailsWindow), "loadoutButtons");
        loadoutButtonsField.SetValue(target, newButtons);
        
        // BasePlugin.Logger.LogInfo("InjectLoadouts done.");
    }
}