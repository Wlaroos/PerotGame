using UnityEngine;

public class Mineral : MonoBehaviour
{
    public MineralData data;

    [SerializeField] private SpriteRenderer _mineralSprite;

    private void Awake()
    {
        if (data != null)
        {
            _mineralSprite.sprite = data.mineralSprite;
        }
    }
}