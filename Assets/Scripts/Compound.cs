using UnityEngine;

public class Compound : MonoBehaviour
{
    public CompoundData data;

    [SerializeField] private SpriteRenderer _compoundSprite;
    [SerializeField] private SpriteRenderer _backgroundSprite;

    private void Awake()
    {
        UpdateDataVisuals();
    }

    public void UpdateDataVisuals()
    {
        if (data != null)
        {
            _compoundSprite.sprite = data.compoundSprite;
            _compoundSprite.color = data.defaultColor;

            // Dim the background color by multiplying each channel by 0.5f
            Color32 c = data.defaultColor;
            Color dimmed = new Color(c.r / 255f * 0.5f, c.g / 255f * 0.5f, c.b / 255f * 0.5f, c.a / 255f);
            _backgroundSprite.color = dimmed;
        }
    }
}