using UnityEngine;

public class Mineral : MonoBehaviour
{
    public MineralData data;

    [SerializeField] private SpriteRenderer _mineralSprite;

    private void Awake()
    {
        UpdateDataVisuals();
    }

    public void UpdateDataVisuals()
    {
        if (data != null)
        {
            _mineralSprite.sprite = data.mineralSprite;
        }
    }
}