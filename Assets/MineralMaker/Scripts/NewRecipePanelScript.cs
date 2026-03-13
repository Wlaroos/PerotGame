using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public class NewRecipePanelScript : MonoBehaviour
{
    [Header("Mineral Choice (Optional)")]
    [SerializeField] private bool _useMineralChoice = false;
    [SerializeField] private GameObject _mineralChoiceRoot;
    [SerializeField] private Image[] _mineralChoiceHolders;
    [SerializeField] private GameObject _recipeSelectorRef;
    [Space(10)]
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
    private int _craftedMineralCount = 0; // Track the number of crafted minerals
    [SerializeField] private GameObject _winCanvasPrefab;

    // Mineral-choice internal state
    private readonly HashSet<CraftingRecipe> _chosenRecipes = new HashSet<CraftingRecipe>();
    private List<CraftingRecipe> _choiceRecipes = new List<CraftingRecipe>();
    private Image[] _mineralChoiceBGs;
    private TextMeshProUGUI[] _mineralNameTexts;
    private Image[] _mineralImages;

    private void Start()
    {
        if (CraftingManager.Instance == null)
        {
            Debug.LogError("RecipeTrackerUI: CraftingManager.Instance is null. Make sure CraftingManager exists in the scene.");
            return;
        }

        _recipes = CraftingManager.Instance._recipes.ToList();

        if (_recipeSelectorRef != null)
        {
            _recipeSelectorRef.SetActive(!_useMineralChoice);
        }

        if (_mineralChoiceRoot != null)
        {
            _mineralChoiceRoot.SetActive(_useMineralChoice);
        }

        _filteredRecipes = BuildFilteredRecipes();
        _craftedStatus = new bool[_filteredRecipes.Count];

        UpdateUI();

        if (_useMineralChoice)
        {
            CacheMineralChoiceUIRefs();
            ShowMineralChoice();
            
        }

        _onRecipeCraftedHandler = (recipe, craftedObj, isFirstTime) => OnRecipeCrafted(recipe, craftedObj, isFirstTime);
        CraftingManager.Instance.OnRecipeCrafted.AddListener(_onRecipeCraftedHandler);
    }

    private void OnDisable()
    {
        CraftingManager.Instance.OnRecipeCrafted.RemoveListener(_onRecipeCraftedHandler);
    }

    private void CacheMineralChoiceUIRefs()
    {
        if (_mineralChoiceHolders == null || _mineralChoiceHolders.Length == 0) return;

        _mineralChoiceBGs = _mineralChoiceHolders
            .Select(img => img != null ? img.transform.GetChild(0).GetComponent<Image>() : null)
            .ToArray();

        _mineralNameTexts = _mineralChoiceBGs
            .Select(img => img != null ? img.transform.GetChild(0).GetComponent<TextMeshProUGUI>() : null)
            .ToArray();

        _mineralImages = _mineralChoiceBGs
            .Select(img => img != null ? img.transform.GetChild(1).GetComponent<Image>() : null)
            .ToArray();
    }

    private void ShowMineralChoice()
    {
        if (!_useMineralChoice) return;
        if (_mineralChoiceRoot != null) _mineralChoiceRoot.SetActive(true);
        PopulateMineralChoice();
    }

    private void HideMineralChoice()
    {
        if (_mineralChoiceRoot != null) _mineralChoiceRoot.SetActive(false);
    }

    private void PopulateMineralChoice()
    {
        if (!_useMineralChoice) return;
        if (CraftingManager.Instance == null || CraftingManager.Instance._recipes == null)
        {
            Debug.LogError("No recipes found in the CraftingManager.");
            return;
        }

        if (_mineralChoiceHolders == null || _mineralChoiceHolders.Length == 0)
        {
            Debug.LogError("MineralChoice is enabled, but no holders are assigned.");
            return;
        }

        if (_mineralNameTexts == null || _mineralImages == null)
        {
            CacheMineralChoiceUIRefs();
        }

        _choiceRecipes = CraftingManager.Instance._recipes
            .Where(r => r.productType == CraftingRecipe.ProductType.Mineral && !_chosenRecipes.Contains(r))
            .OrderBy(_ => Random.value)
            .Take(3)
            .ToList();

        if (_choiceRecipes.Count == 0)
        {
            Debug.LogError("No recipes available after filtering.");
            return;
        }

        for (int i = 0; i < _mineralChoiceHolders.Length; i++)
        {
            bool hasRecipe = i < _choiceRecipes.Count;

            if (_mineralNameTexts != null && i < _mineralNameTexts.Length && _mineralNameTexts[i] != null)
                _mineralNameTexts[i].gameObject.SetActive(hasRecipe);
            if (_mineralImages != null && i < _mineralImages.Length && _mineralImages[i] != null)
                _mineralImages[i].gameObject.SetActive(hasRecipe);

            if (!hasRecipe) continue;

            SetupMineralUI(i, _choiceRecipes[i]);
        }
    }

    private void SetupMineralUI(int index, CraftingRecipe recipe)
    {
        if (recipe == null) return;
        if (_mineralNameTexts == null || _mineralImages == null) return;
        if (index < 0 || index >= _mineralChoiceHolders.Length) return;
        if (index >= _mineralNameTexts.Length || index >= _mineralImages.Length) return;
        if (_mineralNameTexts[index] == null || _mineralImages[index] == null) return;

        _mineralNameTexts[index].text = SOHelpers.GetFullStrippedName(recipe.output);

        _mineralImages[index].sprite = SOHelpers.GetBigSpriteFromData(recipe.output);
        _mineralImages[index].preserveAspect = true;
        _mineralImages[index].color = Color.white;

        var parentParentImage = _mineralImages[index].transform.parent.parent.GetComponent<Image>();
        if (parentParentImage != null)
        {
            parentParentImage.color = SOHelpers.GetColorFromData(recipe.output);
        }

        var parentImage = _mineralImages[index].transform.parent.GetComponent<Image>();
        if (parentImage != null)
        {
            parentImage.color = SOHelpers.GetColorFromData(recipe.output) * new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        var button = _mineralChoiceHolders[index].GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectMineralChoice(recipe));

            var colors = button.colors;
            colors.highlightedColor = SOHelpers.GetColorFromData(recipe.output) + new Color(0.5f, 0.5f, 0.5f, 1f);
            colors.pressedColor = SOHelpers.GetColorFromData(recipe.output) * new Color(0.8f, 0.8f, 0.8f, 1f);
            button.colors = colors;
        }
    }

    private void SelectMineralChoice(CraftingRecipe selectedRecipe)
    {
        if (!_useMineralChoice) return;
        if (selectedRecipe == null) return;

        Debug.Log($"Selected recipe: {SOHelpers.GetFullStrippedName(selectedRecipe.output)}");
        _chosenRecipes.Add(selectedRecipe);

        ReplaceCurrentRecipe(selectedRecipe);
        HideMineralChoice();
    }

    private void ReplaceCurrentRecipe(CraftingRecipe selectedRecipe)
    {
        if (selectedRecipe == null) return;
        if (!_recipes.Contains(selectedRecipe)) return;
        if (_filteredRecipes == null || _filteredRecipes.Count == 0) return;
        if (_currentRecipeIndex < 0 || _currentRecipeIndex >= _filteredRecipes.Count) return;

        int otherIndex = _filteredRecipes.IndexOf(selectedRecipe);
        if (otherIndex >= 0 && otherIndex != _currentRecipeIndex)
        {
            (_filteredRecipes[_currentRecipeIndex], _filteredRecipes[otherIndex]) =
                (_filteredRecipes[otherIndex], _filteredRecipes[_currentRecipeIndex]);

            if (_craftedStatus != null &&
                otherIndex < _craftedStatus.Length &&
                _currentRecipeIndex < _craftedStatus.Length)
            {
                (_craftedStatus[_currentRecipeIndex], _craftedStatus[otherIndex]) =
                    (_craftedStatus[otherIndex], _craftedStatus[_currentRecipeIndex]);
            }
        }
        else
        {
            _filteredRecipes[_currentRecipeIndex] = selectedRecipe;
        }

        UpdateUI();
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

                if (_currentRecipeIndex >= _filteredRecipes.Count)
                {
                    _currentRecipeIndex = Mathf.Clamp(_filteredRecipes.Count - 1, 0, _filteredRecipes.Count - 1);
                }

                if (_useMineralChoice)
                {
                    ShowMineralChoice();
                }
            }
            else
            {
                EndGame();
            }
        }

        UpdateUI();
    }

    private void EndGame()
    {
        _winCanvasPrefab.SetActive(true);

        HideMineralChoice();
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
}