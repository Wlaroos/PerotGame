using UnityEngine;
using UnityEngine.UI;

public class ElementSpawner : MonoBehaviour
{
    private static ElementSpawner _instance;
    public static ElementSpawner Instance => _instance;

    [SerializeField] private GameObject elementPrefab;
    public GameObject ElementPrefab => elementPrefab;

    [SerializeField] private Image _spawnArea;
    public Image SpawnArea => _spawnArea;

    [SerializeField] private Button[] _spawnButtons;
    private Sprite[] _spawnButtonSprites;

    [SerializeField] private Sprite _hiddenButtonSprite;
    public Sprite HiddenButtonSprite => _hiddenButtonSprite;

    [SerializeField] private Vector2 _buffer = new Vector2(100, 100);
    [SerializeField] private bool _unlockAllElements = false;

    // NEW: Assign these in the inspector, one for each button/type
    [SerializeField] private ElementData[] _elementDataList;

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
                Debug.LogError($"ElementSpawner: Button at index {i} does not have a sprite.");
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
        if (elementPrefab == null)
        {
            Debug.LogWarning("ElementSpawner: No element prefab assigned.");
        }

        if (_unlockAllElements)
        {
            UnlockAllButtons();
        }
    }

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
        if (_elementDataList.Length > 0 && _elementDataList[0] != null)
            SpawnElementAtRandomPosition(_elementDataList[0], 2);
    }
    public void SpawnHelium()
    {
        if (_elementDataList.Length > 1 && _elementDataList[1] != null)
            SpawnElementAtRandomPosition(_elementDataList[1], 4);
    }
    public void SpawnBeryllium()
    {
        if (_elementDataList.Length > 2 && _elementDataList[2] != null)
            SpawnElementAtRandomPosition(_elementDataList[2], 8);
    }
    public void SpawnCarbon()
    {
        if (_elementDataList.Length > 3 && _elementDataList[3] != null)
            SpawnElementAtRandomPosition(_elementDataList[3], 12);
    }
    public void SpawnTitanium()
    {
        if (_elementDataList.Length > 4 && _elementDataList[4] != null)
            SpawnElementAtRandomPosition(_elementDataList[4]);
    }
    public void SpawnIron()
    {
        if (_elementDataList.Length > 5 && _elementDataList[5] != null)
            SpawnElementAtRandomPosition(_elementDataList[5]);
    }
    public void SpawnCopper()
    {
        if (_elementDataList.Length > 6 && _elementDataList[6] != null)
            SpawnElementAtRandomPosition(_elementDataList[6]);
    }
    public void SpawnCalcium()
    {
        if (_elementDataList.Length > 7 && _elementDataList[7] != null)
            SpawnElementAtRandomPosition(_elementDataList[7]);
    }
    public void SpawnBarium()
    {
        if (_elementDataList.Length > 8 && _elementDataList[8] != null)
            SpawnElementAtRandomPosition(_elementDataList[8]);
    }
    public void SpawnSilicon()
    {
        if (_elementDataList.Length > 9 && _elementDataList[9] != null)
            SpawnElementAtRandomPosition(_elementDataList[9]);
    }
    public void SpawnAluminum()
    {
        if (_elementDataList.Length > 10 && _elementDataList[10] != null)
            SpawnElementAtRandomPosition(_elementDataList[10]);
    }
    public void SpawnMagnesium()
    {
        if (_elementDataList.Length > 11 && _elementDataList[11] != null)
            SpawnElementAtRandomPosition(_elementDataList[11]);
    }

    public void UnlockAllButtons()
    {
        for (int i = 0; i < _spawnButtons.Length; i++)
        {
            _spawnButtons[i].interactable = true;
            _spawnButtons[i].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[i];
        }
    }

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
}
