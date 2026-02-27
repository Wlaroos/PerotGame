using TMPro;
using UnityEngine;

// Controls how a mineral looks and stores its info
public class Mineral : MonoBehaviour
{
    public MineralData data; // Info about this mineral

    [SerializeField] private SpriteRenderer _mineralSprite;    // Main picture of the mineral
    [SerializeField] private SpriteRenderer _mineralBigSprite; // Big version of the mineral sprite
    [SerializeField] private SpriteRenderer _backgroundSprite; // Background behind the mineral
    [SerializeField] private TextMeshPro _mineralNameText; // Text showing the mineral's name
    private BoxCollider2D _bc;

    // Runs when the object is created
    private void Awake()
    {
        _bc = GetComponent<BoxCollider2D>();
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
            if (_mineralNameText != null)
            {
                string temp = data.mineralName;

                // Remove any prefix before an underscore
                int idx = temp.IndexOf('_');
                if (idx >= 0)
                {
                    temp = (idx + 1 < temp.Length) ? temp.Substring(idx + 1) : string.Empty;
                }

                _mineralNameText.text = temp;
                _mineralNameText.color = data.defaultColor;
            }

            // Make the background a dimmer version of the main color
            Color32 c = data.defaultColor;
            Color dimmed = new Color(c.r / 255f * 0.5f, c.g / 255f * 0.5f, c.b / 255f * 0.5f, c.a / 255f);
            _backgroundSprite.color = dimmed;

            // Combine the bounds of the mineral sprite and the big sprite
            Bounds combinedBounds = _mineralSprite.bounds;
            combinedBounds.Encapsulate(_mineralBigSprite.bounds);

            _bc.size = combinedBounds.size;
            _bc.offset = combinedBounds.center - transform.position;
        }
    }
}