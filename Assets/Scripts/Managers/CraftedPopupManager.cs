using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    [Tooltip("A fullscreen/persistent popup prefab that stays until clicked. If assigned, use this for first-time big popups.")]
    [SerializeField] private GameObject persistentPopupPrefab;
    [Tooltip("How long the popup remains visible (seconds)")]
    [SerializeField] private float popupDuration = 1.6f;
    [Tooltip("Vertical offset in pixels the popup will move up during the animation")]
    [SerializeField] private float moveUp = 40f;
    [SerializeField] private Canvas overrideCanvas;      // assign the correct Canvas in Inspector


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
    // Optional: pass the CraftingRecipe that produced the mineral so we can build a compact formula string
    public void ShowCraftedPopup(MineralData data, Vector3 worldPosition, CraftingRecipe recipe = null)
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
    // Give transient popups their own Canvas with override sorting so they appear above world sprites
    var transientCanvas = go.GetComponent<Canvas>();
    if (transientCanvas == null) transientCanvas = go.AddComponent<Canvas>();
    transientCanvas.overrideSorting = true;
    transientCanvas.sortingOrder = 500;
    if (go.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null) go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

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

            // If we have a recipe, build a compact formula and append in parentheses
            if (recipe != null)
            {
                // Use the title's font to decide whether to use Unicode subscripts or TMP rich-text fallback
                string formula = BuildFormulaForTitle(title, recipe);
                if (!string.IsNullOrEmpty(formula)) temp = string.Format("{0} ({1})", temp, formula);
            }

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
    public void ShowPersistentCraftedPopup(MineralData data, CraftingRecipe recipe = null)
        {
            //Debug.Log($"CraftedPopupManager: ShowPersistentCraftedPopup called for '{(data!=null?data.name:"<null>")}'");
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
                ShowCraftedPopup(data, centerWorld, recipe);
                return;
            }

            var go = Instantiate(persistentPopupPrefab, _uiCanvas.transform, false);
            // Ensure this popup draws above world sprites by giving it its own Canvas with overrideSorting
            var popupCanvas = go.GetComponent<Canvas>();
            if (popupCanvas == null) popupCanvas = go.AddComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 1000;
            if (go.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null) go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Try to populate icon/title similarly to the transient popup
            Image icon = FindChildComponentByName<Image>(go, "icon");
            TextMeshProUGUI title = FindChildComponentByName<TextMeshProUGUI>(go, "titleText");

            if (icon != null)
            {
                icon.sprite = data.mineralBigSprite != null ? data.mineralBigSprite : data.mineralSprite;
                //icon.color = data.defaultColor;
            }

            if (title != null)
            {
                string temp = data.mineralName ?? data.name;
                int idx = temp.IndexOf('_');
                if (idx >= 0 && idx + 1 < temp.Length) temp = temp.Substring(idx + 1);

                if (recipe != null)
                {
                    string formula = BuildFormulaForTitle(title, recipe);
                    if (!string.IsNullOrEmpty(formula)) temp = string.Format("{0} ({1})", temp, formula);
                }

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

    // Build a compact formula string (e.g. 2Al3CO3) from a recipe's ingredients
    private string BuildFormulaFromRecipe(CraftingRecipe recipe)
    {
        if (recipe == null) return string.Empty;

        var ingredients = new List<ScriptableObject> { recipe.inputA, recipe.inputB, recipe.inputC, recipe.inputD, recipe.inputE }
            .Where(x => x != null)
            .ToList();

        if (ingredients.Count == 0) return string.Empty;

        // Map ingredients to their symbol strings
        var symbols = ingredients.Select(i => GetSymbolForScriptableObject(i)).ToList();

        // Preserve first-seen order for symbols
        var ordered = new List<string>();
        foreach (var s in symbols) if (!ordered.Contains(s)) ordered.Add(s);

        var counts = symbols.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

        var sb = new StringBuilder();
        foreach (var s in ordered)
        {
            int c = counts.TryGetValue(s, out var v) ? v : 0;
            if (c <= 1)
            {
                sb.Append(s);
            }
            else
            {
                // If the symbol itself contains digits (e.g. CO3), treat it as a polyatomic group and parenthesize
                bool hasDigit = s.Any(ch => char.IsDigit(ch));
                if (hasDigit)
                {
                    sb.Append('(').Append(s).Append(')');
                }
                else
                {
                    sb.Append(s);
                }

                sb.Append(ToSubscript(c));
            }
        }

        return sb.ToString();
    }

    // Build a formula string appropriate for the provided TextMeshProUGUI title.
    // If the title's font asset contains Unicode subscript digits, use them; otherwise produce TMP rich-text subscripts.
    private string BuildFormulaForTitle(TextMeshProUGUI title, CraftingRecipe recipe)
    {
        if (recipe == null) return string.Empty;

        bool useUnicodeSubscripts = false;
        if (title != null && title.font != null)
        {
            // Check for at least one subscript digit glyph in the font asset
            try
            {
                var fontAsset = title.font;
                useUnicodeSubscripts = fontAsset.HasCharacter('\u2081');
            }
            catch
            {
                useUnicodeSubscripts = false;
            }
        }

        // Build the basic ordered symbols and counts (preserve first-seen order)
        var ingredients = new List<ScriptableObject> { recipe.inputA, recipe.inputB, recipe.inputC, recipe.inputD, recipe.inputE }
            .Where(x => x != null)
            .ToList();
        if (ingredients.Count == 0) return string.Empty;

        var symbols = ingredients.Select(i => GetSymbolForScriptableObject(i)).ToList();
        var ordered = new List<string>();
        foreach (var s in symbols) if (!ordered.Contains(s)) ordered.Add(s);
        var counts = symbols.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

        var sb = new StringBuilder();
        foreach (var s in ordered)
        {
            int c = counts.TryGetValue(s, out var v) ? v : 0;
            if (c <= 1)
            {
                sb.Append(s);
            }
            else
            {
                bool hasDigit = s.Any(ch => char.IsDigit(ch));
                if (useUnicodeSubscripts)
                {
                    if (hasDigit)
                        sb.Append('(').Append(s).Append(')');
                    else
                        sb.Append(s);

                    sb.Append(ToSubscript(c));
                }
                else
                {
                    // Rich-text fallback using TMP tags to simulate subscripts
                    if (hasDigit)
                        sb.Append('(').Append(s).Append(')');
                    else
                        sb.Append(s);

                    sb.Append(MakeSubscriptTMP(c.ToString()));
                }
            }
        }

        // Ensure rich-text is enabled if we emitted TMP tags
        if (!useUnicodeSubscripts && title != null)
        {
            title.richText = true;
        }

        return sb.ToString();
    }

    // Create a TMP rich-text fragment that uses TextMeshPro's <sub> tag for subscripts
    private string MakeSubscriptTMP(string number)
    {
        // <sub> is a built-in TMP tag that lowers and scales text to look like a subscript
        return $"<sub>{number}</sub>";
    }

    // Convert an integer to Unicode subscript digits (e.g. 12 -> ₁₂)
    private string ToSubscript(int value)
    {
        if (value <= 0) return "";
        var s = value.ToString();
        var sb = new StringBuilder();
        foreach (var ch in s)
        {
            switch (ch)
            {
                case '0': sb.Append('\u2080'); break;
                case '1': sb.Append('\u2081'); break;
                case '2': sb.Append('\u2082'); break;
                case '3': sb.Append('\u2083'); break;
                case '4': sb.Append('\u2084'); break;
                case '5': sb.Append('\u2085'); break;
                case '6': sb.Append('\u2086'); break;
                case '7': sb.Append('\u2087'); break;
                case '8': sb.Append('\u2088'); break;
                case '9': sb.Append('\u2089'); break;
                default: sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }

    // Reuse the same symbol mapping used elsewhere (keeps formula consistent with recipe UI)
    private string GetSymbolForScriptableObject(ScriptableObject so)
    {
        if (so == null) return "?";
        string name = StripCommonPrefix(so.name);

        var map = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            {"Hydrogen", "H"},
            {"Helium", "He"},
            {"Beryllium", "Be"},
            {"Carbon", "C"},
            {"Magnesium", "Mg"},
            {"Aluminum", "Al"},
            {"Silicon", "Si"},
            {"Phosphorus", "P"},
            {"Sulphur", "S"},
            {"Sulfur", "S"},
            {"Calcium", "Ca"},
            {"Titanium", "Ti"},
            {"Iron", "Fe"},
            {"Copper", "Cu"},
            {"Barium", "Ba"},
            {"Oxygen", "O"},
            {"Carbonate", "CO3"},
            {"Sulfate", "SO4"},
            {"Nitrate", "NO3"},
            {"Phosphate", "PO4"},
            {"Silicate", "SiO4"},
            {"Oxide", "O"},
        };

        if (map.TryGetValue(name, out var sym)) return sym;

        if (name.Length == 1) return name.ToUpper();
        return char.ToUpper(name[0]) + name.Substring(1).ToLower();
    }

    // Helper to strip common asset prefixes (E_, R_, M_, C_)
    private string StripCommonPrefix(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        string[] prefixes = new[] { "E_", "R_", "M_", "C_" };
        foreach (var p in prefixes)
        {
            if (name.StartsWith(p)) return name.Substring(p.Length);
        }
        return name;
    }
}
