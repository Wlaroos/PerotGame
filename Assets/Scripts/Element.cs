using UnityEngine;

public class Element : MonoBehaviour
{
    public ElementData data;
    public int isotopeNumber = 1;

    [SerializeField] private SpriteRenderer _elementSprite;
    [SerializeField] private SpriteRenderer _numberSprite;
    [SerializeField] private SpriteRenderer _numberSprite2;
    [SerializeField] private SpriteRenderer _backgroundSprite;

    private void Awake()
    {
        if (data != null)
        {
            _elementSprite.sprite = data.elementSprite;
            SetNumberSprites(isotopeNumber);
        }
    }

    public void SetNumberSprites(int number)
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
}
