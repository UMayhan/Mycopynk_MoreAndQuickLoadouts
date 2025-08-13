using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Pigeon.UI;
using UnityEngine;

namespace MoreAndQuickLoadouts;

public class LoadoutInjector : MonoBehaviour
{
    public GearDetailsWindow target;
    public Transform targetTransform => target.transform;

    public int targetCount;
    private int spacingY => 52;

    public static LoadoutInjector Instance { get; private set; }
    public SelectIconWindow selectIconWindow;

    public void Awake()
    {
        Instance = this;
    }
    
    public void Setup()
    {
        SelectIconSetup();
        InjectLoadouts();
    }

    private void SelectIconSetup()
    {
        var infoWindow = Instantiate(Global.Instance.InfoWindow, target.transform);
        var infoWindowGO = infoWindow.gameObject;
        var comp = infoWindowGO.GetComponent<InfoWindow>();
        if(comp != null) DestroyImmediate(comp);
        selectIconWindow = infoWindowGO.AddComponent<SelectIconWindow>();
        selectIconWindow.gameObject.SetActive(false);
        selectIconWindow.Setup(target);
    }

    private void InjectLoadouts()
    {

        Transform lastIconSwitch = null;
        
        // var originalButtons = AccessTools.FieldRefAccess<GearDetailsWindow, LoadoutHoverInfo[]>("loadoutButtons");
        // var buttons = originalButtons(target);
        var buttons = target.loadoutButtons;
        var existingCount = buttons.Length;
        if (targetCount <= existingCount) return;
        
        // Create a new array with the target size
        var newButtons = new LoadoutHoverInfo[targetCount];
        
        // Copy existing buttons to the new array
        for (var j = 0; j < existingCount && j < targetCount; j++)
        {
            newButtons[j] = buttons[j];
        }
        
        var lastLoadout = buttons.Length > 0 ? buttons[^1].transform : null;
        
        for (var i = targetTransform.childCount - 1; i >= 0; i--)
        {
            var child = targetTransform.GetChild(i);
            
            if (lastIconSwitch == null && child.name.StartsWith("IconB"))
                lastIconSwitch = child;
        }

        var originalIncrementLoadoutIcon = targetTransform.GetComponentsInChildren<Button>().Where(b => b.name.Contains("IconB")).ToList();

        for (var i = 0; i < originalIncrementLoadoutIcon.Count; i++)
        {
            var increment = originalIncrementLoadoutIcon[i];
            var persistentEventCount = increment.OnClickUp.GetPersistentEventCount();

            increment.OnClickUp.RemoveAllListeners();

            for (var j = persistentEventCount - 1; j >= 0; j--)
            {
                _ = increment.OnClickUp.GetPersistentTarget(j)?.name ?? "null";
                increment.OnClickUp.GetPersistentMethodName(j);
                increment.OnClickUp.SetPersistentListenerState(j, UnityEngine.Events.UnityEventCallState.Off);
            }

            var i1 = i;
            increment.OnClickUp.AddListener(() =>
            {
                selectIconWindow.OnOpen(buttons[i1]);
            });

            increment.GetComponent<HoverInfoTextBinding>().primaryLabel = "Select Icon";
        }
        
        
        if (lastLoadout == null || lastIconSwitch == null)
        {
            return;
        }

        
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
                
                // Remove all listeners (both runtime and persistent)
                iconBtn.OnClickUp.RemoveAllListeners();
                
                // Alternative approach if the above doesn't work:
                // Get the persistent event count and remove them individually
                for (var j = persistentEventCount - 1; j >= 0; j--)
                {
                    _ = iconBtn.OnClickUp.GetPersistentTarget(j)?.name ?? "null";
                    iconBtn.OnClickUp.GetPersistentMethodName(j);
                    iconBtn.OnClickUp.SetPersistentListenerState(j, UnityEngine.Events.UnityEventCallState.Off);
                }
                
                iconBtn.OnClickUp.AddListener(() =>
                {
                    selectIconWindow.OnOpen(loadoutHoverInfo);
                });
                
                iconBtn.GetComponent<HoverInfoTextBinding>().primaryLabel = "Select Icon";
            }

            newButtons[i] = loadoutHoverInfo;
        }
        
        target.loadoutButtons = newButtons;
        
    }
}