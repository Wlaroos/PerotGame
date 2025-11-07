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

    // tint colors
    public Color undiscoveredColor = Color.white;
    public Color discoveredColor = Color.green;

    // in-memory lookup
    private List<CraftingRecipe> _recipes = new List<CraftingRecipe>();
    private Dictionary<CraftingRecipe, RecipeItemController> _itemControllers = new Dictionary<CraftingRecipe, RecipeItemController>();

    // non-persistent discovery set (resets each run)
    private HashSet<string> _discoveredRecipes = new HashSet<string>();

    private void Start()
    {
        _discoveredRecipes.Clear(); // ensure reset each session

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
    }

    private void PopulateList()
    {
        if (contentParent == null || recipeItemPrefab == null)
        {
            Debug.LogWarning("RecipeTrackerUI: contentParent or recipeItemPrefab not assigned.");
            return;
        }

        // Clear existing
        foreach (Transform t in contentParent)
            Destroy(t.gameObject);

        _itemControllers.Clear();

        foreach (var recipe in _recipes)
        {
            var go = Instantiate(recipeItemPrefab, contentParent);
            var controller = new RecipeItemController(go, recipe, this);
            _itemControllers.Add(recipe, controller);
            controller.Refresh(IsRecipeDiscovered(recipe));
        }
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
    
    // Helper: remove common asset prefixes (E_, R_, M_, C_)
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
            selectedTitleText.text = discovered ? StripCommonPrefix(recipe.name) : "Undiscovered Recipe";

        // Build details: ingredients -> output (output only shown if recipe is discovered)
        var ingredients = new List<ScriptableObject> { recipe.inputA, recipe.inputB, recipe.inputC, recipe.inputD, recipe.inputE }
            .Where(x => x != null)
            .ToList();

        string ingList = string.Join(", ", ingredients.Select(i => StripCommonPrefix(GetDisplayName(i, discovered))));

        if (selectedDetailsText != null)
        {
            if (discovered)
            {
                string output = StripCommonPrefix(GetDisplayName(recipe.output, discovered));
                selectedDetailsText.text = string.Format("Ingredients: {0}\nOutput: {1}", ingList, output);
                // If the output is a MineralData, show its sprite in the selected image (prefer big sprite)
                if (selectedImage != null)
                {
                    if (recipe.output is MineralData md && md.mineralBigSprite != null)
                    {
                        selectedImage.sprite = md.mineralBigSprite;
                        selectedImage.enabled = true;
                    }
                    else if (recipe.output is MineralData md2 && md2.mineralSprite != null)
                    {
                        selectedImage.sprite = md2.mineralSprite;
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

    // Get a short symbol for an asset (tries mapping then falls back to abbreviation)
    private string GetSymbolForScriptableObject(ScriptableObject so)
    {
        if (so == null) return "?";
        string name = StripCommonPrefix(so.name); // asset name (E_Iron -> Iron, R_Pyrite -> Pyrite)
    
        // basic mapping for common elements (expandable)
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
            { "Iron", "Fe"},
            {"Copper", "Cu"},
            {"Barium", "Ba"},
            {"Oxygen", "O"},
            {"Carbonate", "CO3"},
            {"Sulfate", "SO4"},
            {"Nitrate", "NO3"},
            {"Phosphate", "PO4"},
            {"Silicate", "SiO4"},
            {"Oxide", "O2-"},

        };

        if (map.TryGetValue(name, out var sym)) return sym;
    
        // fallback: take first 1-2 letters (capitalized first)
        if (name.Length == 1) return name.ToUpper();
        return char.ToUpper(name[0]) + (char.IsUpper(name[1]) ? name[1].ToString().ToLower() : name[1].ToString().ToLower());
    }

    // Used to show names or mask them for undiscovered
    private string GetDisplayName(ScriptableObject so, bool discovered)
    {
        if (so == null) return "";
        if (discovered) return so.name;
        // Undiscovered objects still show their symbol so the player knows the recipe shape
        return GetSymbolForScriptableObject(so);
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
            SafeSetTitle(parent.StripCommonPrefix(recipe.name));
        }

        public void Refresh(bool discovered)
        {
            // Title: if undiscovered show 'Undiscovered Recipe', otherwise show stripped recipe name
            SafeSetTitle(discovered ? parent.StripCommonPrefix(recipe.name) : "Undiscovered Recipe");

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
