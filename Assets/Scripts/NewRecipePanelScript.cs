using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class NewRecipePanelScript : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _detailsText;
    [SerializeField] private Image[] _ingredientImages; 
    [SerializeField] private Image _productImage;
    [SerializeField] private Image _productBigImage;
    [SerializeField] private int _numberOfRecipesToShow = 3;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _previousButton;
    [SerializeField] private Image[] _selectedCraftPips;
    [SerializeField] private Image[] _successfulCraftPips;
    private List<CraftingRecipe> _recipes = new List<CraftingRecipe>();
    private List<CraftingRecipe> _filteredRecipes = new List<CraftingRecipe>();
    private int _currentRecipeIndex = 0;
    private int _successfulCraftCount = 0;
    private UnityAction<CraftingRecipe, GameObject, bool> _onRecipeCraftedHandler;
    private bool[] _craftedStatus; // Tracks which recipes have been crafted at least once
    [SerializeField] private MineralChoice _mineralChoice; // Reference to MineralChoice
    private int _craftedMineralCount = 0; // Track the number of crafted minerals
    [SerializeField] private GameObject _winCanvasPrefab;

    private void Start()
    {
        if (CraftingManager.Instance == null)
        {
            Debug.LogError("RecipeTrackerUI: CraftingManager.Instance is null. Make sure CraftingManager exists in the scene.");
            return;
        }

        _recipes = CraftingManager.Instance._recipes.ToList();

        if (_mineralChoice != null && _mineralChoice.UseMineralChoice)
        {
            _mineralChoice.OnRecipeSelected.AddListener(SetFirstRecipeFromMineralChoice);
        }

        _filteredRecipes = BuildFilteredRecipes();
        _craftedStatus = new bool[_filteredRecipes.Count];

        UpdateUI();
    }

    private void OnEnable()
    {
        if (CraftingManager.Instance == null) return;

        _onRecipeCraftedHandler = (recipe, craftedObj, isFirstTime) => OnRecipeCrafted(recipe, craftedObj, isFirstTime);
        CraftingManager.Instance.OnRecipeCrafted.AddListener(_onRecipeCraftedHandler);
    }

    private void OnDisable()
    {
        if (CraftingManager.Instance == null) return;
        CraftingManager.Instance.OnRecipeCrafted.RemoveListener(_onRecipeCraftedHandler);
    }

    private void SetFirstRecipeFromMineralChoice(CraftingRecipe selectedRecipe)
    {
        if (_recipes.Contains(selectedRecipe))
        {
            _filteredRecipes[_currentRecipeIndex] = selectedRecipe; // Replace the current recipe with the selected one
            _filteredRecipes = _filteredRecipes.Distinct().ToList(); // Ensure no duplicates
            UpdateUI();
        }
    }

    private List<CraftingRecipe> BuildFilteredRecipes()
    {
        // Start with all recipes
        var filteredRecipes = _recipes.Where(r => r.productType == CraftingRecipe.ProductType.Mineral).ToList();

        // Pick 1 random recipe from the filtered list
        filteredRecipes = filteredRecipes.OrderBy(r => Random.value).Take(_numberOfRecipesToShow).ToList();

        return(filteredRecipes);
    }

    private void UpdateIngredientImages(ScriptableObject[] inputs)
    {
        for (int i = 0; i < _ingredientImages.Length; i++)
        {
            if (i < inputs.Length && inputs[i] != null)
            {
                _ingredientImages[i].sprite = SOHelpers.GetPrimarySpriteFromData(inputs[i]);
                _ingredientImages[i].color = SOHelpers.GetColorFromData(inputs[i]);
                _ingredientImages[i].gameObject.SetActive(true);
            }
            else
            {
                _ingredientImages[i].gameObject.SetActive(false);
            }
        }
    }

    private void UpdateUI()
    {
        if (_filteredRecipes.Count == 0)
        {
            Debug.LogWarning("No recipes available to display.");
            return;
        }

        var recipe = _filteredRecipes[_currentRecipeIndex];

        // Update title and details
        _titleText.text = SOHelpers.GetFullStrippedName(recipe.output);
        _detailsText.text = SOHelpers.GetDescriptionFromData(recipe.output);
        if (string.IsNullOrEmpty(_detailsText.text))
        {
            _detailsText.text = "No description available.";
        }

        // Update ingredient images
        ScriptableObject[] inputs = { recipe.inputA, recipe.inputB, recipe.inputC, recipe.inputD, recipe.inputE, recipe.inputF, recipe.inputG, recipe.inputH };
        UpdateIngredientImages(inputs);

        // Update product image
        _productImage.sprite = SOHelpers.GetPrimarySpriteFromData(recipe.output);
        _productImage.color = SOHelpers.GetColorFromData(recipe.output);
        _productBigImage.sprite = SOHelpers.GetBigSpriteFromData(recipe.output);

        if(_craftedStatus[_currentRecipeIndex])
        {
            _productImage.enabled = true;
            _productBigImage.enabled = true;
        }
        else
        {
            _productImage.enabled = false;
            _productBigImage.enabled = false;
        }

        UpdateCraftPips();
    }

    private void UpdateCraftPips()
    {
        for (int i = 0; i < _selectedCraftPips.Length; i++)
        {
            _selectedCraftPips[i].color = (i == _currentRecipeIndex) ? Color.red : Color.white;
        }
    }

    private void UpdateSuccessfulCraftPips()
    {
        _successfulCraftCount++;
        for (int i = 0; i < _successfulCraftPips.Length; i++)
        {
            _successfulCraftPips[i].color = (i < _successfulCraftCount) ? Color.green : Color.gray;
        }
    }

    private void OnRecipeCrafted(CraftingRecipe recipe, GameObject craftedObj, bool isFirstTime)
    {
        if (_filteredRecipes.Contains(recipe) && isFirstTime)
        {
            UpdateSuccessfulCraftPips();
            _craftedStatus[_currentRecipeIndex] = true;

            // Increment crafted mineral count and check if the loop should continue
            _craftedMineralCount++;
            if (_craftedMineralCount < 3)
            {
                _currentRecipeIndex += 1;

                ResetMineralChoice();
            }
            else
            {
                EndGame();
            }
        }

        UpdateUI();
    }

    private void ResetMineralChoice()
    {
        _mineralChoice.gameObject.SetActive(true); // Reactivate the MineralChoice canvas
        _mineralChoice.ResetMineralChoice();
    }

    private void EndGame()
    {
        _winCanvasPrefab.SetActive(true);

        _mineralChoice.gameObject.SetActive(false);
        this.enabled = false;
    }

    public void ShowNextRecipe()
    {
        if (_filteredRecipes.Count == 0) return;

        _currentRecipeIndex = (_currentRecipeIndex + 1) % _filteredRecipes.Count;
        UpdateUI();
    }

    public void ShowPreviousRecipe()
    {
        if (_filteredRecipes.Count == 0) return;

        _currentRecipeIndex = (_currentRecipeIndex - 1 + _filteredRecipes.Count) % _filteredRecipes.Count;
        UpdateUI();
    }

    public void DisplaySelectedRecipe(CraftingRecipe selectedRecipe)
    {
        if (_recipes.Contains(selectedRecipe))
        {
            _filteredRecipes[_currentRecipeIndex] = selectedRecipe;
            _filteredRecipes = _filteredRecipes.Distinct().ToList(); // Ensure no duplicates
            UpdateUI();
        }
    }
}