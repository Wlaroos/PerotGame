using UnityEngine;

// Stores info about a compound (used as a ScriptableObject)
[CreateAssetMenu(menuName = "Chemistry/Compound Data")]
public class CompoundData : ScriptableObject
{
    public string compoundName;        // Name of the compound
    public Sprite compoundSprite;      // Main sprite for the compound
    public Color32 defaultColor = new Color32(255, 255, 255, 255); // Default color

    private void OnEnable()
    {
        // Set the compound name to the asset's name
        compoundName = this.name;
    }
}