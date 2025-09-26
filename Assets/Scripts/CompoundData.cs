using UnityEngine;

[CreateAssetMenu(menuName = "Chemistry/Compound Data")]
public class CompoundData : ScriptableObject
{
    public string compoundName;
    public Sprite compoundSprite;

    private void OnEnable()
    {
        // Set compoundName to the ScriptableObject asset name
        compoundName = this.name;
    }
}