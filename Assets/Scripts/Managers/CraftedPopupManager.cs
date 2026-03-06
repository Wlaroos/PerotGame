using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftedPopupManager : MonoBehaviour
{
    public static CraftedPopupManager Instance { get; private set; }

    [Header("Prefab & Timing")] 
    [Tooltip("A UI prefab that contains an Image named 'icon' and a TextMeshProUGUI named 'titleText'. It should be a simple root GameObject (no required components).")]
    [SerializeField] private GameObject _popupPrefab;
    [Tooltip("A fullscreen/persistent popup prefab that stays until clicked. If assigned, use this for first-time big popups.")]
    [SerializeField] private GameObject _persistentPopupPrefab;
    [Tooltip("How long the popup remains visible (seconds)")]
    [SerializeField] private float _popupDuration = 1.6f;
    [Tooltip("Vertical offset in pixels the popup will move up during the animation")]
    [SerializeField] private float _moveUp = 40f;
    [SerializeField] private Canvas _popupCanvas;

    [Header("Small Popups")]
    [Tooltip("A UI prefab for elements/compounds that contains a TextMeshProUGUI named 'titleText'.")]
    [SerializeField] private GameObject _smallPopupPrefab;
    [Tooltip("A fullscreen/persistent popup prefab for elements/compounds that stays until clicked.")]
    [SerializeField] private GameObject _smallPersistentPopupPrefab;

    [Tooltip("Delay (in seconds) before the popup can be dismissed.")]
    [SerializeField] private float _dismissDelay = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
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


        // Instantiate under the UI canvas
        var go = Instantiate(chosenPrefab, _popupCanvas.transform, false);

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
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_popupCanvas.transform as RectTransform, screenPos, _popupCanvas.worldCamera, out localPoint);
            rt.anchoredPosition = localPoint;
        }

        // Use SOHelpers for child lookups and data extraction
        Image icon = SOHelpers.FindChildComponentByName<Image>(go, "icon");
        TextMeshProUGUI title = SOHelpers.FindChildComponentByName<TextMeshProUGUI>(go, "titleText");
        TextMeshProUGUI funFact = SOHelpers.FindChildComponentByName<TextMeshProUGUI>(go, "funFactText");

        // Populate icon (if present) using best available sprite fields on the ScriptableObject
        if (icon != null)
        {
            var big = SOHelpers.GetBigSpriteFromData(data);
            var primary = SOHelpers.GetPrimarySpriteFromData(data);
            icon.sprite = big != null ? big : primary;
            icon.color = SOHelpers.GetColorFromData(data);
        }

        if (icon == null && chosenPrefab != _smallPopupPrefab)
        {
            Debug.LogWarning("CraftedPopupManager: popup prefab does not contain an Image child named 'icon' (or similar). The icon will be missing.");
        }

        if (title != null)
        {
            // Display a tidy name (remove prefix before underscore if present)
            string temp = SOHelpers.GetDisplayNameFromData(data);
            int idx = temp.IndexOf('_');
            if (idx >= 0 && idx + 1 < temp.Length) temp = temp.Substring(idx + 1);

            // For Elements and Compounds (including variant compounds) show their symbol via SOHelpers.
            // For Minerals without a recipe, show variant-aware symbol as well.
            if (data is ElementData || data is CompoundData)
            {
                string formula = SOHelpers.GetSymbolForScriptableObject(data);
                if (!string.IsNullOrEmpty(formula)) temp = string.Format("{0} ({1})", temp, formula);
            }
            else if (data is MineralData && recipe == null)
            {
                string formula = SOHelpers.GetSymbolForScriptableObject(data);
                if (!string.IsNullOrEmpty(formula)) temp = string.Format("{0} ({1})", temp, formula);
            }
            else if (recipe != null)
            {
                string formula = BuildFormulaForTitle(title, recipe);
                if (!string.IsNullOrEmpty(formula)) temp = string.Format("{0} ({1})", temp, formula);
            }

            title.text = temp;
            title.color = SOHelpers.GetColorFromData(data);
        }
        else
        {
            if (chosenPrefab != _smallPopupPrefab)
                Debug.LogWarning("CraftedPopupManager: popup prefab does not contain a TextMeshProUGUI child named 'titleText' (or similar). The title will be missing.");
        }

        // Ensure a CanvasGroup exists for fade/alpha animation (transient only)
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        // start animation coroutine
        StartCoroutine(AnimateAndDestroy(go, cg, _popupDuration));
    }

    // Show a fullscreen/persistent popup that stays until the player clicks to dismiss
    public void ShowPersistentCraftedPopup(ScriptableObject data, CraftingRecipe recipe = null)
    {
        if (data == null) return;

        if (_popupCanvas == null)
        {
            _popupCanvas = SOHelpers.GetAnyCanvas(this._popupCanvas);
            if (_popupCanvas == null)
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

        var go = Instantiate(chosenPrefab, _popupCanvas.transform, false);

        // Try to populate icon/title similarly to the transient popup
        Image icon = SOHelpers.FindChildComponentByName<Image>(go, "icon");
        TextMeshProUGUI title = SOHelpers.FindChildComponentByName<TextMeshProUGUI>(go, "titleText");
        TextMeshProUGUI funFact = SOHelpers.FindChildComponentByName<TextMeshProUGUI>(go, "funFactText");

        if (icon != null)
        {
            var big = SOHelpers.GetBigSpriteFromData(data);
            var primary = SOHelpers.GetPrimarySpriteFromData(data);
            icon.sprite = big != null ? big : primary;
        }

        if (title != null)
        {
            string temp = SOHelpers.GetDisplayNameFromData(data);
            int idx = temp.IndexOf('_');
            if (idx >= 0 && idx + 1 < temp.Length) temp = temp.Substring(idx + 1);

            if (data is ElementData)
            {
                string formula = SOHelpers.GetSymbolForScriptableObject(data);
                if (!string.IsNullOrEmpty(formula)) temp = string.Format("{0} ({1})", temp, formula);
            }
            else if (recipe != null)
            {
                string formula = BuildFormulaForTitle(title, recipe);
                if (!string.IsNullOrEmpty(formula)) temp = string.Format("{0} ({1})", temp, formula);
            }

            title.text = temp;
            title.color = SOHelpers.GetColorFromData(data);
        }

        if(funFact != null)
        {
            string temp = SOHelpers.GetFunFactFromData(data);
            funFact.text = temp;
        }

        // Update the 'discovery' text child if present to reflect element/compound/mineral
        var discoveryTmp = SOHelpers.FindChildComponentByName<TextMeshProUGUI>(go, "discovery");
        if (discoveryTmp != null)
        {
            if (data is ElementData) discoveryTmp.text = "You discovered a new element!";
            else if (data is CompoundData) discoveryTmp.text = "You discovered a new compound!";
            else if (data is MineralData && data.name.Contains("Slag")) discoveryTmp.text = "Oops, you created Slag! Try again!";
            else discoveryTmp.text = "You discovered a new mineral!";
        }
        else
        {
            var discoveryText = SOHelpers.FindChildComponentByName<Text>(go, "discovery");
            if (discoveryText != null)
            {
                if (data is ElementData) discoveryText.text = "You discovered a new element!";
                else if (data is CompoundData) discoveryText.text = "You discovered a new compound!";
                else if (data is MineralData && data.name.Contains("Slag")) discoveryText.text = "Oops, you created Slag! Try again!";
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

        // Delay dismiss functionality
        overlayBtn.interactable = false;
        StartCoroutine(EnableDismissAfterDelay(overlayBtn, _dismissDelay));

        overlayBtn.onClick.AddListener(() => { if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go); });
        // Ensure overlay is on top so any click dismisses the popup
        overlay.transform.SetAsLastSibling();
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
                rt.anchoredPosition += new Vector2(0f, (_moveUp / duration) * Time.deltaTime);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(go);
    }

    // Build a formula string appropriate for the provided TextMeshProUGUI title.
    // If the title's font asset contains Unicode subscript digits, use them; otherwise produce TMP rich-text subscripts.
    private string BuildFormulaForTitle(TextMeshProUGUI title, CraftingRecipe recipe)
    {
        if (recipe == null) return string.Empty;

        bool useUnicodeSubscripts = false;
        if (title != null && title.font != null)
        {
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

        // Collect all possible inputs (A..H)
        var ingredients = new List<ScriptableObject>
        {
            recipe.inputA, recipe.inputB, recipe.inputC, recipe.inputD,
            recipe.inputE, recipe.inputF, recipe.inputG, recipe.inputH
        }.Where(x => x != null).ToList();
        if (ingredients.Count == 0) return string.Empty;

        var symbols = ingredients.Select(i => SOHelpers.GetSymbolForScriptableObject(i)).ToList();
        var ordered = new List<string>();
        foreach (var s in symbols) if (!ordered.Contains(s)) ordered.Add(s);
        var counts = symbols.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

        var sb = new StringBuilder();
        foreach (var s in ordered)
        {
            int c = counts.TryGetValue(s, out var v) ? v : 0;
            bool hasDigit = s.Any(ch => char.IsDigit(ch));
            // format symbol digits appropriately
            string formattedSymbol = SOHelpers.FormatFormulaForDisplay(s, useUnicodeSubscripts);

            if (c <= 1)
            {
                if (hasDigit)
                    sb.Append('(').Append(formattedSymbol).Append(')');
                else
                    sb.Append(formattedSymbol);
            }
            else
            {
                if (hasDigit)
                    sb.Append('(').Append(formattedSymbol).Append(')');
                else
                    sb.Append(formattedSymbol);

                if (useUnicodeSubscripts)
                    sb.Append(SOHelpers.ToSubscript(c));
                else
                    sb.Append(SOHelpers.MakeSubscriptTMP(c.ToString()));
            }
        }

        if (!useUnicodeSubscripts && title != null)
        {
            title.richText = true;
        }

        return sb.ToString();
    }

    // Use SOHelpers-based helpers for choosing the prefab/type detection
    private GameObject ChoosePopupPrefab(ScriptableObject data, bool persistent)
    {
        bool small = data != null && (data is ElementData || data is CompoundData);
        if (small)
            return persistent ? _smallPersistentPopupPrefab : _smallPopupPrefab;
        else
            return persistent ? _persistentPopupPrefab : _popupPrefab;
    }

    private IEnumerator EnableDismissAfterDelay(UnityEngine.UI.Button button, float delay)
    {
        yield return new WaitForSeconds(delay);
        button.interactable = true;
    }
}
