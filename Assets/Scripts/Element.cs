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

    private ParticleSystem _ps;

    // Called when the object is created
    private void Awake()
    {
        _ps = GetComponentInChildren<ParticleSystem>();
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
        // if (data == null)
        //     return;

        // switch (data.elementType)
        // {
        //     case ElementData.ElementType.Solid:
        //         _ps.Stop();
        //         break;
        //     case ElementData.ElementType.Liquid:
        //         var mainL = _ps.main;
        //         mainL.startSpeed = 0.5f;
        //         _ps.Play();
        //         break;
        //     case ElementData.ElementType.Gas:
        //         var mainG = _ps.main;
        //         mainG.startSpeed = 1.5f;
        //         _ps.Play();
        //         break;
        // }


        // ensure editor-safe lazy load (avoid OnEnable loads)
        data.EnsureNumberSpritesLoaded();

        if (_elementSprite != null)
        {
            _elementSprite.sprite = data.elementSprite;
            _elementSprite.color = data.defaultColor;

            if (_ps != null)
            {
                var main = _ps.main;
                Color c1 = data.defaultColor;
                Color c2 = new Color(Mathf.Min(1f, c1.r + 0.3f), Mathf.Min(1f, c1.g + 0.3f), Mathf.Min(1f, c1.b + 0.3f), c1.a);
                main.startColor = new ParticleSystem.MinMaxGradient(c1, c2);
            }
        }

        // Dim the background color
        Color32 c = data.defaultColor;
        Color dimmed = new Color(c.r / 255f * 0.5f, c.g / 255f * 0.5f, c.b / 255f * 0.5f, c.a / 255f);
        _backgroundSprite.color = dimmed;

        SetNumberSprites(isotopeNumber);
    }
}
