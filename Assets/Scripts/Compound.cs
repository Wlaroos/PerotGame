using UnityEngine;

public class Compound : MonoBehaviour
{
    public CompoundData data;

    [SerializeField] private SpriteRenderer _compoundSprite;
    [SerializeField] private SpriteRenderer _backgroundSprite;

    private void Awake()
    {
        if (data != null)
        {
            _compoundSprite.sprite = data.compoundSprite;
            // Set color or other visuals as needed
        }
    }
}