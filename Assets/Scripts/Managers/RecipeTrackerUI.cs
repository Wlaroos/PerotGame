using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeTrackerUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform contentParent;
    public GameObject recipeItemPrefab;
    public TextMeshProUGUI selectedTitleText;
    public TextMeshProUGUI selectedDetailsText;
    public Image selectedImage; // optional image shown for the selected recipe's output (when discovered)

    // (Sorting modes are represented by booleans; enum removed as it was unused)

    [Header("Sort Options")]
    [Tooltip("Sort recipes alphabetically by their (stripped) name")]
    private bool sortAlphabetical = true;
    [Tooltip("Sort recipes by number of ingredients (fewest -> most)")]
    private bool sortByIngredientCount = false;
    [Tooltip("Sort recipes by output type (Mineral, Compound, Element)")]
    private bool sortByOutputType = false;
    [Tooltip("Reverse the final sort order (after applying the enabled sorts)")]
    private bool reverseSort = false;

    [Tooltip("If true, insert a small spacer or header prefab between different sort sections (e.g. between output types)")]
    [SerializeField] private bool addSectionSpacers = true;

    [Tooltip("Optional prefab to use as a section header between groups. If assigned, it's instantiated and its child Text/TMP will be set to the group label.")]
    [SerializeField] private GameObject sectionHeaderPrefab;

    [Header("Runtime Sort UI (optional)")]
    [Tooltip("Optional UI Button to toggle alphabetical sort at runtime")]
    [SerializeField] private Button sortAlphabeticalButton;
    [Tooltip("Optional UI Button to toggle ingredient-count sort at runtime")]
    [SerializeField] private Button sortIngredientCountButton;
    [Tooltip("Optional UI Button to toggle output-type sort at runtime")]
    [SerializeField] private Button sortOutputTypeButton;
    [Tooltip("Optional UI Button to toggle reverse order at runtime")]
    [SerializeField] private Button reverseSortButton;
    [Tooltip("Color used for an active/selected sort button")]
    [SerializeField] private Color sortButtonActiveColor = Color.green;
    [Tooltip("Color used for an inactive/unselected sort button")]
    [SerializeField] private Color sortButtonInactiveColor = Color.white;

    // tint colors
    public Color undiscoveredColor = Color.white;
    public Color discoveredColor = Color.green;

    // in-memory lookup
    private List<CraftingRecipe> _recipes = new List<CraftingRecipe>();
    private Dictionary<CraftingRecipe, RecipeItemController> _itemControllers = new Dictionary<CraftingRecipe, RecipeItemController>();
    // Track instantiated section header instances so we can clear them efficiently
    private List<GameObject> _activeSectionHeaders = new List<GameObject>();

    // non-persistent discovery set (resets each run)
    private HashSet<string> _discoveredRecipes = new HashSet<string>();

    private void Start()
    {
        // discovery set is initialized at declaration; no need to clear here

        if (CraftingManager.Instance == null)
        {
            Debug.LogError("RecipeTrackerUI: CraftingManager.Instance is null. Make sure CraftingManager exists in the scene.");
            return;
        }

        // grab recipes from the crafting manager
        // Exclude internal/group recipes whose asset name starts with "GR_"
        _recipes = CraftingManager.Instance._recipes
            .Where(r => r != null && !(r.name != null && r.name.StartsWith("GR_")))
            .ToList();

        PopulateList();

        // subscribe for first-time crafted events (expects CraftingManager to only fire this when craft actually occurs)
        CraftingManager.Instance.OnFirstTimeRecipeCrafted.AddListener(OnFirstTimeRecipeCrafted);

        SelectRecipe(null);
    }

    private void OnDestroy()
    {
        if (CraftingManager.Instance != null)
            CraftingManager.Instance.OnFirstTimeRecipeCrafted.RemoveListener(OnFirstTimeRecipeCrafted);
        // Unbind runtime UI listeners
        UnbindRuntimeSortButtons();
    }

    private void OnEnable()
    {
        BindRuntimeSortButtons();
    }

    private void OnDisable()
    {
        UnbindRuntimeSortButtons();
    }

    private void BindRuntimeSortButtons()
    {
        if (sortAlphabeticalButton != null) sortAlphabeticalButton.onClick.AddListener(ToggleSortAlphabetical);
        if (sortIngredientCountButton != null) sortIngredientCountButton.onClick.AddListener(ToggleSortIngredientCount);
        if (sortOutputTypeButton != null) sortOutputTypeButton.onClick.AddListener(ToggleSortOutputType);
        if (reverseSortButton != null) reverseSortButton.onClick.AddListener(ToggleReverseSort);
        UpdateSortButtonVisuals();
    }

    private void UnbindRuntimeSortButtons()
    {
        if (sortAlphabeticalButton != null) sortAlphabeticalButton.onClick.RemoveListener(ToggleSortAlphabetical);
        if (sortIngredientCountButton != null) sortIngredientCountButton.onClick.RemoveListener(ToggleSortIngredientCount);
        if (sortOutputTypeButton != null) sortOutputTypeButton.onClick.RemoveListener(ToggleSortOutputType);
        if (reverseSortButton != null) reverseSortButton.onClick.RemoveListener(ToggleReverseSort);
    }

    private void UpdateSortButtonVisuals()
    {
        // Set the button image colors to indicate active/inactive state
        if (sortAlphabeticalButton != null && sortAlphabeticalButton.image != null)
            sortAlphabeticalButton.image.color = sortAlphabetical ? sortButtonActiveColor : sortButtonInactiveColor;
        if (sortIngredientCountButton != null && sortIngredientCountButton.image != null)
            sortIngredientCountButton.image.color = sortByIngredientCount ? sortButtonActiveColor : sortButtonInactiveColor;
        if (sortOutputTypeButton != null && sortOutputTypeButton.image != null)
            sortOutputTypeButton.image.color = sortByOutputType ? sortButtonActiveColor : sortButtonInactiveColor;
        if (reverseSortButton != null && reverseSortButton.image != null)
            reverseSortButton.image.color = reverseSort ? sortButtonActiveColor : sortButtonInactiveColor;
    }

    // Runtime toggle methods that can be wired to UI buttons or called programmatically
    public void ToggleSortAlphabetical()
    {
        ApplyExclusiveToggle("Alphabetical");
        UpdateSortButtonVisuals();
        ScheduleRefresh();
    }

    public void ToggleSortIngredientCount()
    {
        ApplyExclusiveToggle("IngredientCount");
        UpdateSortButtonVisuals();
        ScheduleRefresh();
    }

    public void ToggleSortOutputType()
    {
        ApplyExclusiveToggle("OutputType");
        UpdateSortButtonVisuals();
        ScheduleRefresh();
    }

    public void ToggleReverseSort()
    {
        reverseSort = !reverseSort;
        UpdateSortButtonVisuals();
        ScheduleRefresh();
    }

    // Defer refresh to avoid modifying UI hierarchy during event processing (prevents potential crashes)
    private Coroutine _refreshCoroutine;
    private void ScheduleRefresh(float delayFrames = 1)
    {
        if (_refreshCoroutine != null) StopCoroutine(_refreshCoroutine);
        _refreshCoroutine = StartCoroutine(DeferredRefresh(delayFrames));
    }

    private System.Collections.IEnumerator DeferredRefresh(float delayFrames)
    {
        // Wait for specified frames (default 1) to let UI event processing complete
        for (int i = 0; i < (int)delayFrames; i++)
            yield return null;

        try
        {
            PopulateList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RecipeTrackerUI: error during deferred PopulateList: {ex}");
        }

        _refreshCoroutine = null;
    }

    // Exclusive sort setters: select one sort mode and unselect the others
    

    // Toggle a sort exclusively; if toggling off the active sort, pick a fallback.
    private void ApplyExclusiveToggle(string sortName)
    {
        // Helper to set a specific sort
        void SetSort(string name)
        {
            sortAlphabetical = name == "Alphabetical";
            sortByIngredientCount = name == "IngredientCount";
            sortByOutputType = name == "OutputType";
        }

        // Check current state
        bool isActive = (sortName == "Alphabetical" && sortAlphabetical)
                        || (sortName == "IngredientCount" && sortByIngredientCount)
                        || (sortName == "OutputType" && sortByOutputType);

        if (!isActive)
        {
            // Activate this sort exclusively
            SetSort(sortName);
            return;
        }

        // It was active; toggle it off and choose a fallback in priority order
        // Fallback priority: IngredientCount -> OutputType -> Alphabetical
        if (sortName == "Alphabetical") sortAlphabetical = false;
        else if (sortName == "IngredientCount") sortByIngredientCount = false;
        else if (sortName == "OutputType") sortByOutputType = false;

        // Pick fallback
        if (!sortByIngredientCount && sortName != "IngredientCount")
        {
            // prefer ingredient count as fallback
            SetSort("IngredientCount");
            return;
        }

        if (!sortByOutputType && sortName != "OutputType")
        {
            SetSort("OutputType");
            return;
        }

        // final fallback to alphabetical
        SetSort("Alphabetical");
    }

    private void OnValidate()
    {
        // Ensure at least one sort is active. If multiple are active (inspector), prefer alphabetical, then ingredient, then output.
        if (sortAlphabetical && (sortByIngredientCount || sortByOutputType))
        {
            sortByIngredientCount = false;
            sortByOutputType = false;
        }
        else if (sortByIngredientCount && sortByOutputType)
        {
            // keep ingredient count, disable output
            sortByOutputType = false;
        }
        else if (!sortAlphabetical && !sortByIngredientCount && !sortByOutputType)
        {
            // ensure at least one is active
            sortAlphabetical = true;
        }
    }

    private void PopulateList()
    {
        if (contentParent == null || recipeItemPrefab == null)
        {
            Debug.LogWarning("RecipeTrackerUI: contentParent or recipeItemPrefab not assigned.");
            return;
        }
        // Clear previous section headers (we'll recreate them below if needed)
        ClearSectionHeaders();

        // Build sorted/filtered recipe list according to current sort options
        var desiredRecipes = BuildSortedRecipes();

        // Track which controllers we reuse this pass so we can cleanup the rest
        var usedControllers = new HashSet<CraftingRecipe>();
        string lastGroupKey = null;

        // Iterate desired recipes, create headers as needed and ensure an item controller exists
        foreach (var recipe in desiredRecipes)
        {
            string groupKey = ComputeGroupKey(recipe);

            if (addSectionSpacers && (lastGroupKey == null || groupKey != lastGroupKey))
            {
                CreateSectionHeader(groupKey);
            }

            EnsureControllerForRecipe(recipe, usedControllers);

            lastGroupKey = groupKey;
        }

        // Remove any controllers that were not used this pass
        CleanupUnusedControllers(usedControllers);
    }

        // Build the sorted recipe list according to current sort options
        private List<CraftingRecipe> BuildSortedRecipes()
        {
            IEnumerable<CraftingRecipe> sorted = _recipes.Where(r => r != null);

            IOrderedEnumerable<CraftingRecipe> ordered = null;

            if (sortByOutputType)
            {
                ordered = sorted.OrderBy(r => GetOutputTypeOrder(r)).ThenBy(r => GetRecipeSortKey(r));
            }

            if (sortByIngredientCount)
            {
                if (ordered != null) ordered = ordered.ThenBy(r => CountIngredients(r));
                else ordered = sorted.OrderBy(r => CountIngredients(r)).ThenBy(r => GetRecipeSortKey(r));
            }

            if (sortAlphabetical)
            {
                if (ordered != null) ordered = ordered.ThenBy(r => GetRecipeSortKey(r));
                else ordered = sorted.OrderBy(r => GetRecipeSortKey(r));
            }

            if (ordered != null) sorted = ordered;
            else sorted = sorted.OrderBy(r => GetRecipeSortKey(r));

            if (reverseSort) sorted = sorted.Reverse();

            return sorted.ToList();
        }

        private void ClearSectionHeaders()
        {
            foreach (var h in _activeSectionHeaders)
            {
                if (h == null) continue;
                if (Application.isPlaying) Destroy(h);
                else DestroyImmediate(h);
            }
            _activeSectionHeaders.Clear();
        }

        private void CreateSectionHeader(string groupKey)
        {
            if (sectionHeaderPrefab == null) return;
            var header = Instantiate(sectionHeaderPrefab, contentParent);
            var headerTmp = header.GetComponentInChildren<TextMeshProUGUI>(true);
            if (headerTmp != null) headerTmp.text = groupKey;
            else
            {
                var headerText = header.GetComponentInChildren<Text>(true);
                if (headerText != null) headerText.text = groupKey;
            }
            _activeSectionHeaders.Add(header);
        }

        private void EnsureControllerForRecipe(CraftingRecipe recipe, HashSet<CraftingRecipe> usedControllers)
        {
            if (recipe == null) return;

            if (_itemControllers.TryGetValue(recipe, out var existing))
            {
                // reuse existing controller
                existing.root.transform.SetParent(contentParent, false);
                existing.root.transform.SetAsLastSibling();
                existing.root.SetActive(true);
                existing.Refresh(IsRecipeDiscovered(recipe));
                usedControllers.Add(recipe);
            }
            else
            {
                var go = Instantiate(recipeItemPrefab, contentParent);
                var controller = new RecipeItemController(go, recipe, this);
                _itemControllers.Add(recipe, controller);
                controller.Refresh(IsRecipeDiscovered(recipe));
                usedControllers.Add(recipe);
            }
        }

        private void CleanupUnusedControllers(HashSet<CraftingRecipe> usedControllers)
        {
            var toRemove = _itemControllers.Keys.Except(usedControllers).ToList();
            foreach (var r in toRemove)
            {
                if (_itemControllers.TryGetValue(r, out var c))
                {
                    if (c != null && c.root != null)
                    {
                        if (Application.isPlaying) Destroy(c.root);
                        else DestroyImmediate(c.root);
                    }
                    _itemControllers.Remove(r);
                }
            }
        }


    // Clear the current list items (keeps the _recipes list intact)
    public void ClearList()
    {
        if (contentParent == null) return;
        // Destroy all children safely (use while loop to avoid iterator issues)
        while (contentParent.childCount > 0)
        {
            var c = contentParent.GetChild(0);
            if (Application.isPlaying)
                Destroy(c.gameObject);
            else
                DestroyImmediate(c.gameObject);
        }

        _itemControllers.Clear();
        // Clear any preview discovery state so the list returns to initial state
        _discoveredRecipes.Clear();
    }

    // Editor-friendly: load recipes from Resources/SOs/recipes and populate the list immediately.
    // This is safe to call from an editor script to preview the list without running the game.
    public void PreviewPopulateFromResources()
    {
        var loaded = Resources.LoadAll<CraftingRecipe>("SOs/recipes");
        _recipes = loaded.Where(r => r != null && !(r.name != null && r.name.StartsWith("GR_"))).ToList();

        // Mark all loaded recipes as discovered so the preview shows full names/details
        _discoveredRecipes.Clear();
        foreach (var r in _recipes)
        {
            if (r != null && !string.IsNullOrEmpty(r.name)) _discoveredRecipes.Add(r.name);
        }

        PopulateList();
    }

    // Helper: compute a string key to group recipes by based on the active primary sort
    private string ComputeGroupKey(CraftingRecipe recipe)
    {
        // Grouping is based on the highest-priority enabled sort. Priority order is:
        // OutputType -> IngredientCount -> Alphabetical
        if (sortByOutputType)
        {
            var t = GetOutputTypeLabel(recipe);
            return t ?? "";
        }
        else if (sortByIngredientCount)
        {
            return CountIngredients(recipe).ToString();
        }
        else if (sortAlphabetical)
        {
            string name = GetBaseName(recipe.name); // use base name so variants group under same letter
            if (string.IsNullOrEmpty(name)) return "";
            return name.Substring(0, 1).ToUpper();
        }

        return "";
    }

    private int CountIngredients(CraftingRecipe recipe)
    {
        if (recipe == null) return 0;
        return new List<ScriptableObject> { recipe.inputA, recipe.inputB, recipe.inputC, recipe.inputD, recipe.inputE, recipe.inputF, recipe.inputG, recipe.inputH }
            .Where(x => x != null).Count();
    }

    // Output type ordering: Mineral first, then Compound, then Element, then other
    private int GetOutputTypeOrder(CraftingRecipe recipe)
    {
        if (recipe == null || recipe.output == null) return 3;
        if (recipe.output is MineralData) return 0;
        if (recipe.output is CompoundData) return 1;
        if (recipe.output is ElementData) return 2;
        return 3;
    }

    private string GetOutputTypeLabel(CraftingRecipe recipe)
    {
        if (recipe == null || recipe.output == null) return "Other";
        if (recipe.output is MineralData) return "Mineral";
        if (recipe.output is CompoundData) return "Compound";
        if (recipe.output is ElementData) return "Element";
        return "Other";
    }

    private bool IsRecipeDiscovered(CraftingRecipe recipe)
    {
        if (recipe == null) return false;
        return _discoveredRecipes.Contains(recipe.name);
    }

    private void SetRecipeDiscovered(CraftingRecipe recipe, bool discovered)
    {
        if (recipe == null) return;
        if (discovered) _discoveredRecipes.Add(recipe.name);
        else _discoveredRecipes.Remove(recipe.name);
    }

    private void OnFirstTimeRecipeCrafted(CraftingRecipe recipe)
    {
        // mark discovered/completed (in-memory only)
        SetRecipeDiscovered(recipe, true);

        if (_itemControllers.TryGetValue(recipe, out var controller))
        {
            controller.Refresh(true);
        }

        // auto-select the crafted recipe
        SelectRecipe(recipe);
    }
    
    // Replace internal helper implementations with SOHelpers calls
    public void SelectRecipe(CraftingRecipe recipe)
    {
        if (recipe == null)
        {
            if (selectedTitleText != null) selectedTitleText.text = "";
            if (selectedDetailsText != null) selectedDetailsText.text = "";
            if (selectedImage != null) { selectedImage.sprite = null; selectedImage.enabled = false; }
            return;
        }

        bool discovered = IsRecipeDiscovered(recipe);

        if (selectedTitleText != null)
            selectedTitleText.text = discovered ? SOHelpers.StripCommonPrefix(recipe.name) : "Undiscovered Recipe";

        var ingredients = new List<ScriptableObject> { recipe.inputA, recipe.inputB, recipe.inputC, recipe.inputD, recipe.inputE, recipe.inputF, recipe.inputG, recipe.inputH }
            .Where(x => x != null)
            .ToList();

        string ingList = string.Join(", ", ingredients.Select(i => SOHelpers.StripCommonPrefix(GetDisplayName(i, discovered))));

        if (selectedDetailsText != null)
        {
            if (discovered)
            {
                string output = SOHelpers.StripCommonPrefix(GetDisplayName(recipe.output, discovered));
                selectedDetailsText.text = string.Format("Ingredients: {0}\nOutput: {1}", ingList, output);
                if (selectedImage != null)
                {
                    var md = recipe.output as MineralData;
                    if (md != null && md.mineralBigSprite != null)
                    {
                        selectedImage.sprite = md.mineralBigSprite;
                        selectedImage.enabled = true;
                    }
                    else if (md != null && md.mineralSprite != null)
                    {
                        selectedImage.sprite = md.mineralSprite;
                        selectedImage.enabled = true;
                    }
                    else
                    {
                        selectedImage.sprite = null;
                        selectedImage.enabled = false;
                    }
                }
            }
            else
            {
                selectedDetailsText.text = string.Format("Ingredients: {0}", ingList);
                if (selectedImage != null) { selectedImage.sprite = null; selectedImage.enabled = false; }
            }
        }
    }

    // Use SOHelpers.GetBaseName / StripCommonPrefix rather than duplicating logic
    private string GetBaseName(string name)
    {
        return SOHelpers.GetBaseName(name);
    }

    private string GetBaseName(ScriptableObject so)
    {
        if (so == null) return "";
        return SOHelpers.GetBaseName(so.name);
    }

    private string GetRecipeSortKey(CraftingRecipe r)
    {
        if (r == null) return "";
        string baseName = SOHelpers.GetBaseName(r.name) ?? "";
        string fullStripped = SOHelpers.StripCommonPrefix(r.name) ?? "";
        return (baseName + "|" + fullStripped).ToLowerInvariant();
    }

    private string GetSymbolForScriptableObject(ScriptableObject so)
    {
        return SOHelpers.GetSymbolForScriptableObject(so);
    }

    // Used to show names or mask them for undiscovered
    private string GetDisplayName(ScriptableObject so, bool discovered)
    {
        if (so == null) return "";
        if (discovered) return so.name;
        return SOHelpers.GetSymbolForScriptableObject(so);
    }

    // Internal controller for each list item (keeps the script self contained)
    private class RecipeItemController
    {
        public GameObject root;
        // support both TextMeshProUGUI and UnityEngine.UI.Text for the title only
        public TextMeshProUGUI titleTextTMP;
        public Text titleTextUI;
        public Button button;

        private CraftingRecipe recipe;
        private RecipeTrackerUI parent;

        public RecipeItemController(GameObject go, CraftingRecipe recipe, RecipeTrackerUI parent)
        {
            this.root = go;
            this.recipe = recipe;
            this.parent = parent;

            // try to find child titled "titleText"
            var titleTransform = go.transform.Find("titleText");
            if (titleTransform != null)
            {
                titleTextTMP = titleTransform.GetComponent<TextMeshProUGUI>();
                titleTextUI = titleTransform.GetComponent<Text>();
            }

            // Fallback: find any Text/TMP in children for title
            if (titleTextTMP == null && titleTextUI == null)
            {
                titleTextTMP = go.GetComponentInChildren<TextMeshProUGUI>();
                if (titleTextTMP == null)
                    titleTextUI = go.GetComponentInChildren<Text>();
            }

            button = go.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnClicked);
            }

            // set initial visuals safely (use stripped name)
            SafeSetTitle(SOHelpers.StripCommonPrefix(recipe.name));
        }

        public void Refresh(bool discovered)
        {
            SafeSetTitle(discovered ? SOHelpers.StripCommonPrefix(recipe.name) : "Undiscovered Recipe");

            // apply tint to title text only
            if (titleTextTMP != null) titleTextTMP.color = discovered ? parent.discoveredColor : parent.undiscoveredColor;
            if (titleTextUI != null) titleTextUI.color = discovered ? parent.discoveredColor : parent.undiscoveredColor;
        }

        private void OnClicked()
        {
            parent.SelectRecipe(recipe);
        }

        private void SafeSetTitle(string text)
        {
            if (titleTextTMP != null) titleTextTMP.text = text;
            else if (titleTextUI != null) titleTextUI.text = text;
            else Debug.LogWarning($"RecipeTrackerUI: recipe item prefab '{root.name}' has no Text/TMP component for title.");
        }
    }
}
