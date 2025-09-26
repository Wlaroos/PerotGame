using System.Collections.Generic;
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

    // Try to craft something from two ingredients
    public ScriptableObject TryCraft(ScriptableObject a, ScriptableObject b)
    {
        foreach (var recipe in _recipes)
        {
            // Check if the two ingredients match (order doesn't matter)
            if ((recipe.inputA == a && recipe.inputB == b) || (recipe.inputA == b && recipe.inputB == a))
            {
                // If this is the first time, fire the event
                if (!_craftedRecipes.Contains(recipe))
                {
                    _craftedRecipes.Add(recipe);
                    OnFirstTimeRecipeCrafted.Invoke(recipe);
                }
                return recipe.output; // Return the result
            }
        }
        return null; // No match found
    }
}