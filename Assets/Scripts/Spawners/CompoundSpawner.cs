using UnityEngine;
using UnityEngine.UI;

// Spawns compound objects and manages compound spawn buttons
public class CompoundSpawner : MonoBehaviour
{
    private static CompoundSpawner _instance;
    public static CompoundSpawner Instance => _instance;

    [SerializeField] private GameObject compoundPrefab; // Prefab for spawning compounds
    public GameObject CompoundPrefab => compoundPrefab;

    [SerializeField] private Image _spawnArea; // UI area where compounds can spawn
    public Image SpawnArea => _spawnArea;

    [SerializeField] private Button[] _spawnButtons; // Buttons to spawn each compound
    private Sprite[] _spawnButtonSprites; // Stores the normal sprites for each button

    [SerializeField] private Sprite _hiddenButtonSprite; // Sprite for locked/hidden buttons
    public Sprite HiddenButtonSprite => _hiddenButtonSprite;

    [SerializeField] private Vector2 _buffer = new Vector2(100, 100); // Padding from spawn area edges

    [SerializeField] private bool _unlockAllCompounds = false; // If true, unlock all buttons at start

    [SerializeField] private CompoundData[] _compoundDataList; // List of compound data for each button

    // Runs when the object is created
    private void Awake()
    {
        // Set up singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

        // Store the original sprites for each button and lock them
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

    // When this object is destroyed
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    // Runs at the start of the game
    private void Start()
    {
        // Optionally unlock all buttons at the start
        if (_unlockAllCompounds)
        {
            UnlockAllButtons();
        }
    }

    // Spawn a compound at a random position inside the spawn area
    public GameObject SpawnCompoundAtRandomPosition(CompoundData data)
    {
        Vector2 randomPosition = new Vector2
        (
            Random.Range(SpawnArea.rectTransform.rect.xMin + _buffer.x, SpawnArea.rectTransform.rect.xMax - _buffer.x),
            Random.Range(SpawnArea.rectTransform.rect.yMin + _buffer.y, SpawnArea.rectTransform.rect.yMax - _buffer.y)
        );

        Vector3 worldPosition = SpawnArea.rectTransform.TransformPoint(randomPosition);

        return SpawnCompoundAtPosition(data, worldPosition);
    }

    // Spawn a compound at a specific position
    public GameObject SpawnCompoundAtPosition(CompoundData data, Vector3 position)
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
            compoundComponent.data = data;
            compoundComponent.UpdateDataVisuals();
        }
        else
        {
            Debug.LogError("CompoundSpawner: Spawned prefab does not have a Compound component.");
            Destroy(newCompound);
            return null;
        }

        return newCompound;
    }

    // Spawn methods for each compound type, using the ScriptableObjects
    public void SpawnOxide()
    {
        if (_compoundDataList.Length > 0 && _compoundDataList[0] != null)
            SpawnCompoundAtRandomPosition(_compoundDataList[0]);
    }
    public void SpawnCarbonate()
    {
        if (_compoundDataList.Length > 1 && _compoundDataList[1] != null)
            SpawnCompoundAtRandomPosition(_compoundDataList[1]);
    }
    public void SpawnPhosphate()
    {
        if (_compoundDataList.Length > 2 && _compoundDataList[2] != null)
            SpawnCompoundAtRandomPosition(_compoundDataList[2]);
    }
    public void SpawnSilicate()
    {
        if (_compoundDataList.Length > 3 && _compoundDataList[3] != null)
            SpawnCompoundAtRandomPosition(_compoundDataList[3]);
    }
    public void SpawnSulfate()
    {
        if (_compoundDataList.Length > 4 && _compoundDataList[4] != null)
            SpawnCompoundAtRandomPosition(_compoundDataList[4]);
    }

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

    // Draws the spawn area and buffer in the editor for debugging
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