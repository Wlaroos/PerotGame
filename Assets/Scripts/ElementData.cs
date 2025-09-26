using UnityEngine;

[CreateAssetMenu(menuName = "Chemistry/Element Data")]
public class ElementData : ScriptableObject
{
    public string elementName;
    public Sprite elementSprite;
    public Sprite[] numberSprites;
    public int defaultIsotopeNumber = 1;
    public Color32 defaultColor = new Color32(255, 255, 255, 255);

    private void OnEnable()
    {
        // Load all number sprites from a Resources folder (e.g., "Sprites/Numbers")
        if (numberSprites == null || numberSprites.Length == 0)
        {
            numberSprites = Resources.LoadAll<Sprite>("Sprites/Numbers");
        }

        // Set elementName to the ScriptableObject asset name
        elementName = this.name;
    }
}