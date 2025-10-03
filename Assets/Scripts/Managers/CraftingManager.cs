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

    // Event for when a recipe is crafted for the first time
    [System.Serializable]
    public class RecipeCraftedEvent : UnityEvent<CraftingRecipe> { }
    public RecipeCraftedEvent OnFirstTimeRecipeCrafted = new RecipeCraftedEvent();

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

    // Try to craft something from a list of ingredients
    public GameObject TryCraft(List<ScriptableObject> ingredients, Vector3 spawnPosition)
    {
        foreach (var recipe in _recipes)
        {
            // Check if the recipe matches the ingredients (ignoring order)
            if (MatchIngredients(recipe, ingredients))
            {
                // If this is the first time, fire the event
                if (!_craftedRecipes.Contains(recipe))
                {
                    _craftedRecipes.Add(recipe);
                    OnFirstTimeRecipeCrafted.Invoke(recipe);
                }

                // Instantiate the crafted object
                return CreateCraftedObject(recipe.output, spawnPosition);
            }
        }
        return null;
    }

    // Check if the recipe matches the given ingredients
    private bool MatchIngredients(CraftingRecipe recipe, List<ScriptableObject> ingredients)
    {
        // Filter out null values from the recipe's ingredients
        var recipeIngredients = new List<ScriptableObject> { recipe.inputA, recipe.inputB, recipe.inputC, recipe.inputD, recipe.inputE }
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