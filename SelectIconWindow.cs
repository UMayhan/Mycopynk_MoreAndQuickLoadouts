using System;
using Pigeon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace MoreAndQuickLoadouts;

public class SelectIconWindow : MonoBehaviour
{
    private static Sprite[] Icons => Global.Instance.LoadoutIcons;
    public GearDetailsWindow Target { get; set; }
    private LoadoutHoverInfo _currentLoadout;
    
    
    public void Setup(GearDetailsWindow target)
    {
        BasePlugin.Logger.LogInfo("SelectIconWindow.Setup()");
        Target = target;
        var titleGO = transform.Find("Title");
        var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
        titleTMP.text = "Select Loadout Icon";
        titleTMP.fontSize = 32;
        
        var titleRectTransform = (RectTransform)titleGO.transform;
        titleRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,64);
        titleRectTransform.anchoredPosition = new Vector2(titleRectTransform.anchoredPosition.x, -48);
            
        var desc = transform.Find("Desc");
        DestroyImmediate(desc.gameObject.GetComponent<TextMeshPro>());
        
        // Ensure the parent container has proper RectTransform setup
        var parentRect = desc.GetComponent<RectTransform>();
        if (parentRect == null)
            parentRect = desc.gameObject.AddComponent<RectTransform>();

        // Set anchors to stretch on all axes (full stretch)
        parentRect.anchorMin = new Vector2(0f, 0f);
        parentRect.anchorMax = new Vector2(1f, 1f);

        // Set margins: left=20, right=20, bottom=20, top=100 (48+32+20)
        parentRect.offsetMin = new Vector2(20f, 20f);     // left and bottom margins
        parentRect.offsetMax = new Vector2(-20f, -100f);  // right and top margins (negative values)
        
        // Set up the grid layout
        var grid = desc.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(72, 72);
        grid.spacing = new Vector2(16, 16);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 14;
        // Add ContentSizeFitter to handle automatic sizing
        var contentSizeFitter = desc.gameObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // Create icon buttons
        for (var i = 0; i < Icons.Length; i++)
        {
            var sprite = Icons[i];
            var go = new GameObject($"Icon_{i}");
            go.transform.SetParent(grid.transform, false); // Important: worldPositionStays = false
            go.gameObject.SetActive(sprite != null);
            // Set up RectTransform properly for grid children
            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var i1 = i;
            button.onClick.AddListener(() => SetIcon(i1));
        }
    
        // Force layout rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    
        transform.Find("Close")?.GetComponent<DefaultButton>().OnClickUp.AddListener(OnClose);
        transform.Find("Ok").gameObject.SetActive(false);
}

    private void SetIcon(int i)
    {
        var index = Array.IndexOf(Target.loadoutButtons, _currentLoadout);
        var gearData = PlayerData.GetGearData(Target.UpgradablePrefab);
        gearData.loadouts[index].iconIndex = i;
        Target.UpdateLoadoutIcon(_currentLoadout, index);
        OnClose();
    }

    public void OnOpen(LoadoutHoverInfo currentLoadout)
    {
        BasePlugin.Logger.LogInfo("SelectIconWindow.OnOpen()");
        _currentLoadout = currentLoadout;
        gameObject.SetActive(true);
    }

    public void OnClose()
    {
        BasePlugin.Logger.LogInfo("SelectIconWindow.OnClose()");
        _currentLoadout = null;
        gameObject.SetActive(false);
    }
}