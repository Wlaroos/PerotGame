using System.Collections;
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
    _uiCanvas = GetAnyCanvas();
        if (_uiCanvas == null)
            Debug.LogWarning("CraftedPopupManager: No Canvas found in scene. Popups will not be visible until a Canvas exists.");
    }

    // Show a popup for a mineral at world position
    public void ShowCraftedPopup(MineralData data, Vector3 worldPosition)
    {
        if (data == null) return;
        if (popupPrefab == null)
        {
            Debug.LogWarning("CraftedPopupManager: popupPrefab not assigned.");
            return;
        }

        if (_uiCanvas == null)
        {
            _uiCanvas = GetAnyCanvas();
            if (_uiCanvas == null)
            {
                Debug.LogWarning("CraftedPopupManager: No Canvas in scene to show popup.");
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

        if (title != null)
        {
            // Display a tidy name (remove prefix before underscore if present)
            string temp = data.mineralName ?? data.name;
            int idx = temp.IndexOf('_');
            if (idx >= 0 && idx + 1 < temp.Length) temp = temp.Substring(idx + 1);
            title.text = temp;
            title.color = data.defaultColor;
        }

        // Ensure a CanvasGroup exists for fade/alpha animation
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        // start animation coroutine
        StartCoroutine(AnimateAndDestroy(go, cg, popupDuration));
    }

    private Canvas GetAnyCanvas()
    {
        // Prefer active scene canvases
    var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
    if (canvases != null && canvases.Length > 0) return canvases[0];
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
