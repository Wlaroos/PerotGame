using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shows a small animated UI popup when something is crafted (e.g. a new mineral)
public class CraftedPopupManager : MonoBehaviour
{
    public static CraftedPopupManager Instance { get; private set; }

    [Header("Prefab & Timing")] 
    [Tooltip("A UI prefab that contains an Image named 'icon' and a TextMeshProUGUI named 'titleText'. It should be a simple root GameObject (no required components).")]
    [SerializeField] private GameObject popupPrefab;
    [Tooltip("How long the popup remains visible (seconds)")]
    [SerializeField] private float popupDuration = 1.6f;
    [Tooltip("Vertical offset in pixels the popup will move up during the animation")]
    [SerializeField] private float moveUp = 40f;

    [Header("Debug / Test")]
    [Tooltip("If true, popups will be shown for every mineral craft")]
    public bool debugAlwaysShow = false;

    // new serialized fields near the top
    [SerializeField] private Canvas overrideCanvas;      // assign the correct Canvas in Inspector

    [Tooltip("A fullscreen/persistent popup prefab that stays until clicked. If assigned, use this for first-time big popups.")]
    [SerializeField] private GameObject persistentPopupPrefab;

    private Canvas _uiCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        // Find a Canvas to parent popups to. Prefer the first active one in the scene.
        _uiCanvas = overrideCanvas != null ? overrideCanvas : GetAnyCanvas();
        if (_uiCanvas == null)
            Debug.LogWarning("CraftedPopupManager: No Canvas found in scene. Popups will not be visible until a Canvas exists.");
    }

    // Show a popup for a mineral at world position
    public void ShowCraftedPopup(MineralData data, Vector3 worldPosition)
    {
        Debug.Log($"CraftedPopupManager: ShowCraftedPopup called for '{(data!=null?data.name:"<null>")}' at {worldPosition}");
        if (data == null) return;
        if (popupPrefab == null)
        {
            Debug.LogWarning("CraftedPopupManager: popupPrefab not assigned — cannot show popup.");
            Debug.LogWarning("CraftedPopupManager: popupPrefab not assigned.");
            return;
        }

        if (_uiCanvas == null)
        {
            _uiCanvas = GetAnyCanvas();
            if (_uiCanvas == null)
            {
                Debug.LogWarning("CraftedPopupManager: No Canvas in scene to show popup. Ensure a Canvas exists or assign one to the manager.");
                return;
            }
        }

        // Instantiate under the UI canvas
        var go = Instantiate(popupPrefab, _uiCanvas.transform, false);

        // Position it near the world point by converting to screen space
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            // Convert screen point to canvas local point
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_uiCanvas.transform as RectTransform, screenPos, _uiCanvas.worldCamera, out localPoint);
            rt.anchoredPosition = localPoint;
        }

        // Find icon and title inside prefab (case-insensitive search)
        Image icon = FindChildComponentByName<Image>(go, "icon");
        TextMeshProUGUI title = FindChildComponentByName<TextMeshProUGUI>(go, "titleText");

        if (icon != null)
        {
            icon.sprite = data.mineralBigSprite != null ? data.mineralBigSprite : data.mineralSprite;
            icon.color = data.defaultColor;
        }

        if (icon == null)
        {
            Debug.LogWarning("CraftedPopupManager: popup prefab does not contain an Image child named 'icon' (or similar). The icon will be missing.");
        }

        if (title != null)
        {
            // Display a tidy name (remove prefix before underscore if present)
            string temp = data.mineralName ?? data.name;
            int idx = temp.IndexOf('_');
            if (idx >= 0 && idx + 1 < temp.Length) temp = temp.Substring(idx + 1);
            title.text = temp;
            title.color = data.defaultColor;
        }
        else
        {
            Debug.LogWarning("CraftedPopupManager: popup prefab does not contain a TextMeshProUGUI child named 'titleText' (or similar). The title will be missing.");
        }

        // Ensure a CanvasGroup exists for fade/alpha animation
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        // start animation coroutine
        StartCoroutine(AnimateAndDestroy(go, cg, popupDuration));
    }

        // Show a fullscreen/persistent popup that stays until the player clicks to dismiss
        public void ShowPersistentCraftedPopup(MineralData data)
        {
            Debug.Log($"CraftedPopupManager: ShowPersistentCraftedPopup called for '{(data!=null?data.name:"<null>")}'");
            if (data == null) return;
            if (_uiCanvas == null)
            {
                _uiCanvas = GetAnyCanvas();
                if (_uiCanvas == null)
                {
                    Debug.LogWarning("CraftedPopupManager: No Canvas in scene to show persistent popup.");
                    return;
                }
            }

            if (persistentPopupPrefab == null)
            {
                Debug.LogWarning("CraftedPopupManager: persistentPopupPrefab not assigned — falling back to transient popup.");
                // fallback to the small popup (center of screen)
                Vector3 centerWorld = Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 2f : Vector3.zero;
                ShowCraftedPopup(data, centerWorld);
                return;
            }

            var go = Instantiate(persistentPopupPrefab, _uiCanvas.transform, false);

            // Try to populate icon/title similarly to the transient popup
            Image icon = FindChildComponentByName<Image>(go, "icon");
            TextMeshProUGUI title = FindChildComponentByName<TextMeshProUGUI>(go, "titleText");

            if (icon != null)
            {
                icon.sprite = data.mineralBigSprite != null ? data.mineralBigSprite : data.mineralSprite;
                icon.color = data.defaultColor;
            }

            if (title != null)
            {
                string temp = data.mineralName ?? data.name;
                int idx = temp.IndexOf('_');
                if (idx >= 0 && idx + 1 < temp.Length) temp = temp.Substring(idx + 1);
                title.text = temp;
                title.color = data.defaultColor;
            }

            // Ensure it blocks raycasts
            var bgCg = go.GetComponent<CanvasGroup>();
            if (bgCg == null) bgCg = go.AddComponent<CanvasGroup>();
            bgCg.blocksRaycasts = true;

            // Wire any existing buttons in the prefab to dismiss this specific instance
            var existingButtons = go.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var b in existingButtons)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => { if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go); });
            }

            // Create a full-screen transparent overlay that captures clicks anywhere and dismisses the popup
            var overlay = new GameObject("DismissOverlay");
            overlay.transform.SetParent(go.transform, false);
            var rtOverlay = overlay.AddComponent<RectTransform>();
            rtOverlay.anchorMin = Vector2.zero;
            rtOverlay.anchorMax = Vector2.one;
            rtOverlay.offsetMin = Vector2.zero;
            rtOverlay.offsetMax = Vector2.zero;
            var overlayImg = overlay.AddComponent<Image>();
            // Transparent but still receives raycasts
            overlayImg.color = new Color(0f, 0f, 0f, 0f);
            overlayImg.raycastTarget = true;
            var overlayBtn = overlay.AddComponent<UnityEngine.UI.Button>();
            overlayBtn.onClick.AddListener(() => { if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go); });
            // Ensure overlay is on top so any click dismisses the popup
            overlay.transform.SetAsLastSibling();
        }

    private Canvas GetAnyCanvas()
    {
        if (overrideCanvas != null) return overrideCanvas; // prefer manual assignment

        // Prefer active scene canvases
        // Use Resources.FindObjectsOfTypeAll and filter to active canvases so this works in
        // a wide range of editor/runtime setups without calling obsolete APIs.
        var canvases = Resources.FindObjectsOfTypeAll<Canvas>()
            .Where(c => c != null && c.isActiveAndEnabled)
            .ToArray();

        if (canvases != null && canvases.Length > 0)
        {
            // Prefer an active Screen Space - Camera canvas that uses the main camera
            var preferred = canvases.FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera == Camera.main);
            if (preferred != null) return preferred;

            // Prefer any active Screen Space - Camera canvas
            preferred = canvases.FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceCamera);
            if (preferred != null) return preferred;

            // Otherwise return any active canvas
            return canvases[0];
        }

        return null;
    }

    private IEnumerator AnimateAndDestroy(GameObject go, CanvasGroup cg, float duration)
    {
        float elapsed = 0f;
        var rt = go.GetComponent<RectTransform>();
        Vector3 startScale = Vector3.one * 0.6f;
        Vector3 midScale = Vector3.one * 1.05f;
        Vector3 endScale = Vector3.one * 1f;
        if (rt != null) rt.localScale = startScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // scale: quick pop then settle
            if (rt != null)
            {
                if (t < 0.35f)
                    rt.localScale = Vector3.Lerp(startScale, midScale, t / 0.35f);
                else
                    rt.localScale = Vector3.Lerp(midScale, endScale, (t - 0.35f) / (1f - 0.35f));
            }

            // fade out in the final third
            if (cg != null)
            {
                float alpha = (t < 0.66f) ? 1f : Mathf.Lerp(1f, 0f, (t - 0.66f) / (1f - 0.66f));
                cg.alpha = alpha;
            }

            // move up slowly
            if (rt != null)
            {
                rt.anchoredPosition += new Vector2(0f, (moveUp / duration) * Time.deltaTime);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(go);
    }

    // Utility: search for a component of type T on children where the GameObject name contains the provided name
    private T FindChildComponentByName<T>(GameObject root, string childName) where T : Component
    {
        if (root == null) return null;
        var comps = root.GetComponentsInChildren<T>(true);
        foreach (var c in comps)
        {
            if (c == null || c.gameObject == null) continue;
            if (c.gameObject.name.ToLower().Contains(childName.ToLower())) return c;
        }
        return null;
    }
}
