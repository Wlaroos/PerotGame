using UnityEngine;

public class Element : MonoBehaviour
{
    [SerializeField] public enum ElementType { Hydrogen, Helium, Beryllium, Carbon }
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
                _elementSprite.color = new Color(1f, 1f, 1f, 1f); // White with full opacity
                _backgroundSprite.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Darker white with full opacity
                break;
            case ElementType.Helium:
                _elementSprite.sprite = _elementSprites[1];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(1f, 0.84f, 0f, 1f); // Gold with full opacity
                _backgroundSprite.color = new Color(0.8f, 0.67f, 0f, 1f); // Darker gold with full opacity
                break;
            case ElementType.Beryllium:
                _elementSprite.sprite = _elementSprites[2];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(0.76f, 1f, 0.76f, 1f); // Light green with full opacity
                _backgroundSprite.color = new Color(0.57f, 0.8f, 0.57f, 1f); // Darker green with full opacity
                break;
            case ElementType.Carbon:
                _elementSprite.sprite = _elementSprites[3];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray with full opacity
                _backgroundSprite.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Darker gray with full opacity
                break;
            default:
                Debug.LogError("Element: Unknown element type.");
                break;
        }

        _numberSprite.color = new Color(1f, 0f, 0f, 1f); // Red with full opacity
        _numberSprite2.color = new Color(1f, 0f, 0f, 1f); // Red with full opacity
    }
}
