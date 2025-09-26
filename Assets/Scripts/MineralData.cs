using UnityEngine;

[CreateAssetMenu(menuName = "Chemistry/Mineral Data")]
public class MineralData : ScriptableObject
{
    public string mineralName;
    public Sprite mineralSprite;

        private void OnEnable()
    {
        // Set mineralName to the ScriptableObject asset name
        mineralName = this.name;
    }
}