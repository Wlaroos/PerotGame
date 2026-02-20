using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.Events;

public class MineralChoice : MonoBehaviour
{
    [SerializeField] private Image[] _mineralChoice;
    [SerializeField] private MineralData[] _excludedMinerals; // Optional: minerals to exclude from the random selection
    private Image[] _mineralChoiceBG;
    private MineralData[] _mineralDataArray;
    private TextMeshProUGUI[] _mineralNameTexts; // UI Texts for mineral names
    private Image[] _mineralImages; // UI Images for minerals

    public UnityEvent<MineralData> OnMineralSelected = new UnityEvent<MineralData>(); // Event to send mineral data on click

    private void Start() 
    {
        // Load all minerals from the Resources folder
        _mineralDataArray = Resources.LoadAll<MineralData>("SOs/Minerals");

        if (_excludedMinerals != null && _excludedMinerals.Length > 0)
        {
            // Exclude specified minerals by comparing their names or unique identifiers
            var excludedNames = _excludedMinerals.Select(m => m.name).ToHashSet();
            _mineralDataArray = _mineralDataArray.Where(m => !excludedNames.Contains(m.name)).ToArray();
        }

        _mineralChoiceBG = _mineralChoice.Select(img => img.transform.GetChild(0).GetComponent<Image>()).ToArray();
        _mineralNameTexts = _mineralChoiceBG.Select(img => img.transform.GetChild(0).GetComponent<TextMeshProUGUI>()).ToArray();
        _mineralImages = _mineralChoiceBG.Select(img => img.transform.GetChild(1).GetComponent<Image>()).ToArray();

        if (_mineralDataArray == null || _mineralDataArray.Length == 0)
        {
            Debug.LogError("No minerals found in the Resources/Minerals folder.");
            return;
        }

        // Select three random minerals
        var randomMinerals = _mineralDataArray.OrderBy(x => Random.value).Take(3).ToArray();

        // Update the UI with the selected minerals
        for (int i = 0; i < randomMinerals.Length; i++)
        {
            if (i < _mineralNameTexts.Length && i < _mineralImages.Length)
            {
                // Set the mineral name
                _mineralNameTexts[i].text = SOHelpers.GetFullStrippedName(randomMinerals[i]); // Assuming MineralData has a mineralName property

                // Set the mineral image while maintaining the original aspect ratio
                if (randomMinerals[i].mineralBigSprite != null) // Check if the big sprite is assigned
                {
                    _mineralImages[i].sprite = randomMinerals[i].mineralBigSprite; // Assuming MineralData has a mineralBigSprite property
                }
                else if (randomMinerals[i].mineralSprite != null) // Fallback to the regular sprite if big sprite is not assigned
                {
                    _mineralImages[i].sprite = randomMinerals[i].mineralSprite; // Assuming MineralData has a mineralSprite property
                }
                else
                {
                    Debug.LogWarning($"Mineral '{randomMinerals[i].name}' does not have a sprite assigned.");
                    _mineralImages[i].sprite = null; // Clear the image if no sprite is available
                }
                _mineralImages[i].preserveAspect = true;

                // Set the parent's parent image color to the mineral's color
                var parentParentImage = _mineralImages[i].transform.parent.parent.GetComponent<Image>();
                if (parentParentImage != null)
                {
                    parentParentImage.color = randomMinerals[i].defaultColor; // Assuming MineralData has a defaultColor property
                }

                // Set the parent's image color to a slightly darker version of the mineral's color
                var parentImage = _mineralImages[i].transform.parent.GetComponent<Image>();
                if (parentImage != null)
                {
                    parentImage.color = (Color)randomMinerals[i].defaultColor * new Color(0.5f, 0.5f, 0.5f, 1f); // Darken the color slightly
                }

                // Ensure the mineral image is visible
                _mineralImages[i].color = Color.white;

                // Add button functionality
                var button = _mineralChoice[i].GetComponent<Button>();
                if (button != null)
                {
                    int index = i; // Capture the current index for the lambda
                    button.onClick.AddListener(() => OnMineralSelected.Invoke(randomMinerals[index]));

                    // Add hover effects
                    var colors = button.colors;
                    colors.highlightedColor = (Color)randomMinerals[index].defaultColor + new Color(0.5f, 0.5f, 0.5f, 1f); // Closer to white on hover
                    colors.pressedColor = (Color)randomMinerals[index].defaultColor * new Color(0.8f, 0.8f, 0.8f, 1f); // Slightly darker on click
                    button.colors = colors;
                }
            }
        }

        // Hide any extra UI elements if there are fewer than 3 minerals
        for (int i = randomMinerals.Length; i < _mineralNameTexts.Length; i++)
        {
            _mineralNameTexts[i].gameObject.SetActive(false);
            _mineralImages[i].gameObject.SetActive(false);
        }
    }
}
