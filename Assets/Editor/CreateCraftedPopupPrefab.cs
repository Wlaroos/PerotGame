using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UI;
using TMPro;

// Editor helper: creates a sample crafted popup prefab with the expected structure
public static class CreateCraftedPopupPrefab
{
    [MenuItem("Tools/Create Crafted Popup Prefab")]
    public static void CreatePrefab()
    {
        // Ensure Prefabs folder exists
        string prefabsPath = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabsPath))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // Root GameObject for the popup prefab
        GameObject root = new GameObject("CraftedPopup");
        RectTransform rtRoot = root.AddComponent<RectTransform>();
        // Default size
        rtRoot.sizeDelta = new Vector2(220, 80);

        // CanvasGroup for fade animations
        root.AddComponent<CanvasGroup>();

        // Create Icon child (Image)
        GameObject iconGO = new GameObject("icon");
        iconGO.transform.SetParent(root.transform, false);
        RectTransform rtIcon = iconGO.AddComponent<RectTransform>();
        rtIcon.anchorMin = new Vector2(0f, 0.5f);
        rtIcon.anchorMax = new Vector2(0f, 0.5f);
        rtIcon.pivot = new Vector2(0.5f, 0.5f);
        rtIcon.anchoredPosition = new Vector2(40, 0);
        rtIcon.sizeDelta = new Vector2(64, 64);
        var img = iconGO.AddComponent<Image>();
        img.color = Color.white;

        // Create TitleText child (TextMeshProUGUI)
        GameObject titleGO = new GameObject("titleText");
        titleGO.transform.SetParent(root.transform, false);
        RectTransform rtTitle = titleGO.AddComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0f, 0f);
        rtTitle.anchorMax = new Vector2(1f, 1f);
        rtTitle.offsetMin = new Vector2(100, 10);
        rtTitle.offsetMax = new Vector2(-10, -10);
        var tmp = titleGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "New Mineral";
        tmp.fontSize = 22;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;

        // Optionally add a background image for the root (transparent default)
        GameObject bgGO = new GameObject("background");
        bgGO.transform.SetParent(root.transform, false);
        var rtBg = bgGO.AddComponent<RectTransform>();
        rtBg.anchorMin = new Vector2(0f, 0f);
        rtBg.anchorMax = new Vector2(1f, 1f);
        rtBg.offsetMin = Vector2.zero;
        rtBg.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.35f);
        // Send background to back
        bgGO.transform.SetAsFirstSibling();

        // Save as prefab
        string prefabPath = prefabsPath + "/CraftedPopup.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
        if (success)
        {
            Debug.Log($"Created crafted popup prefab at {prefabPath}");
        }
        else
        {
            Debug.LogError("Failed to create crafted popup prefab.");
        }

        // Clean up the temporary root object in the scene
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
    }
}
#endif
