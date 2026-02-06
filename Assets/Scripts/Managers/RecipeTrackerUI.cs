using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class RecipeTrackerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _contentParent;
    [SerializeField] private GameObject _recipeItemPrefab;
    [SerializeField] private TextMeshProUGUI _selectedTitleText;
    [SerializeField] private TextMeshProUGUI _selectedDetailsText;
    [SerializeField] private Image _selectedImage; // optional image shown for the selected recipe's output (when discovered)

    [Header("Sort Options")]
    [Tooltip("Enable or disable sorting. When disabled, sorting buttons and headers are hidden.")]
    [SerializeField] private bool _enableSorting = true;
    [Tooltip("Sort recipes alphabetically by their (stripped) name")]
    [SerializeField] private bool _sortAlphabetical = true;
    [Tooltip("Sort recipes by number of ingredients (fewest -> most)")]
    [SerializeField] private bool _sortByIngredientCount = false;
    [Tooltip("Sort recipes by output type (Mineral, Compound, Element)")]
    [SerializeField] private bool _sortByOutputType = false;
    [Tooltip("Reverse the final sort order (after applying the enabled sorts)")]
    [SerializeField] private bool _reverseSort = false;

    [Header("Recipe Display Options")]
    [Tooltip("If true, show all recipes. If false, show 5 random recipes.")]
    [SerializeField] private bool _showAllRecipes = true;
    [Tooltip("Recipes to exclude from the random pool.")]
    [SerializeField] private List<CraftingRecipe> _excludedRecipes = new List<CraftingRecipe>();

    [Tooltip("If true, insert a small spacer or header prefab between different sort sections (e.g. between output types)")]
    [SerializeField] private bool _addSectionSpacers = true;

    [Tooltip("Optional prefab to use as a section header between groups. If assigned, it's instantiated and its child Text/TMP will be set to the group label.")]
    [SerializeField] private GameObject _sectionHeaderPrefab;

    [Header("Runtime Sort UI (optional)")]
    [Tooltip("Optional UI Button to toggle alphabetical sort at runtime")]
    [SerializeField] private Button _sortAlphabeticalButton;
    [Tooltip("Optional UI Button to toggle ingredient-count sort at runtime")]
    [SerializeField] private Button _sortIngredientCountButton;
    [Tooltip("Optional UI Button to toggle output-type sort at runtime")]
    [SerializeField] private Button _sortOutputTypeButton;
    [Tooltip("Optional UI Button to toggle reverse order at runtime")]
    [SerializeField] private Button _reverseSortButton;
    [Tooltip("Color used for an active/selected sort button")]
    [SerializeField] private Color _sortButtonActiveColor = Color.green;
    [Tooltip("Color used for an inactive/unselected sort button")]
    [SerializeField] private Color _sortButtonInactiveColor = Color.white;
    [SerializeField] private Color _undiscoveredColor = Color.white;
    [SerializeField] private Color _discoveredColor = Color.green;

    [Header("Display Settings")]
    [Tooltip("Name to show for undiscovered recipes")]
    [SerializeField] private string _undiscoveredName = "???";

    
    // Event fired on every successful craft.
    // Args: (CraftingRecipe recipe, GameObject craftedObject, bool isFirstTime)
    [System.Serializable]
    public class RecipeCraftedEvent : UnityEvent<CraftingRecipe, GameObject, bool> { }
    private UnityAction<CraftingRecipe, GameObject, bool> _onRecipeCraftedHandler;

    // In-memory lookup
    private List<CraftingRecipe> _recipes = new List<CraftingRecipe>();
    private Dictionary<CraftingRecipe, RecipeItemController> _itemControllers = new Dictionary<CraftingRecipe, RecipeItemController>();
    private List<GameObject> _activeSectionHeaders = new List<GameObject>(); // Track instantiated section header instances
    private HashSet<string> _discoveredRecipes = new HashSet<string>(); // Non-persistent discovery set (resets each run)

    private Coroutine _refreshCoroutine; // Deferred refresh coroutine

    private void Start()
    {
        if (CraftingManager.Instance == null)
        {
            Debug.LogError("RecipeTrackerUI: CraftingManager.Instance is null. Make sure CraftingManager exists in the scene.");
            return;
        }

        // Grab recipes from the crafting manager
        _recipes = CraftingManager.Instance._recipes
            .Where(r => r != null && !(r.name != null && r.name.StartsWith("GR_")))
            .ToList();

        UpdateSortingUI();
        PopulateList();

        SelectRecipe(null);
    }

    private void UpdateSortingUI()
    {
        // Enable or disable sorting buttons and their parents based on _enableSorting
        if (_sortAlphabeticalButton != null && _sortAlphabeticalButton.transform.parent != null)
            _sortAlphabeticalButton.transform.parent.gameObject.SetActive(_enableSorting);
        if (_sortIngredientCountButton != null && _sortIngredientCountButton.transform.parent != null)
            _sortIngredientCountButton.transform.parent.gameObject.SetActive(_enableSorting);
        if (_sortOutputTypeButton != null && _sortOutputTypeButton.transform.parent != null)
            _sortOutputTypeButton.transform.parent.gameObject.SetActive(_enableSorting);
        if (_reverseSortButton != null && _reverseSortButton.transform.parent != null)
            _reverseSortButton.transform.parent.gameObject.SetActive(_enableSorting);

        // Adjust content parent width and scroll view height when sorting is disabled
        if (_contentParent != null)
        {
            var contentParentRect = _contentParent.GetComponent<RectTransform>();
            if (contentParentRect != null)
            {
                contentParentRect.sizeDelta = _enableSorting ? new Vector2(contentParentRect.sizeDelta.x, contentParentRect.sizeDelta.y) : new Vector2(380, contentParentRect.sizeDelta.y);
            }
        }

        var scrollView = GetComponent<RectTransform>();
        if (scrollView != null)
        {
            scrollView.sizeDelta = _enableSorting ? new Vector2(scrollView.sizeDelta.x, scrollView.sizeDelta.y) : new Vector2(scrollView.sizeDelta.x, 775);
        }

        // Hide section headers if sorting is disabled
        if (!_enableSorting)
        {
            ClearSectionHeaders();
        }
    }

    private void OnDestroy()
    {
        UnbindRuntimeSortButtons();
    }

private void OnEnable()
{
    BindRuntimeSortButtons();

    if (CraftingManager.Instance == null) return;

    _onRecipeCraftedHandler = (recipe, craftedObj, isFirstTime) => OnRecipeCrafted(recipe, craftedObj, isFirstTime);
    CraftingManager.Instance.OnRecipeCrafted.AddListener(_onRecipeCraftedHandler);
}
    
    private void OnDisable()
    {
        UnbindRuntimeSortButtons();

        if (CraftingManager.Instance == null) return;
        if (_onRecipeCraftedHandler != null)
        CraftingManager.Instance.OnRecipeCrafted.RemoveListener(_onRecipeCraftedHandler);
    }

    private void BindRuntimeSortButtons()
    {
        if (_sortAlphabeticalButton != null) _sortAlphabeticalButton.onClick.AddListener(ToggleSortAlphabetical);
        if (_sortIngredientCountButton != null) _sortIngredientCountButton.onClick.AddListener(ToggleSortIngredientCount);
        if (_sortOutputTypeButton != null) _sortOutputTypeButton.onClick.AddListener(ToggleSortOutputType);
        if (_reverseSortButton != null) _reverseSortButton.onClick.AddListener(ToggleReverseSort);
        UpdateSortButtonVisuals();
    }

    private void UnbindRuntimeSortButtons()
    {
        if (_sortAlphabeticalButton != null) _sortAlphabeticalButton.onClick.RemoveListener(ToggleSortAlphabetical);
        if (_sortIngredientCountButton != null) _sortIngredientCountButton.onClick.RemoveListener(ToggleSortIngredientCount);
        if (_sortOutputTypeButton != null) _sortOutputTypeButton.onClick.RemoveListener(ToggleSortOutputType);
        if (_reverseSortButton != null) _reverseSortButton.onClick.RemoveListener(ToggleReverseSort);
    }

    private void UpdateSortButtonVisuals()
    {
        if (_sortAlphabeticalButton != null && _sortAlphabeticalButton.image != null)
            _sortAlphabeticalButton.image.color = _sortAlphabetical ? _sortButtonActiveColor : _sortButtonInactiveColor;
        if (_sortIngredientCountButton != null && _sortIngredientCountButton.image != null)
            _sortIngredientCountButton.image.color = _sortByIngredientCount ? _sortButtonActiveColor : _sortButtonInactiveColor;
        if (_sortOutputTypeButton != null && _sortOutputTypeButton.image != null)
            _sortOutputTypeButton.image.color = _sortByOutputType ? _sortButtonActiveColor : _sortButtonInactiveColor;
        if (_reverseSortButton != null && _reverseSortButton.image != null)
            _reverseSortButton.image.color = _reverseSort ? _sortButtonActiveColor : _sortButtonInactiveColor;
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
        _reverseSort = !_reverseSort;
        UpdateSortButtonVisuals();
        ScheduleRefresh();
    }
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
            _sortAlphabetical = name == "Alphabetical";
            _sortByIngredientCount = name == "IngredientCount";
            _sortByOutputType = name == "OutputType";
        }

        // Check current state
        bool isActive = (sortName == "Alphabetical" && _sortAlphabetical)
                        || (sortName == "IngredientCount" && _sortByIngredientCount)
                        || (sortName == "OutputType" && _sortByOutputType);

        if (!isActive)
        {
            // Activate this sort exclusively
            SetSort(sortName);
            return;
        }

        // It was active; toggle it off and choose a fallback in priority order
        // Fallback priority: IngredientCount -> OutputType -> Alphabetical
        if (sortName == "Alphabetical") _sortAlphabetical = false;
        else if (sortName == "IngredientCount") _sortByIngredientCount = false;
        else if (sortName == "OutputType") _sortByOutputType = false;

        // Pick fallback
        if (!_sortByIngredientCount && sortName != "IngredientCount")
        {
            // prefer ingredient count as fallback
            SetSort("IngredientCount");
            return;
        }

        if (!_sortByOutputType && sortName != "OutputType")
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
        if (_sortAlphabetical && (_sortByIngredientCount || _sortByOutputType))
        {
            _sortByIngredientCount = false;
            _sortByOutputType = false;
        }
        else if (_sortByIngredientCount && _sortByOutputType)
        {
            // keep ingredient count, disable output
            _sortByOutputType = false;
        }
        else if (!_sortAlphabetical && !_sortByIngredientCount && !_sortByOutputType)
        {
            // ensure at least one is active
            _sortAlphabetical = true;
        }
    }

    private void PopulateList()
    {
        if (_contentParent == null || _recipeItemPrefab == null)
        {
            Debug.LogWarning("RecipeTrackerUI: _contentParent or recipeItemPrefab not assigned.");
            return;
        }
        // Clear previous section headers (we'll recreate them below if needed)
        ClearSectionHeaders();

        // Build sorted/filtered recipe list according to current sort options
        var desiredRecipes = BuildFilteredRecipes();

        // Track which controllers we reuse this pass so we can cleanup the rest
        var usedControllers = new HashSet<CraftingRecipe>();
        string lastGroupKey = null;

        // Iterate desired recipes, create headers as needed and ensure an item controller exists
        foreach (var recipe in desiredRecipes)
        {
            string groupKey = ComputeGroupKey(recipe);

            if (_enableSorting && _addSectionSpacers && (lastGroupKey == null || groupKey != lastGroupKey))
            {
                CreateSectionHeader(groupKey);
            }

            EnsureControllerForRecipe(recipe, usedControllers);

            lastGroupKey = groupKey;
        }

        // Remove any controllers that were not used this pass
        CleanupUnusedControllers(usedControllers);
    }

    private List<CraftingRecipe> BuildFilteredRecipes()
    {
        // Start with all recipes
        var filteredRecipes = _recipes.Where(r => r.productType == CraftingRecipe.ProductType.Mineral).ToList();

        // If _showAllRecipes is false, pick 5 random recipes from the filtered list
        if (!_showAllRecipes && filteredRecipes.Count > 5)
        {
            filteredRecipes = filteredRecipes.OrderBy(r => Random.value).Take(5).ToList();
        }

        // Apply sorting
        return BuildSortedRecipes(filteredRecipes);
    }

    private List<CraftingRecipe> BuildSortedRecipes(List<CraftingRecipe> recipes)
    {
        IEnumerable<CraftingRecipe> sorted = recipes.Where(r => r != null);

        IOrderedEnumerable<CraftingRecipe> ordered = null;

        if (_sortByOutputType)
        {
            ordered = sorted.OrderBy(r => GetOutputTypeOrder(r)).ThenBy(r => GetRecipeSortKey(r));
        }

        if (_sortByIngredientCount)
        {
            if (ordered != null) ordered = ordered.ThenBy(r => CountIngredients(r));
            else ordered = sorted.OrderBy(r => CountIngredients(r)).ThenBy(r => GetRecipeSortKey(r));
        }

        if (_sortAlphabetical)
        {
            if (ordered != null) ordered = ordered.ThenBy(r => GetRecipeSortKey(r));
            else ordered = sorted.OrderBy(r => GetRecipeSortKey(r));
        }

        if (ordered != null) sorted = ordered;
        else sorted = sorted.OrderBy(r => GetRecipeSortKey(r));

        if (_reverseSort) sorted = sorted.Reverse();

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
        if (_sectionHeaderPrefab == null) return;
        var header = Instantiate(_sectionHeaderPrefab, _contentParent);
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
            existing.root.transform.SetParent(_contentParent, false);
            existing.root.transform.SetAsLastSibling();
            existing.root.SetActive(true);
            existing.Refresh(IsRecipeDiscovered(recipe));
            usedControllers.Add(recipe);
        }
        else
        {
            var go = Instantiate(_recipeItemPrefab, _contentParent);
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
        if (_contentParent == null) return;
        // Destroy all children safely (use while loop to avoid iterator issues)
        while (_contentParent.childCount > 0)
        {
            var c = _contentParent.GetChild(0);
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
        if (_sortByOutputType)
        {
            var t = GetOutputTypeLabel(recipe);
            return t ?? "";
        }
        else if (_sortByIngredientCount)
        {
            return CountIngredients(recipe).ToString();
        }
        else if (_sortAlphabetical)
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

    private void OnRecipeCrafted(CraftingRecipe recipe, GameObject craftedObj, bool isFirstTime)
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
            if (_selectedTitleText != null) _selectedTitleText.text = "";
            if (_selectedDetailsText != null) _selectedDetailsText.text = "";
            if (_selectedImage != null) { _selectedImage.sprite = null; _selectedImage.enabled = false; }
            return;
        }

        bool discovered = IsRecipeDiscovered(recipe);

        if (_selectedTitleText != null)
            _selectedTitleText.text = discovered ? SOHelpers.StripCommonPrefix(recipe.name) : _undiscoveredName;

        var ingredients = new List<ScriptableObject> { recipe.inputA, recipe.inputB, recipe.inputC, recipe.inputD, recipe.inputE, recipe.inputF, recipe.inputG, recipe.inputH }
            .Where(x => x != null)
            .ToList();

        string ingList = string.Join(", ", ingredients.Select(i => SOHelpers.StripCommonPrefix(GetDisplayName(i, discovered))));

        if (_selectedDetailsText != null)
        {
            if (discovered)
            {
                string output = SOHelpers.StripCommonPrefix(GetDisplayName(recipe.output, discovered));
                _selectedDetailsText.text = string.Format("Ingredients: {0}\nOutput: {1}", ingList, output);
                if (_selectedImage != null)
                {
                    var md = recipe.output as MineralData;
                    if (md != null && md.mineralBigSprite != null)
                    {
                        _selectedImage.sprite = md.mineralBigSprite;
                        _selectedImage.enabled = true;
                    }
                    else if (md != null && md.mineralSprite != null)
                    {
                        _selectedImage.sprite = md.mineralSprite;
                        _selectedImage.enabled = true;
                    }
                    else
                    {
                        _selectedImage.sprite = null;
                        _selectedImage.enabled = false;
                    }
                }
            }
            else
            {
                _selectedDetailsText.text = string.Format("Ingredients: {0}", ingList);
                if (_selectedImage != null) { _selectedImage.sprite = null; _selectedImage.enabled = false; }
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
            SafeSetTitle(discovered ? SOHelpers.StripCommonPrefix(recipe.name) : parent._undiscoveredName);

            // apply tint to title text only
            if (titleTextTMP != null) titleTextTMP.color = discovered ? parent._discoveredColor : parent._undiscoveredColor;
            if (titleTextUI != null) titleTextUI.color = discovered ? parent._discoveredColor : parent._undiscoveredColor;
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
