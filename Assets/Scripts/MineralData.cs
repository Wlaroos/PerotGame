using UnityEngine;

// Stores info about a mineral (used as a ScriptableObject)
[CreateAssetMenu(menuName = "Chemistry/Mineral Data")]
public class MineralData : ScriptableObject
{
    public string mineralName;         // Name of the mineral
    public Sprite mineralSprite;       // Main sprite for the mineral
    public Sprite mineralBigSprite;    // Big version of the mineral sprite
    public Color32 defaultColor = new Color32(255, 255, 255, 255); // Default color

    private void OnEnable()
    {
        // Set the mineral name to the asset's name
        mineralName = this.name;
    }
}