using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

// Spawns element objects and manages element spawn buttons
public class ElementSpawner : MonoBehaviour
{
    private static ElementSpawner _instance;
    public static ElementSpawner Instance => _instance;

    [SerializeField] private GameObject elementPrefab; // Prefab for spawning elements
    public GameObject ElementPrefab => elementPrefab;

    [SerializeField] private Image _spawnArea; // UI area where elements can spawn
    public Image SpawnArea => _spawnArea;

    [SerializeField] private Button[] _spawnButtons; // Buttons to spawn each element
    private Sprite[] _spawnButtonSprites; // Stores the normal sprites for each button

    [SerializeField] private Sprite _hiddenButtonSprite; // Sprite for locked/hidden buttons
    public Sprite HiddenButtonSprite => _hiddenButtonSprite;

    [SerializeField] private Vector2 _spawnAreaSize = new Vector2(100, 100); // Width/Height of the allowed spawn area (centered inside the SpawnArea)
    public Vector2 SpawnAreaSize => _spawnAreaSize;
    [SerializeField] private Vector2 _spawnAreaCenter = Vector2.zero; // Local offset (in rect local space) from the SpawnArea center
    public Vector2 SpawnAreaCenter => _spawnAreaCenter;
    [SerializeField] private bool _unlockAllElements = false; // If true, unlock all buttons at start

    [SerializeField] private List<ElementData> _elementDataList = new List<ElementData>(); // List of element data for each button

    // Track whether the player is currently dragging a spawned object (to suppress click-spawns)
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
                Debug.LogError($"ElementSpawner: Button at index {i} does not have a sprite.");
            }
            _spawnButtons[i].interactable = false;
            _spawnButtons[i].targetGraphic.GetComponent<Image>().sprite = _hiddenButtonSprite;
        }

        // Load all element data from Resources and add to the list
        _elementDataList.Clear();
        ElementData[] loadedElements = Resources.LoadAll<ElementData>("SOs/Elements");
        _elementDataList.AddRange(loadedElements);

        // Set up button listeners so each button spawns the right element
        for (int i = 0; i < _spawnButtons.Length; i++)
        {
            Button btn = _spawnButtons[i];
            string btnName = btn.name;

            ElementData matchedData = _elementDataList.Find(e => e != null
                && btnName != null
                && btnName.IndexOf(SOHelpers.StripCommonPrefix(e.name), System.StringComparison.OrdinalIgnoreCase) >= 0);

            if (matchedData != null)
            {
                ElementData dataForClosure = matchedData;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnSpawnButtonClicked(dataForClosure));

                SpawnDragHandler dragHandler = btn.gameObject.GetComponent<SpawnDragHandler>() ?? btn.gameObject.AddComponent<SpawnDragHandler>();
                dragHandler.Init(this, dataForClosure);
            }
            else
            {
                Debug.LogWarning($"ElementSpawner: No ElementData found matching button name '{btnName}'.");
            }
        }
    }

    // suppress click-spawn when a drag is in progress
    private void OnSpawnButtonClicked(ElementData data)
    {
        if (_isDragging) return;
        SpawnElementAtRandomPosition(data);
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
        if (elementPrefab == null)
        {
            Debug.LogWarning("ElementSpawner: No element prefab assigned.");
        }

        // Optionally unlock all buttons at the start
        if (_unlockAllElements)
        {
            UnlockAllButtons();
        }
    }

    // Spawn an element at a random position inside the spawn area
    public GameObject SpawnElementAtRandomPosition(ElementData data, int isotopeNumber = -1)
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

        return SpawnElementAtPosition(data, isotopeNumber, worldPosition);
    }

    // Spawn an element at a specific position
    public GameObject SpawnElementAtPosition(ElementData data, int isotopeNumber, Vector3 position)
    {
        if (elementPrefab == null)
        {
            Debug.LogError("ElementSpawner: Cannot spawn element, prefab is not assigned.");
            return null;
        }

        GameObject newElement = Instantiate(elementPrefab, position, Quaternion.identity);

        Element elementComponent = newElement.GetComponent<Element>();
        if (elementComponent != null)
        {
            elementComponent.data = data;
            elementComponent.isotopeNumber = isotopeNumber < 0 ? data.defaultIsotopeNumber : isotopeNumber;
            elementComponent.UpdateDataVisuals();
        }
        else
        {
            Debug.LogError("ElementSpawner: Spawned prefab does not have an Element component.");
            Destroy(newElement);
            return null;
        }

        // Add the new element to the DraggableHolder
        if (DraggableHolder.Instance != null)
        {
            DraggableHolder.Instance.AddDraggable(newElement);
        }

        return newElement;
    }

    // Example spawn methods for each element, using the ScriptableObjects
    public void SpawnHydrogen()
    {
        if (_elementDataList.Count > 0 && _elementDataList[0] != null)
            SpawnElementAtRandomPosition(_elementDataList[0], 2);
    }
    public void SpawnHelium()
    {
        if (_elementDataList.Count > 1 && _elementDataList[1] != null)
            SpawnElementAtRandomPosition(_elementDataList[1], 4);
    }
    public void SpawnBeryllium()
    {
        if (_elementDataList.Count > 2 && _elementDataList[2] != null)
            SpawnElementAtRandomPosition(_elementDataList[2], 8);
    }
    public void SpawnCarbon()
    {
        if (_elementDataList.Count > 3 && _elementDataList[3] != null)
            SpawnElementAtRandomPosition(_elementDataList[3], 12);
    }
    public void SpawnTitanium()
    {
        if (_elementDataList.Count > 4 && _elementDataList[4] != null)
            SpawnElementAtRandomPosition(_elementDataList[4]);
    }
    public void SpawnIron()
    {
        if (_elementDataList.Count > 5 && _elementDataList[5] != null)
            SpawnElementAtRandomPosition(_elementDataList[5]);
    }
    public void SpawnCopper()
    {
        if (_elementDataList.Count > 6 && _elementDataList[6] != null)
            SpawnElementAtRandomPosition(_elementDataList[6]);
    }
    public void SpawnCalcium()
    {
        if (_elementDataList.Count > 7 && _elementDataList[7] != null)
            SpawnElementAtRandomPosition(_elementDataList[7]);
    }
    public void SpawnBarium()
    {
        if (_elementDataList.Count > 8 && _elementDataList[8] != null)
            SpawnElementAtRandomPosition(_elementDataList[8]);
    }
    public void SpawnSilicon()
    {
        if (_elementDataList.Count > 9 && _elementDataList[9] != null)
            SpawnElementAtRandomPosition(_elementDataList[9]);
    }
    public void SpawnAluminum()
    {
        if (_elementDataList.Count > 10 && _elementDataList[10] != null)
            SpawnElementAtRandomPosition(_elementDataList[10]);
    }
    public void SpawnMagnesium()
    {
        if (_elementDataList.Count > 11 && _elementDataList[11] != null)
            SpawnElementAtRandomPosition(_elementDataList[11]);
    }

    public void SpawnHeat()
    {
        if (_elementDataList.Count > 12 && _elementDataList[12] != null)
            SpawnElementAtRandomPosition(_elementDataList[12]);
    }

    public void SpawnSulfur()
    {
        if (_elementDataList.Count > 13 && _elementDataList[13] != null)
            SpawnElementAtRandomPosition(_elementDataList[13]);
    }

    public void SpawnPhosphorus()
    {
        if (_elementDataList.Count > 14 && _elementDataList[14] != null)
            SpawnElementAtRandomPosition(_elementDataList[14]);
    }

    public void SpawnFluorine()
    {
        if (_elementDataList.Count > 15 && _elementDataList[15] != null)
            SpawnElementAtRandomPosition(_elementDataList[15]);
    }

    public void SpawnChlorine()
    {
        if (_elementDataList.Count > 16 && _elementDataList[16] != null)
            SpawnElementAtRandomPosition(_elementDataList[16]);
    }

    public void SpawnSodium()
    {
        if (_elementDataList.Count > 17 && _elementDataList[17] != null)
            SpawnElementAtRandomPosition(_elementDataList[17]);
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

    // Draws the spawn area and buffer in the editor for debugging
    private void OnDrawGizmos()
    {
        if (_spawnArea != null)
        {
            RectTransform rectTransform = _spawnArea.rectTransform;
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            // Draw the original spawn area
            Gizmos.color = Color.green;
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }

            // Calculate and draw the exact spawn area centered inside the spawn rect
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

    // Extracts the base name of an element from its SO name (e.g., "H_Hydrogen" -> "Hydrogen")
    private string GetElementBaseName(string soName)
    {
        if (string.IsNullOrEmpty(soName))
            return soName;

        int underscoreIndex = soName.IndexOf('_');
        return underscoreIndex >= 0 ? soName.Substring(underscoreIndex + 1) : soName;
    }
}
