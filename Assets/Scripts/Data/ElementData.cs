using UnityEngine;

// Stores info about an element (used as a ScriptableObject)
[CreateAssetMenu(menuName = "Chemistry/Element Data")]
public class ElementData : ScriptableObject
{
    public string elementName;         // Name of the element
    public Sprite elementSprite;       // Main sprite for the element
    public Sprite[] numberSprites;     // Sprites for numbers (for isotope)
    public int defaultIsotopeNumber = -1; // Default isotope number
    public Color32 defaultColor = new Color32(255, 255, 255, 255); // Default color

    private void OnEnable()
    {
        // Load number sprites if not set
        if (numberSprites == null || numberSprites.Length == 0)
        {
            numberSprites = Resources.LoadAll<Sprite>("Sprites/Numbers");
        }

        // Set the element name to the asset's name
        elementName = this.name;
    }
}