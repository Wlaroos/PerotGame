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
    [SerializeField] private SpriteRenderer _backgroundSprite;
    [SerializeField] private SpriteRenderer _secondNumberSprite; // Added for two-digit numbers

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
        if (_secondNumberSprite == null)
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
            _secondNumberSprite.gameObject.SetActive(false); // Hide the second digit
        }
        else
        {
            int tens = number / 10;
            int ones = number % 10;

            _numberSprite.sprite = _numberSprites[tens];
            _secondNumberSprite.sprite = _numberSprites[ones];
            _secondNumberSprite.gameObject.SetActive(true); // Show the second digit
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
                _backgroundSprite.color = new Color(1f, 1f, 1f, 0.5f); // White with 50% opacity
                break;
            case ElementType.Helium:
                _elementSprite.sprite = _elementSprites[1];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(1f, 0.84f, 0f, 1f); // Gold with full opacity
                _backgroundSprite.color = new Color(1f, 0.84f, 0f, 0.5f); // Gold with 50% opacity
                break;
            case ElementType.Beryllium:
                _elementSprite.sprite = _elementSprites[2];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(0.76f, 1f, 0.76f, 1f); // Light green with full opacity
                _backgroundSprite.color = new Color(0.76f, 1f, 0.76f, 0.5f); // Light green with 50% opacity
                break;
            case ElementType.Carbon:
                _elementSprite.sprite = _elementSprites[3];
                SetNumberSprites(isotopeNumber);
                _elementSprite.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray with full opacity
                _backgroundSprite.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gray with 50% opacity
                break;
            default:
                Debug.LogError("Element: Unknown element type.");
                break;
        }

        _numberSprite.color = new Color(1f, 0f, 0f, 1f); // Red with full opacity
    }
}
