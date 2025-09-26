using UnityEngine;

// Controls how a mineral looks and stores its info
public class Mineral : MonoBehaviour
{
    public MineralData data; // Info about this mineral

    [SerializeField] private SpriteRenderer _mineralSprite;    // Main picture of the mineral
    [SerializeField] private SpriteRenderer _mineralBigSprite; // Big version of the mineral sprite
    [SerializeField] private SpriteRenderer _backgroundSprite; // Background behind the mineral

    // Runs when the object is created
    private void Awake()
    {
        UpdateDataVisuals(); // Set up how the mineral looks
    }

    // Updates the mineral's appearance based on its data
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

            // Make the background a dimmer version of the main color
            Color32 c = data.defaultColor;
            Color dimmed = new Color(c.r / 255f * 0.5f, c.g / 255f * 0.5f, c.b / 255f * 0.5f, c.a / 255f);
            _backgroundSprite.color = dimmed;
        }
    }
}