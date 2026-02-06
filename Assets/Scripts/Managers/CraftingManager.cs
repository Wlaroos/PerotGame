using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

// Handles all crafting logic in the game
public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; } // Singleton

    public List<CraftingRecipe> _recipes = new List<CraftingRecipe>(); // All recipes

    // Keeps track of recipes crafted at least once
    private HashSet<CraftingRecipe> _craftedRecipes = new HashSet<CraftingRecipe>();

    // Event fired on every successful craft.
    // Args: (CraftingRecipe recipe, GameObject craftedObject, bool isFirstTime)
    [System.Serializable]
    public class RecipeCraftedEvent : UnityEvent<CraftingRecipe, GameObject, bool> { }
    public RecipeCraftedEvent OnRecipeCrafted = new RecipeCraftedEvent();

    [Header("Prefabs")]
    [SerializeField] private GameObject elementPrefab;   // Prefab for new elements
    [SerializeField] private GameObject compoundPrefab;  // Prefab for new compounds
    [SerializeField] private GameObject mineralPrefab;   // Prefab for new minerals

    private void Awake()
    {
        // Set up singleton
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        // Load all recipes from Resources/SOs/recipes
        _recipes.Clear();
        CraftingRecipe[] loadedRecipes = Resources.LoadAll<CraftingRecipe>("SOs/recipes");
        _recipes.AddRange(loadedRecipes);
    }

    // Find a matching recipe for given ingredients without creating/ registering the craft
    public CraftingRecipe FindMatchingRecipe(List<ScriptableObject> ingredients)
    {
        if (ingredients == null) return null;
        foreach (var recipe in _recipes)
        {
            if (MatchIngredients(recipe, ingredients))
                return recipe;
        }
        return null;
    }

    // Try to craft something from a list of ingredients (performs creation and registers first-time events)
    public GameObject TryCraft(List<ScriptableObject> ingredients, Vector3 spawnPosition)
    {
        var recipe = FindMatchingRecipe(ingredients);
        if (recipe == null) return null;

        // Determine if this is the first time this recipe was crafted
        bool isFirstTime = !_craftedRecipes.Contains(recipe);
        if (isFirstTime)
        {
            _craftedRecipes.Add(recipe);
        }

        // Instantiate the crafted object
        var crafted = CreateCraftedObject(recipe.output, spawnPosition);

        // If crafted object exists, show appropriate popup depending on output type.
        if (crafted != null && CraftedPopupManager.Instance != null)
        {
            Vector3 pos = crafted.transform != null ? crafted.transform.position : spawnPosition;

            // Minerals -> normal big/transient popups (existing behavior)
            if (recipe.output is MineralData mineralData)
            {
                if (isFirstTime)
                    CraftedPopupManager.Instance.ShowPersistentCraftedPopup(mineralData, recipe);
                else
                    CraftedPopupManager.Instance.ShowCraftedPopup(mineralData, pos, recipe);
            }
            // Elements -> use small popups (persistent on first time, transient otherwise)
            else if (recipe.output is ElementData elementData)
            {
                if (isFirstTime)
                    CraftedPopupManager.Instance.ShowPersistentCraftedPopup(elementData, recipe);
                else
                    CraftedPopupManager.Instance.ShowCraftedPopup(elementData, pos, recipe);
            }
            // Compounds -> use small popups (persistent on first time, transient otherwise)
            else if (recipe.output is CompoundData compoundData)
            {
                if (isFirstTime)
                    CraftedPopupManager.Instance.ShowPersistentCraftedPopup(compoundData, recipe);
                else
                    CraftedPopupManager.Instance.ShowCraftedPopup(compoundData, pos, recipe);
            }
        }

        // Fire the unified "successful craft" event for other scripts to hook into
        if (crafted != null)
        {
            OnRecipeCrafted?.Invoke(recipe, crafted, isFirstTime);
        }

        return crafted;
    }

    // Check if the recipe matches the given ingredients
    private bool MatchIngredients(CraftingRecipe recipe, List<ScriptableObject> ingredients)
    {
        // Filter out null values from the recipe's ingredients
        var recipeIngredients = new List<ScriptableObject> { recipe.inputA, recipe.inputB, recipe.inputC, recipe.inputD, recipe.inputE, recipe.inputF, recipe.inputG, recipe.inputH }
            .Where(ingredient => ingredient != null)
            .ToList();

        // Check if the counts of each ingredient match
        if (recipeIngredients.Count != ingredients.Count)
            return false;

        // Group and count occurrences of each ingredient in the recipe
        var recipeCounts = recipeIngredients.GroupBy(i => i).ToDictionary(g => g.Key, g => g.Count());

        // Group and count occurrences of each ingredient in the provided ingredients
        var ingredientCounts = ingredients.GroupBy(i => i).ToDictionary(g => g.Key, g => g.Count());

        // Compare the counts for each ingredient
        foreach (var kvp in recipeCounts)
        {
            if (!ingredientCounts.TryGetValue(kvp.Key, out int count) || count != kvp.Value)
            {
                return false; // Mismatch in ingredient counts
            }
        }

        return true; // All ingredients match
    }

    private GameObject CreateCraftedObject(ScriptableObject result, Vector3 spawnPosition)
    {
        GameObject craftedObj = null;

        if (result is MineralData mineralData)
        {
            craftedObj = Instantiate(mineralPrefab, spawnPosition, Quaternion.identity);
            Mineral mineralComponent = craftedObj.GetComponent<Mineral>();
            if (mineralComponent != null)
            {
                mineralComponent.data = mineralData;
                mineralComponent.UpdateDataVisuals();
            }
        }
        else if (result is CompoundData compoundData)
        {
            craftedObj = Instantiate(compoundPrefab, spawnPosition, Quaternion.identity);
            Compound compoundComponent = craftedObj.GetComponent<Compound>();
            if (compoundComponent != null)
            {
                compoundComponent.data = compoundData;
                compoundComponent.UpdateDataVisuals();
            }
        }
        else if (result is ElementData elementData)
        {
            craftedObj = Instantiate(elementPrefab, spawnPosition, Quaternion.identity);
            Element elementComponent = craftedObj.GetComponent<Element>();
            if (elementComponent != null)
            {
                elementComponent.data = elementData;
                elementComponent.isotopeNumber = elementData.defaultIsotopeNumber;
                elementComponent.UpdateDataVisuals();
            }
        }

        // Add the crafted object to the DraggableHolder
        if (craftedObj != null && DraggableHolder.Instance != null)
        {
            DraggableHolder.Instance.AddDraggable(craftedObj);
        }

        // Play crafting effect using the crafted object's color
        Color color = Color.white;
        if (craftedObj != null)
        {
            var sr = craftedObj.GetComponent<SpriteRenderer>();
            if (sr != null) color = sr.color;
        }
        EffectManager.Instance.PlayCraftEffect(spawnPosition, color);

        return craftedObj;
    }
}