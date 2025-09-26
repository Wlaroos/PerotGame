using UnityEngine;

// Handles the visual and data logic for an element object
public class Element : MonoBehaviour
{
    public ElementData data; // Data for this element
    public int isotopeNumber = 1; // Isotope number to display

    [SerializeField] private SpriteRenderer _elementSprite;    // Main element sprite
    [SerializeField] private SpriteRenderer _numberSprite;     // Sprite for tens or single digit
    [SerializeField] private SpriteRenderer _numberSprite2;    // Sprite for ones digit (if needed)
    [SerializeField] private SpriteRenderer _backgroundSprite; // Background sprite

    // Called when the object is created
    private void Awake()
    {
        UpdateDataVisuals();
    }

    // Sets the number sprites based on the isotope number
    private void SetNumberSprites(int number)
    {
        if (number < 0)
        {
            _numberSprite.gameObject.SetActive(false);
            _numberSprite2.gameObject.SetActive(false);
            return;
        }
        if (number < 10)
        {
            _numberSprite.sprite = data.numberSprites[number];
            _numberSprite.gameObject.SetActive(true);
            _numberSprite2.gameObject.SetActive(false);
        }
        else
        {
            int tens = number / 10;
            int ones = number % 10;
            _numberSprite.sprite = data.numberSprites[tens];
            _numberSprite2.sprite = data.numberSprites[ones];
            _numberSprite.gameObject.SetActive(true);
            _numberSprite2.gameObject.SetActive(true);
        }
    }

    // Updates the element's visuals based on its data
    public void UpdateDataVisuals()
    {
        if (data != null)
        {
            _elementSprite.sprite = data.elementSprite;
            _elementSprite.color = data.defaultColor;

            // Dim the background color
            Color32 c = data.defaultColor;
            Color dimmed = new Color(c.r / 255f * 0.5f, c.g / 255f * 0.5f, c.b / 255f * 0.5f, c.a / 255f);
            _backgroundSprite.color = dimmed;

            SetNumberSprites(isotopeNumber);
        }
    }
}
