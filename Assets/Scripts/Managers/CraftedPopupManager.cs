using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
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

    [Header("Small Popups")]
    [Tooltip("A UI prefab for elements/compounds that contains a TextMeshProUGUI named 'titleText'.")]
    [SerializeField] private GameObject smallPopupPrefab;
    [Tooltip("A fullscreen/persistent popup prefab for elements/compounds that stays until clicked.")]
    [SerializeField] private GameObject smallPersistentPopupPrefab;

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

    // Show a popup for a mineral/element/compound at world position
    // Optional: pass the CraftingRecipe that produced the item so we can build a compact formula string
    public void ShowCraftedPopup(ScriptableObject data, Vector3 worldPosition, CraftingRecipe recipe = null)
    {
        Debug.Log($"CraftedPopupManager: ShowCraftedPopup called for '{(data!=null?data.name:"<null>")}' at {worldPosition}");
        if (data == null) return;

        // choose appropriate prefab (elements/compounds -> small popups; minerals -> normal)
        var chosenPrefab = ChoosePopupPrefab(data, persistent: false);
        if (chosenPrefab == null)
        {
            Debug.LogWarning("CraftedPopupManager: chosen transient popup prefab not assigned — cannot show popup.");
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
        var go = Instantiate(chosenPrefab, _uiCanvas.transform, false);

        // Give transient popups their own Canvas with override sorting so they appear above world sprites
        var transientCanvas = go.GetComponent<Canvas>();
        if (transientCanvas == null) transientCanvas = go.AddComponent<Canvas>();
        transientCanvas.overrideSorting = true;
        transientCanvas.sortingOrder = 500;
        if (go.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null) go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Position it near the world point by converting to screen space
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_uiCanvas.transform as RectTransform, screenPos, _uiCanvas.worldCamera, out localPoint);
            rt.anchoredPosition = localPoint;
        }

        // Find icon and title inside prefab (case-insensitive search)
        Image icon = FindChildComponentByName<Image>(go, "icon");
        TextMeshProUGUI title = FindChildComponentByName<TextMeshProUGUI>(go, "titleText");

        // Populate icon (if present) using best available sprite fields on the ScriptableObject
        if (icon != null)
        {
            var big = GetBigSpriteFromData(data);
            var primary = GetPrimarySpriteFromData(data);
            icon.sprite = big != null ? big : primary;
            icon.color = GetColorFromData(data, Color.white);
        }

        if (icon == null && chosenPrefab != smallPopupPrefab)
        {
            Debug.LogWarning("CraftedPopupManager: popup prefab does not contain an Image child named 'icon' (or similar). The icon will be missing.");
        }

        if (title != null)
        {
            // Display a tidy name (remove prefix before underscore if present)
            string temp = GetDisplayNameFromData(data);
            int idx = temp.IndexOf('_');
            if (idx >= 0 && idx + 1 < temp.Length) temp = temp.Substring(idx + 1);

            // If we have a recipe, build a compact formula and append in parentheses.
            // For Elements, prefer deriving a formula/symbol from their name instead of the recipe.
            if (data is ElementData)
            {
                string formula = GetSymbolForScriptableObject(data);
                if (!string.IsNullOrEmpty(formula)) temp = string.Format("{0} ({1})", temp, formula);
            }
            else if (recipe != null)
            {
                // Use the title's font to decide whether to use Unicode subscripts or TMP rich-text fallback
                string formula = BuildFormulaForTitle(title, recipe);
                if (!string.IsNullOrEmpty(formula)) temp = string.Format("{0} ({1})", temp, formula);
            }

            title.text = temp;
            title.color = GetColorFromData(data, Color.white);
        }
        else
        {
            if (chosenPrefab != smallPopupPrefab)
                Debug.LogWarning("CraftedPopupManager: popup prefab does not contain a TextMeshProUGUI child named 'titleText' (or similar). The title will be missing.");
        }

        // Ensure a CanvasGroup exists for fade/alpha animation (transient only)
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        // start animation coroutine
        StartCoroutine(AnimateAndDestroy(go, cg, popupDuration));
    }

    // Show a fullscreen/persistent popup that stays until the player clicks to dismiss
    public void ShowPersistentCraftedPopup(ScriptableObject data, CraftingRecipe recipe = null)
    {
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

        // choose appropriate prefab (elements/compounds -> small persistent; minerals -> normal persistent)
        var chosenPrefab = ChoosePopupPrefab(data, persistent: true);
        if (chosenPrefab == null)
        {
            Debug.LogWarning("CraftedPopupManager: chosen persistent popup prefab not assigned — falling back to transient popup.");
            // fallback to the transient variant placed slightly in front of camera
            Vector3 centerWorld = Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 2f : Vector3.zero;
            ShowCraftedPopup(data, centerWorld, recipe);
            return;
        }

        var go = Instantiate(chosenPrefab, _uiCanvas.transform, false);
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
            var big = GetBigSpriteFromData(data);
            var primary = GetPrimarySpriteFromData(data);
            icon.sprite = big != null ? big : primary;
        }

        if (title != null)
        {
            string temp = GetDisplayNameFromData(data);
            int idx = temp.IndexOf('_');
            if (idx >= 0 && idx + 1 < temp.Length) temp = temp.Substring(idx + 1);

            if (data is ElementData)
            {
                string formula = GetSymbolForScriptableObject(data);
                if (!string.IsNullOrEmpty(formula)) temp = string.Format("{0} ({1})", temp, formula);
            }
            else if (recipe != null)
            {
                string formula = BuildFormulaForTitle(title, recipe);
                if (!string.IsNullOrEmpty(formula)) temp = string.Format("{0} ({1})", temp, formula);
            }

            title.text = temp;
            title.color = GetColorFromData(data, Color.white);
        }

        // Update the 'discovery' text child if present to reflect element/compound/mineral
        var discoveryTmp = FindChildComponentByName<TextMeshProUGUI>(go, "discovery");
        if (discoveryTmp != null)
        {
            if (data is ElementData) discoveryTmp.text = "You discovered a new element!";
            else if (data is CompoundData) discoveryTmp.text = "You discovered a new compound!";
            else discoveryTmp.text = "You discovered a new mineral!";
        }
        else
        {
            var discoveryText = FindChildComponentByName<Text>(go, "discovery");
            if (discoveryText != null)
            {
                if (data is ElementData) discoveryText.text = "You discovered a new element!";
                else if (data is CompoundData) discoveryText.text = "You discovered a new compound!";
                else discoveryText.text = "You discovered a new mineral!";
            }
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

    // NOTE: formula construction is handled by BuildFormulaForTitle which formats for TMP/font support.
    // The older BuildFormulaFromRecipe implementation was removed to reduce duplication.

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

    // Helper: determine whether this data represents an element or compound (by type name heuristic)
    private bool IsElementOrCompound(ScriptableObject data)
    {
        if (data == null) return false;
        var typeName = data.GetType().Name;
        if (string.IsNullOrEmpty(typeName)) return false;
        var lower = typeName.ToLower();
        return lower.Contains("element") || lower.Contains("compound");
    }

    // Helper: pick the appropriate prefab based on type and whether persistent is requested
    private GameObject ChoosePopupPrefab(ScriptableObject data, bool persistent)
    {
        bool small = IsElementOrCompound(data);
        if (small)
            return persistent ? smallPersistentPopupPrefab : smallPopupPrefab;
        else
            return persistent ? persistentPopupPrefab : popupPrefab;
    }

    // Reflection helpers to extract common fields/properties from different Data ScriptableObjects

    private Sprite GetPrimarySpriteFromData(ScriptableObject so)
    {
        if (so == null) return null;
        var candidates = new[] { "mineralSprite", "elementSprite", "compoundSprite", "sprite", "icon" };
        foreach (var name in candidates)
        {
            var v = GetFieldOrPropertyValue(so, name);
            if (v is Sprite sp) return sp;
        }
        return null;
    }

    private Sprite GetBigSpriteFromData(ScriptableObject so)
    {
        if (so == null) return null;
        var candidates = new[] { "mineralBigSprite", "bigSprite", "largeSprite" };
        foreach (var name in candidates)
        {
            var v = GetFieldOrPropertyValue(so, name);
            if (v is Sprite sp) return sp;
        }
        return null;
    }

    private Color GetColorFromData(ScriptableObject so, Color fallback)
    {
        if (so == null) return fallback;
        var v = GetFieldOrPropertyValue(so, "defaultColor");
        if (v is Color c) return c;
        if (v is Color32 c32) return (Color)c32;
        return fallback;
    }

    private string GetDisplayNameFromData(ScriptableObject so)
    {
        if (so == null) return string.Empty;
        var candidates = new[] { "mineralName", "elementName", "compoundName", "displayName", "title" };
        foreach (var name in candidates)
        {
            var v = GetFieldOrPropertyValue(so, name);
            if (v is string s && !string.IsNullOrEmpty(s)) return s;
        }
        // fallback to asset name (strip prefixes)
        return StripCommonPrefix(so.name);
    }

    private object GetFieldOrPropertyValue(ScriptableObject so, string name)
    {
        if (so == null) return null;
        var type = so.GetType();
        // try field
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) return field.GetValue(so);
        // try property
        var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null) return prop.GetValue(so, null);
        // try PascalCase variant
        var pascal = char.ToUpperInvariant(name[0]) + name.Substring(1);
        field = type.GetField(pascal, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) return field.GetValue(so);
        prop = type.GetProperty(pascal, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null) return prop.GetValue(so, null);
        return null;
    }
}
