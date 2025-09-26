using UnityEngine;

[CreateAssetMenu(menuName = "Chemistry/Mineral Data")]
public class MineralData : ScriptableObject
{
    public string mineralName;
    public Sprite mineralSprite;
    public Sprite mineralBigSprite;
    public Color32 defaultColor = new Color32(255, 255, 255, 255);

    private void OnEnable()
    {
        // Set mineralName to the ScriptableObject asset name
        mineralName = this.name;
    }
}