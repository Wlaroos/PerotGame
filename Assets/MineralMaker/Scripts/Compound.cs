using UnityEngine;

// Controls how a compound looks and stores its info
public class Compound : MonoBehaviour
{
    public CompoundData data; // Info about this compound

    [SerializeField] private SpriteRenderer _compoundSprite;   // Main picture of the compound
    [SerializeField] private SpriteRenderer _backgroundSprite; // Background behind the compound

    // Runs when the object is created
    private void Awake()
    {
        UpdateDataVisuals(); // Set up how the compound looks
    }

    // Updates the compound's appearance based on its data
    public void UpdateDataVisuals()
    {
        if (data != null)
        {
            // Set the main sprite and color
            _compoundSprite.sprite = data.compoundSprite;
            _compoundSprite.color = data.defaultColor;

            // Make the background a dimmer version of the main color
            Color32 c = data.defaultColor;
            Color dimmed = new Color(c.r / 255f * 0.5f, c.g / 255f * 0.5f, c.b / 255f * 0.5f, c.a / 255f);
            _backgroundSprite.color = dimmed;
        }
    }
}