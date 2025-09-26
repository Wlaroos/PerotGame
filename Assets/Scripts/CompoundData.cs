using UnityEngine;

[CreateAssetMenu(menuName = "Chemistry/Compound Data")]
public class CompoundData : ScriptableObject
{
    public string compoundName;
    public Sprite compoundSprite;
    public Color32 defaultColor = new Color32(255, 255, 255, 255);

    private void OnEnable()
    {
        // Set compoundName to the ScriptableObject asset name
        compoundName = this.name;
    }
}