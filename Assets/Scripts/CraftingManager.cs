using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    public List<CraftingRecipe> _recipes = new List<CraftingRecipe>();

    // Track which recipes have been crafted at least once
    private HashSet<CraftingRecipe> _craftedRecipes = new HashSet<CraftingRecipe>();

    // Event that fires the first time a recipe is crafted
    [System.Serializable]
    public class RecipeCraftedEvent : UnityEvent<CraftingRecipe> { }
    public RecipeCraftedEvent OnFirstTimeRecipeCrafted = new RecipeCraftedEvent();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        // Auto-fill recipes list from Resources/SOs/recipes
        _recipes.Clear();
        CraftingRecipe[] loadedRecipes = Resources.LoadAll<CraftingRecipe>("SOs/recipes");
        _recipes.AddRange(loadedRecipes);
    }

    public ScriptableObject TryCraft(ScriptableObject a, ScriptableObject b)
    {
        foreach (var recipe in _recipes)
        {
            if ((recipe.inputA == a && recipe.inputB == b) || (recipe.inputA == b && recipe.inputB == a))
            {
                // Fire first-time event if not already crafted
                if (!_craftedRecipes.Contains(recipe))
                {
                    _craftedRecipes.Add(recipe);
                    OnFirstTimeRecipeCrafted.Invoke(recipe);
                }
                return recipe.output;
            }
        }
        return null;
    }
}