using UnityEngine;
using UnityEngine.UI;

public class CompoundSpawner : MonoBehaviour
{
    private static CompoundSpawner _instance;
    public static CompoundSpawner Instance => _instance;

    [SerializeField] private GameObject compoundPrefab;
    public GameObject CompoundPrefab => compoundPrefab;

    [SerializeField] private Image _spawnArea;
    public Image SpawnArea => _spawnArea;

    [SerializeField] private Button[] _spawnButtons;
    private Sprite[] _spawnButtonSprites;

    [SerializeField] private Sprite _hiddenButtonSprite;
    public Sprite HiddenButtonSprite => _hiddenButtonSprite;

    [SerializeField] private Vector2 _buffer = new Vector2(100, 100);

    [SerializeField] private bool _unlockAllCompounds = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

        _spawnButtonSprites = new Sprite[_spawnButtons.Length];
        for (int i = 0; i < _spawnButtons.Length; i++)
        {
            _spawnButtonSprites[i] = _spawnButtons[i].targetGraphic.GetComponent<Image>().sprite;
            if (_spawnButtonSprites[i] == null)
            {
                Debug.LogError($"CompoundSpawner: Button at index {i} does not have a sprite.");
            }
            _spawnButtons[i].interactable = false;
            _spawnButtons[i].targetGraphic.GetComponent<Image>().sprite = _hiddenButtonSprite;
        }
    }

    private void OnDestroy()
    {
    if (_instance == this)
    {
        _instance = null;
    }
}

    private void Start()
    {
        if (_unlockAllCompounds)
        {
            UnlockAllButtons();
        }
    }

public GameObject SpawnCompoundAtRandomPosition(Compound.CompoundType type)
{
    Vector2 randomPosition = new Vector2
    (
        Random.Range(SpawnArea.rectTransform.rect.xMin + _buffer.x, SpawnArea.rectTransform.rect.xMax - _buffer.x),
        Random.Range(SpawnArea.rectTransform.rect.yMin + _buffer.y, SpawnArea.rectTransform.rect.yMax - _buffer.y)
    );

    Vector3 worldPosition = SpawnArea.rectTransform.TransformPoint(randomPosition);

    return SpawnCompoundAtPosition(type, worldPosition);
}

public GameObject SpawnCompoundAtPosition(Compound.CompoundType type, Vector3 position)
{
    if (compoundPrefab == null)
    {
        Debug.LogError("CompoundSpawner: Cannot spawn compound, prefab is not assigned.");
        return null;
    }

    GameObject newCompound = Instantiate(compoundPrefab, position, Quaternion.identity);
    Compound compoundComponent = newCompound.GetComponent<Compound>();
    if (compoundComponent != null)
    {
        compoundComponent.SetCompound(type);
    }
    else
    {
        Debug.LogError("CompoundSpawner: Spawned prefab does not have a Compound component.");
        Destroy(newCompound);
        return null;
    }

    return newCompound;
}

// Spawn methods for each compound type
public void SpawnOxide()    { SpawnCompoundAtRandomPosition(Compound.CompoundType.Oxide); }
public void SpawnCarbonate(){ SpawnCompoundAtRandomPosition(Compound.CompoundType.Carbonate); }
public void SpawnPhosphate(){ SpawnCompoundAtRandomPosition(Compound.CompoundType.Phosphate); }
public void SpawnSilicate(){ SpawnCompoundAtRandomPosition(Compound.CompoundType.Silicate); }
public void SpawnSulfate() { SpawnCompoundAtRandomPosition(Compound.CompoundType.Sulfate); }

// Unlock all buttons at once (call from UI)
public void UnlockAllButtons()
{
    for (int i = 0; i < _spawnButtons.Length; i++)
    {
        _spawnButtons[i].interactable = true;
        _spawnButtons[i].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[i];
    }
}

// Optional: Individual unlock methods for event-based unlocking
private void FirstOxide()
{
    _spawnButtons[0].interactable = true;
    _spawnButtons[0].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[0];
}
private void FirstCarbonate()
{
    _spawnButtons[1].interactable = true;
    _spawnButtons[1].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[1];
}
private void FirstPhosphate()
{
    _spawnButtons[2].interactable = true;
    _spawnButtons[2].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[2];
}
private void FirstSilicate()
{
    _spawnButtons[3].interactable = true;
    _spawnButtons[3].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[3];
}
private void FirstSulfate()
{
    _spawnButtons[4].interactable = true;
    _spawnButtons[4].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[4];
}

private void OnDrawGizmos()
{
    if (_spawnArea != null)
    {
        RectTransform rectTransform = _spawnArea.rectTransform;
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Gizmos.color = Color.green;
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }

        Vector3 center = rectTransform.position;
        Vector2 size = rectTransform.rect.size;
        Vector3 bufferedMin = rectTransform.TransformPoint(new Vector3(-size.x / 2 + _buffer.x, -size.y / 2 + _buffer.y, 0));
        Vector3 bufferedMax = rectTransform.TransformPoint(new Vector3(size.x / 2 - _buffer.x, size.y / 2 - _buffer.y, 0));

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(bufferedMin.x, bufferedMin.y, 0), new Vector3(bufferedMax.x, bufferedMin.y, 0));
        Gizmos.DrawLine(new Vector3(bufferedMax.x, bufferedMin.y, 0), new Vector3(bufferedMax.x, bufferedMax.y, 0));
        Gizmos.DrawLine(new Vector3(bufferedMax.x, bufferedMax.y, 0), new Vector3(bufferedMin.x, bufferedMax.y, 0));
        Gizmos.DrawLine(new Vector3(bufferedMin.x, bufferedMax.y, 0), new Vector3(bufferedMin.x, bufferedMin.y, 0));
    }
}
}