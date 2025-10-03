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

    // Try to craft something from two ingredients
    public GameObject TryCraft(ScriptableObject a, ScriptableObject b, Vector3 spawnPosition)
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

                // Instantiate the crafted object
                return CreateCraftedObject(recipe.output, spawnPosition);
            }
        }
        return null; // No match found
    }

    // Creates the crafted object based on the recipe output
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