using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

    [SerializeField] private Vector2 _buffer = new Vector2(100, 100); // Padding from spawn area edges
    [SerializeField] private bool _unlockAllElements = false; // If true, unlock all buttons at start

    [SerializeField] private List<ElementData> _elementDataList = new List<ElementData>(); // List of element data for each button

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

            // Find the matching element by name (use the part after the first underscore in SO names)
            ElementData matchedData = _elementDataList.Find(e => e != null
                && btnName != null
                && btnName.IndexOf(GetElementBaseName(e.name), System.StringComparison.OrdinalIgnoreCase) >= 0);

            if (matchedData != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SpawnElementAtRandomPosition(matchedData));
            }
            else
            {
                Debug.LogWarning($"ElementSpawner: No ElementData found matching button name '{btnName}'.");
            }
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
        Vector2 randomPosition = new Vector2
        (
            Random.Range(SpawnArea.rectTransform.rect.xMin + _buffer.x, SpawnArea.rectTransform.rect.xMax - _buffer.x),
            Random.Range(SpawnArea.rectTransform.rect.yMin + _buffer.y, SpawnArea.rectTransform.rect.yMax - _buffer.y)
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

            // Calculate and draw the buffered spawn area
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

    // Extracts the base name of an element from its SO name (e.g., "H_Hydrogen" -> "Hydrogen")
    private string GetElementBaseName(string soName)
    {
        if (string.IsNullOrEmpty(soName))
            return soName;

        int underscoreIndex = soName.IndexOf('_');
        return underscoreIndex >= 0 ? soName.Substring(underscoreIndex + 1) : soName;
    }
}
