using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // added

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

    [SerializeField] private Vector2 _spawnAreaSize = new Vector2(100, 100); // Width/Height of the allowed spawn area (centered inside the SpawnArea)
    public Vector2 SpawnAreaSize => _spawnAreaSize;
    [SerializeField] private Vector2 _spawnAreaCenter = Vector2.zero; // Local offset (in rect local space) from the SpawnArea center
    public Vector2 SpawnAreaCenter => _spawnAreaCenter;

    [SerializeField] private bool _unlockAllCompounds = false; // If true, unlock all buttons at start

    [SerializeField] private CompoundData[] _compoundDataList; // List of compound data for each button

    // Track whether the player is currently dragging a spawned compound (to suppress click-spawns)
    private bool _isDragging = false;
    public bool IsDragging => _isDragging;
    public void SetDragging(bool value) => _isDragging = value;

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

        // Load compound data from Resources (if not assigned in inspector)
        if (_compoundDataList == null || _compoundDataList.Length == 0)
        {
            CompoundData[] loadedCompounds = Resources.LoadAll<CompoundData>("SOs/Compounds");
            if (loadedCompounds != null && loadedCompounds.Length > 0)
                _compoundDataList = loadedCompounds;
        }

        // Set up button listeners so each button spawns the right compound
        for (int i = 0; i < _spawnButtons.Length; i++)
        {
            Button btn = _spawnButtons[i];
            string btnName = btn.name;

            CompoundData matchedData = null;
            if (_compoundDataList != null)
            {
                matchedData = System.Array.Find(_compoundDataList, c => c != null
                    && !string.IsNullOrEmpty(btnName)
                    && btnName.IndexOf(GetCompoundBaseName(c.name), System.StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (matchedData != null)
            {
                btn.onClick.RemoveAllListeners();
                CompoundData dataCopy = matchedData; // capture local for closure safety
                btn.onClick.AddListener(() => OnSpawnButtonClicked(dataCopy));

                // Ensure a drag handler is attached so the player can drag to spawn and place manually
                SpawnDragHandler dragHandler = btn.gameObject.GetComponent<SpawnDragHandler>() ?? btn.gameObject.AddComponent<SpawnDragHandler>();
                dragHandler.Init(this, dataCopy);
            }
            else
            {
                Debug.LogWarning($"CompoundSpawner: No CompoundData found matching button name '{btnName}'.");
            }
        }
    }

    // suppress click-spawn when a drag is in progress
    private void OnSpawnButtonClicked(CompoundData data)
    {
        if (_isDragging) return;
        SpawnCompoundAtRandomPosition(data);
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
        // Compute spawn rect centered inside the SpawnArea rect using the explicit _spawnAreaSize.
        Rect rect = SpawnArea.rectTransform.rect;

        // Clamp the requested spawn area size so it never exceeds the SpawnArea rect
        float halfWidth = Mathf.Min(_spawnAreaSize.x * 0.5f, rect.width * 0.5f);
        float halfHeight = Mathf.Min(_spawnAreaSize.y * 0.5f, rect.height * 0.5f);


        // Apply the optional local-space center offset (in the rect's coordinate space)
        Vector2 rectCenter = rect.center + _spawnAreaCenter;

        Vector2 randomPosition = new Vector2(
            Random.Range(rectCenter.x - halfWidth, rectCenter.x + halfWidth),
            Random.Range(rectCenter.y - halfHeight, rectCenter.y + halfHeight)
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

        GameObject newCompound;
        if (DraggableHolder.Instance != null)
            newCompound = Instantiate(compoundPrefab, position, Quaternion.identity, DraggableHolder.Instance.transform);
        else
            newCompound = Instantiate(compoundPrefab, position, Quaternion.identity);

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

            // Clamp half-sizes so the visualized area never exceeds the parent rect
            float halfWidth = Mathf.Min(_spawnAreaSize.x * 0.5f, size.x * 0.5f);
            float halfHeight = Mathf.Min(_spawnAreaSize.y * 0.5f, size.y * 0.5f);

            // Compute the local-space center offset and apply it when transforming to world
            Vector3 centerLocal = new Vector3(_spawnAreaCenter.x, _spawnAreaCenter.y, 0);
            Vector3 min = rectTransform.TransformPoint(centerLocal + new Vector3(-halfWidth, -halfHeight, 0));
            Vector3 max = rectTransform.TransformPoint(centerLocal + new Vector3(halfWidth, halfHeight, 0));

            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(min.x, min.y, 0), new Vector3(max.x, min.y, 0));
            Gizmos.DrawLine(new Vector3(max.x, min.y, 0), new Vector3(max.x, max.y, 0));
            Gizmos.DrawLine(new Vector3(max.x, max.y, 0), new Vector3(min.x, max.y, 0));
            Gizmos.DrawLine(new Vector3(min.x, max.y, 0), new Vector3(min.x, min.y, 0));
        }
    }

    // Extracts the base name of a compound from its SO name (e.g., "C_Carbonate" -> "Carbonate")
    private string GetCompoundBaseName(string soName)
    {
        if (string.IsNullOrEmpty(soName))
            return soName;

        int underscoreIndex = soName.IndexOf('_');
        return underscoreIndex >= 0 ? soName.Substring(underscoreIndex + 1) : soName;
    }
}