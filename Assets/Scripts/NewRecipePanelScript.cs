using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class NewRecipePanelScript : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _detailsText;
    [SerializeField] private Image[] _ingredientImages; 
    [SerializeField] private Image _productImage;
    [SerializeField] private Image _productBigImage;
    private List<CraftingRecipe> _recipes = new List<CraftingRecipe>();
    private List<CraftingRecipe> _filteredRecipes = new List<CraftingRecipe>();

    private void Start()
    {
        if (CraftingManager.Instance == null)
        {
            Debug.LogError("RecipeTrackerUI: CraftingManager.Instance is null. Make sure CraftingManager exists in the scene.");
            return;
        }

        _recipes = CraftingManager.Instance._recipes.ToList();
        _filteredRecipes = BuildFilteredRecipes();

        UpdateUI();
    }

    private List<CraftingRecipe> BuildFilteredRecipes()
    {
        // Start with all recipes
        var filteredRecipes = _recipes.Where(r => r.productType == CraftingRecipe.ProductType.Mineral).ToList();

        // Pick 1 random recipe from the filtered list
        filteredRecipes = filteredRecipes.OrderBy(r => Random.value).Take(1).ToList();

        return(filteredRecipes);
    }

    private void UpdateUI()
    {
        if (_filteredRecipes.Count == 0)
        {
            Debug.LogWarning("No recipes available to display.");
            return;
        }

        var recipe = _filteredRecipes[0];

        // Update title and details
        _titleText.text = SOHelpers.GetFullStrippedName(recipe.output);
        _detailsText.text = SOHelpers.GetDescriptionFromData(recipe.output);
        if (string.IsNullOrEmpty(_detailsText.text))
        {
            _detailsText.text = "No description available.";
        }

        // Update ingredient images
        ScriptableObject[] inputs = { recipe.inputA, recipe.inputB, recipe.inputC, recipe.inputD, recipe.inputE, recipe.inputF, recipe.inputG, recipe.inputH };
        for (int i = 0; i < _ingredientImages.Length; i++)
        {
            if (i < inputs.Length && inputs[i] != null)
            {
                _ingredientImages[i].sprite = SOHelpers.GetPrimarySpriteFromData(inputs[i]);
                _ingredientImages[i].gameObject.SetActive(true);
            }
            else
            {
                _ingredientImages[i].gameObject.SetActive(false);
            }
        }

        // Update product image
        _productImage.sprite = SOHelpers.GetPrimarySpriteFromData(recipe.output);
        _productBigImage.sprite = SOHelpers.GetBigSpriteFromData(recipe.output);
    }
}