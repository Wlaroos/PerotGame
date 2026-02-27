using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.Events;
using System.Collections.Generic; // Added for List support
using System.Collections.Specialized; // Added for HashSet support

public class MineralChoice : MonoBehaviour
{
    [SerializeField] private bool _useMineralChoice = false;
    public bool UseMineralChoice => _useMineralChoice; // Public getter for external access
    [SerializeField] private Image[] _mineralChoiceHolders;
    private List<CraftingRecipe> _filteredRecipes;
    private Image[] _mineralChoiceBGs;
    private TextMeshProUGUI[] _mineralNameTexts; // UI Texts for recipe names
    private Image[] _mineralImages; // UI Images for recipes

    [HideInInspector] public UnityEvent<CraftingRecipe> OnRecipeSelected = new UnityEvent<CraftingRecipe>(); // Event to send recipe data on click
    [SerializeField] private NewRecipePanelScript _newRecipePanel; // Reference to the NewRecipePanelScript
    [SerializeField] private GameObject _recipeSelectorRef;

    private HashSet<CraftingRecipe> _chosenRecipes = new HashSet<CraftingRecipe>(); // Track chosen recipes

    private void Start() 
    {
        if(!_useMineralChoice)
        {
            _recipeSelectorRef.SetActive(true);
            gameObject.SetActive(false);
        }
        else
        {
            ResetMineralChoice();
        }

        // Load all recipes from the CraftingManager
        if (CraftingManager.Instance == null || CraftingManager.Instance._recipes == null)
        {
            Debug.LogError("No recipes found in the CraftingManager.");
            return;
        }
    }

    private void SetupMineralUI(int index, CraftingRecipe recipe)
    {
        // Set the recipe name
        _mineralNameTexts[index].text = SOHelpers.GetFullStrippedName(recipe.output);

        // Set the recipe image while maintaining the original aspect ratio
        _mineralImages[index].sprite = SOHelpers.GetPrimarySpriteFromData(recipe.output);
        _mineralImages[index].preserveAspect = true;

        // Set the parent's parent image color to the recipe's color
        var parentParentImage = _mineralImages[index].transform.parent.parent.GetComponent<Image>();
        if (parentParentImage != null)
        {
            parentParentImage.color = SOHelpers.GetColorFromData(recipe.output);
        }

        // Set the parent's image color to a slightly darker version of the recipe's color
        var parentImage = _mineralImages[index].transform.parent.GetComponent<Image>();
        if (parentImage != null)
        {
            parentImage.color = SOHelpers.GetColorFromData(recipe.output) * new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        // Ensure the recipe image is visible
        _mineralImages[index].color = Color.white;

        // Add button functionality
        var button = _mineralChoiceHolders[index].GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners(); // Clear previous listeners
            button.onClick.AddListener(() => OnRecipeSelected.Invoke(recipe));

            // Add hover effects
            var colors = button.colors;
            colors.highlightedColor = SOHelpers.GetColorFromData(recipe.output) + new Color(0.5f, 0.5f, 0.5f, 1f); // Closer to white on hover
            colors.pressedColor = SOHelpers.GetColorFromData(recipe.output) * new Color(0.8f, 0.8f, 0.8f, 1f); // Slightly darker on click
            button.colors = colors;
        }
    }

    private void OnEnable()
    {
        if(_useMineralChoice)
        {
            OnRecipeSelected.AddListener(HandleRecipeSelected);
        }
    }

    private void OnDisable()
    {
        if(_useMineralChoice)
        {
            OnRecipeSelected.RemoveListener(HandleRecipeSelected);
        }
    }

    private void HandleRecipeSelected(CraftingRecipe selectedRecipe)
    {
        Debug.Log($"Selected recipe: {SOHelpers.GetFullStrippedName(selectedRecipe.output)}");

        // Add the selected recipe to the chosen set
        _chosenRecipes.Add(selectedRecipe);

        // If integration with NewRecipePanelScript is enabled, invoke the event to update the panel
        if (_useMineralChoice)
        {
            if (_newRecipePanel != null)
            {
                _newRecipePanel.DisplaySelectedRecipe(selectedRecipe);
            }
        }
        
        gameObject.SetActive(false);
    }

    public void ResetMineralChoice()
    {
        _filteredRecipes = CraftingManager.Instance._recipes
            .Where(r => r.productType == CraftingRecipe.ProductType.Mineral && !_chosenRecipes.Contains(r)) // Exclude already chosen recipes
            .OrderBy(x => Random.value)
            .Take(3)
            .ToList();

        _mineralChoiceBGs = _mineralChoiceHolders.Select(img => img.transform.GetChild(0).GetComponent<Image>()).ToArray();
        _mineralNameTexts = _mineralChoiceBGs.Select(img => img.transform.GetChild(0).GetComponent<TextMeshProUGUI>()).ToArray();
        _mineralImages = _mineralChoiceBGs.Select(img => img.transform.GetChild(1).GetComponent<Image>()).ToArray();

        if (_filteredRecipes == null || _filteredRecipes.Count == 0)
        {
            Debug.LogError("No recipes available after filtering.");
            return;
        }

        // Update the UI with the selected recipes
        for (int i = 0; i < _filteredRecipes.Count; i++)
        {
            if (i < _mineralNameTexts.Length && i < _mineralImages.Length)
            {
                SetupMineralUI(i, _filteredRecipes[i]);
            }
        }

        // Hide any extra UI elements if there are fewer than 3 recipes
        for (int i = _filteredRecipes.Count; i < _mineralNameTexts.Length; i++)
        {
            _mineralNameTexts[i].gameObject.SetActive(false);
            _mineralImages[i].gameObject.SetActive(false);
        }
    }

}