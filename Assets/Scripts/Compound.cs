using UnityEngine;

public class Compound : MonoBehaviour
{
    [SerializeField] public enum CompoundType
    {
        Oxide,
        Carbonate,
        Phosphate,
        Silicate,
        Sulfate
    }
    [SerializeField] private CompoundType _compoundType;
    public CompoundType Type => _compoundType;

    [SerializeField] private Sprite[] _compoundSprites;
    public Sprite[] CompoundSprites => _compoundSprites;

    [SerializeField] private SpriteRenderer _compoundSprite;
    [SerializeField] private SpriteRenderer _backgroundSprite;

    private void Awake()
    {
        if (_compoundSprite == null)
        {
            Debug.LogError("Compound: Compound sprite is not assigned.");
        }
        if (_backgroundSprite == null)
        {
            Debug.LogError("Compound: Background sprite is not assigned.");
        }

        SetCompound(_compoundType);
    }

    public void SetCompound(CompoundType newType)
    {
        _compoundType = newType;

        switch (_compoundType)
        {
            case CompoundType.Oxide:
                _compoundSprite.sprite = _compoundSprites[0];
                _compoundSprite.color = new Color(0.9f, 0.1f, 0.1f, 1f); // Reddish (rust)
                _backgroundSprite.color = new Color(0.6f, 0.05f, 0.05f, 1f);
                break;
            case CompoundType.Carbonate:
                _compoundSprite.sprite = _compoundSprites[1];
                _compoundSprite.color = new Color(0.8f, 0.8f, 0.7f, 1f); // Pale tan
                _backgroundSprite.color = new Color(0.5f, 0.5f, 0.4f, 1f);
                break;
            case CompoundType.Phosphate:
                _compoundSprite.sprite = _compoundSprites[2];
                _compoundSprite.color = new Color(0.7f, 0.9f, 0.6f, 1f); // Light green
                _backgroundSprite.color = new Color(0.4f, 0.6f, 0.3f, 1f);
                break;
            case CompoundType.Silicate:
                _compoundSprite.sprite = _compoundSprites[3];
                _compoundSprite.color = new Color(0.7f, 0.8f, 0.9f, 1f); // Light blue-gray
                _backgroundSprite.color = new Color(0.4f, 0.5f, 0.6f, 1f);
                break;
            case CompoundType.Sulfate:
                _compoundSprite.sprite = _compoundSprites[4];
                _compoundSprite.color = new Color(0.95f, 0.95f, 0.7f, 1f); // Pale yellow
                _backgroundSprite.color = new Color(0.7f, 0.7f, 0.4f, 1f);
                break;
            default:
                Debug.LogError("Compound: Unknown compound type.");
                break;
        }
    }
}