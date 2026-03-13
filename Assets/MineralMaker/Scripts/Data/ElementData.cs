using UnityEngine;

// Stores info about an element (used as a ScriptableObject)
[CreateAssetMenu(menuName = "Chemistry/Element Data")]
public class ElementData : ScriptableObject
{
    public string elementName;         // Name of the element
    public Sprite elementSprite;       // Main sprite for the element
    public Sprite altElementSprite;    // Alternative sprite for the element
    public Sprite[] numberSprites;     // Sprites for numbers (for isotope)
    public int defaultIsotopeNumber = -1; // Default isotope number
    public Color32 defaultColor = new Color32(255, 255, 255, 255); // Default color
    public enum ElementType { Solid, Liquid, Gas }
    public ElementType elementType;    // Type of the element (for visual effects)

    private void OnEnable()
    {
        elementName = this.name;
    }

    public void EnsureNumberSpritesLoaded()
    {
        if (numberSprites == null || numberSprites.Length == 0)
        {
            numberSprites = Resources.LoadAll<Sprite>("Sprites/Numbers");
        }
    }
}