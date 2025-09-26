using UnityEngine;

[CreateAssetMenu(menuName = "Chemistry/Element Data")]
public class ElementData : ScriptableObject
{
    public string elementName;
    public Sprite elementSprite;
    public Sprite[] numberSprites;
    public int defaultIsotopeNumber = 1;
}