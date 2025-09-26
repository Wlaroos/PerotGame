using UnityEngine;

public class Element : MonoBehaviour
{
    [SerializeField] public enum ElementType
    {
        Hydrogen,
        Helium,
        Beryllium,
        Carbon,
        Titanium,
        Iron,
        Copper,
        Calcium,
        Barium,
        Silicon,
        Aluminum,
        Magnesium
    }
    [SerializeField] private ElementType _elementType;
    public ElementType Type => _elementType;

    [SerializeField] private Sprite[] _elementSprites;
    public Sprite[] ElementSprites => _elementSprites;
    [SerializeField] private Sprite[] _numberSprites;
    public Sprite[] NumberSprites => _numberSprites;

    [SerializeField] private int isotopeNumber = 1;
    public int IsotopeNumber => isotopeNumber;

    [SerializeField] private SpriteRenderer _elementSprite;
    [SerializeField] private SpriteRenderer _numberSprite;
    [SerializeField] private SpriteRenderer _numberSprite2; // Added for two-digit numbers
    [SerializeField] private SpriteRenderer _backgroundSprite;

    private void Awake()
    {
        if (_elementSprite == null)
        {
            Debug.LogError("Element: Element sprite is not assigned.");
        }
        if (_numberSprite == null)
        {
            Debug.LogError("Element: Number sprite is not assigned.");
        }
        if (_backgroundSprite == null)
        {
            Debug.LogError("Element: Background sprite is not assigned.");
        }
        if (_numberSprite2 == null)
        {
            Debug.LogError("Element: Second number sprite is not assigned.");
        }

        SetElement(_elementType, isotopeNumber);
    }

    private void SetNumberSprites(int number)
    {
        if (number < 10)
        {
            _numberSprite.sprite = _numberSprites[number];
            _numberSprite2.gameObject.SetActive(false); // Hide the second digit
        }
        else
        {
            int tens = number / 10;
            int ones = number % 10;

            _numberSprite.sprite = _numberSprites[tens];
            _numberSprite2.sprite = _numberSprites[ones];
            _numberSprite2.gameObject.SetActive(true); // Show the second digit
        }
    }

    public void SetElement(ElementType newType, int newIsotopeNumber)
    {
        _elementType = newType;
        isotopeNumber = newIsotopeNumber;

        switch (_elementType)
        {
            case ElementType.Hydrogen:
                _elementSprite.sprite = _elementSprites[0];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(1f, 1f, 1f, 1f); // White
                _backgroundSprite.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Darker white
                break;
            case ElementType.Helium:
                _elementSprite.sprite = _elementSprites[1];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(1f, 0.84f, 0f, 1f); // Gold
                _backgroundSprite.color = new Color(0.8f, 0.67f, 0f, 1f); // Darker gold
                break;
            case ElementType.Beryllium:
                _elementSprite.sprite = _elementSprites[2];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(0.76f, 1f, 0.76f, 1f); // Light green
                _backgroundSprite.color = new Color(0.57f, 0.8f, 0.57f, 1f); // Darker green
                break;
            case ElementType.Carbon:
                _elementSprite.sprite = _elementSprites[3];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray
                _backgroundSprite.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Darker gray
                break;
            case ElementType.Titanium:
                _elementSprite.sprite = _elementSprites[4];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(0.7f, 0.7f, 0.8f, 1f); // Light steel blue
                _backgroundSprite.color = new Color(0.4f, 0.4f, 0.5f, 1f); // Darker steel blue
                break;
            case ElementType.Iron:
                _elementSprite.sprite = _elementSprites[5];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(0.56f, 0.57f, 0.58f, 1f); // Iron gray
                _backgroundSprite.color = new Color(0.3f, 0.3f, 0.32f, 1f); // Darker iron gray
                break;
            case ElementType.Copper:
                _elementSprite.sprite = _elementSprites[6];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(0.72f, 0.45f, 0.2f, 1f); // Copper
                _backgroundSprite.color = new Color(0.45f, 0.28f, 0.12f, 1f); // Darker copper
                break;
            case ElementType.Calcium:
                _elementSprite.sprite = _elementSprites[7];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(0.9f, 0.96f, 0.98f, 1f); // Pale blue-white
                _backgroundSprite.color = new Color(0.6f, 0.7f, 0.75f, 1f); // Darker pale blue
                break;
            case ElementType.Barium:
                _elementSprite.sprite = _elementSprites[8];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(0.8f, 1f, 0.8f, 1f); // Pale green
                _backgroundSprite.color = new Color(0.5f, 0.7f, 0.5f, 1f); // Darker pale green
                break;
            case ElementType.Silicon:
                _elementSprite.sprite = _elementSprites[9];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(0.6f, 0.6f, 0.7f, 1f); // Blue-gray
                _backgroundSprite.color = new Color(0.35f, 0.35f, 0.45f, 1f); // Darker blue-gray
                break;
            case ElementType.Aluminum:
                _elementSprite.sprite = _elementSprites[10];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(0.8f, 0.85f, 0.88f, 1f); // Silvery
                _backgroundSprite.color = new Color(0.5f, 0.55f, 0.6f, 1f); // Darker silvery
                break;
            case ElementType.Magnesium:
                _elementSprite.sprite = _elementSprites[11];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(0.85f, 0.9f, 0.85f, 1f); // Light gray-green
                _backgroundSprite.color = new Color(0.55f, 0.6f, 0.55f, 1f); // Darker gray-green
                break;
            default:
                Debug.LogError("Element: Unknown element type.");
                break;
        }

        _numberSprite.color = new Color(1f, 0f, 0f, 1f); // Red
        _numberSprite2.color = new Color(1f, 0f, 0f, 1f); // Red
    }
}
