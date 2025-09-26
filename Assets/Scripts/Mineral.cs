using UnityEngine;

public class Mineral : MonoBehaviour
{
    public enum MineralType
    {
        Silicate,
        IronCarbonate,
        IronPhosphate,
        AluminumOxide
    }

    [SerializeField] private MineralType _mineralType;
    public MineralType Type => _mineralType;

    [SerializeField] private Sprite[] _mineralSprites;
    [SerializeField] private Sprite[] _bigMineralSprites;
    [SerializeField] private SpriteRenderer _mineralSprite;

    [SerializeField] private SpriteRenderer _bigMineralSprite;
    [SerializeField] private SpriteRenderer _backgroundSprite;

    private void Awake()
    {
        if (_mineralSprite == null)
            Debug.LogError("Mineral: Mineral sprite is not assigned.");
        if (_bigMineralSprite == null)
            Debug.LogError("Mineral: Big mineral sprite is not assigned.");
        if (_backgroundSprite == null)
            Debug.LogError("Mineral: Background sprite is not assigned.");

        SetMineral(_mineralType);
    }

    public void SetMineral(MineralType newType)
    {
        _mineralType = newType;

        switch (_mineralType)
        {
            case MineralType.Silicate:
                if (_mineralSprites.Length > 0) _mineralSprite.sprite = _mineralSprites[0];
                if (_bigMineralSprites.Length > 0) _bigMineralSprite.sprite = _bigMineralSprites[0];
                _mineralSprite.color = new Color(0.7f, 0.8f, 0.9f, 1f); // Example color
                _backgroundSprite.color = new Color(0.4f, 0.5f, 0.6f, 1f);
                break;
            case MineralType.IronCarbonate:
                if (_mineralSprites.Length > 1) _mineralSprite.sprite = _mineralSprites[1];
                if (_bigMineralSprites.Length > 1) _bigMineralSprite.sprite = _bigMineralSprites[1];
                _mineralSprite.color = new Color(0.8f, 0.7f, 0.6f, 1f);
                _backgroundSprite.color = new Color(0.5f, 0.4f, 0.3f, 1f);
                break;
            case MineralType.IronPhosphate:
                if (_mineralSprites.Length > 2) _mineralSprite.sprite = _mineralSprites[2];
                if (_bigMineralSprites.Length > 2) _bigMineralSprite.sprite = _bigMineralSprites[2];
                _mineralSprite.color = new Color(0.6f, 0.8f, 0.7f, 1f);
                _backgroundSprite.color = new Color(0.3f, 0.5f, 0.4f, 1f);
                break;
            case MineralType.AluminumOxide:
                if (_mineralSprites.Length > 3) _mineralSprite.sprite = _mineralSprites[3];
                if (_bigMineralSprites.Length > 3) _bigMineralSprite.sprite = _bigMineralSprites[3];
                _mineralSprite.color = new Color(0.9f, 0.9f, 1f, 1f);
                _backgroundSprite.color = new Color(0.7f, 0.7f, 0.9f, 1f);
                break;
            default:
                Debug.LogError("Mineral: Unknown mineral type.");
                break;
        }
    }
}