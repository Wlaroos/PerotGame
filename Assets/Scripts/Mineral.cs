using UnityEngine;

public class Mineral : MonoBehaviour
{
    public MineralData data;

    [SerializeField] private SpriteRenderer _mineralSprite;
    [SerializeField] private SpriteRenderer _mineralBigSprite;
    [SerializeField] private SpriteRenderer _backgroundSprite;

    private void Awake()
    {
        UpdateDataVisuals();
    }

    public void UpdateDataVisuals()
    {
        if (data != null)
        {
            if (_mineralSprite != null)
            {
                _mineralSprite.sprite = data.mineralSprite;
                _mineralSprite.color = data.defaultColor;
            }
            if (_mineralBigSprite != null)
            {
                _mineralBigSprite.sprite = data.mineralBigSprite;
            }

                        // Dim the background color by multiplying each channel by 0.5f
            Color32 c = data.defaultColor;
            Color dimmed = new Color(c.r / 255f * 0.5f, c.g / 255f * 0.5f, c.b / 255f * 0.5f, c.a / 255f);
            _backgroundSprite.color = dimmed;
        }
    }
}