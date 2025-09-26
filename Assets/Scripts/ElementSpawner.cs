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

    [SerializeField] private Vector2 _buffer = new Vector2(100,100); // Serialized buffer field to adjust in the Inspector

    private void OnDisable()
    {
        if (CraftingManager.Instance != null)
        {
            CraftingManager.Instance.OnHydrogen2Crafted.RemoveListener(FirstHydrogen);
            CraftingManager.Instance.OnHelium4Crafted.RemoveListener(FirstHelium);
            CraftingManager.Instance.OnBeryllium8Crafted.RemoveListener(FirstBeryllium);
            CraftingManager.Instance.OnCarbon12Crafted.RemoveListener(FirstCarbon);
        }
    }

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
            _spawnButtons[i].interactable = false; // Disable all buttons initially
            _spawnButtons[i].targetGraphic.GetComponent<Image>().sprite = _hiddenButtonSprite; // Set to hidden sprite
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

        if (CraftingManager.Instance != null)
        {
            CraftingManager.Instance.OnHydrogen2Crafted.AddListener(FirstHydrogen);
            CraftingManager.Instance.OnHelium4Crafted.AddListener(FirstHelium);
            CraftingManager.Instance.OnBeryllium8Crafted.AddListener(FirstBeryllium);
            CraftingManager.Instance.OnCarbon12Crafted.AddListener(FirstCarbon);
        }

        UnlockAllButtons();
    }

    public GameObject SpawnElementAtRandomPosition(Element.ElementType type, int isotopeNumber = 1)
    {
        Vector2 randomPosition = new Vector2
        (
            Random.Range(SpawnArea.rectTransform.rect.xMin + _buffer.x, SpawnArea.rectTransform.rect.xMax - _buffer.x),
            Random.Range(SpawnArea.rectTransform.rect.yMin + _buffer.y, SpawnArea.rectTransform.rect.yMax - _buffer.y)
        );

        Vector3 worldPosition = SpawnArea.rectTransform.TransformPoint(randomPosition);

        //Debug.Log(worldPosition);

        return SpawnElementAtPosition(type, isotopeNumber, worldPosition);
    }

    public GameObject SpawnElementAtPosition(Element.ElementType type, int isotopeNumber, Vector3 position)
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
            elementComponent.SetElement(type, isotopeNumber);
        }
        else
        {
            Debug.LogError("ElementSpawner: Spawned prefab does not have an Element component.");
            Destroy(newElement);
            return null;
        }

        return newElement;
    }


    public void SpawnHydrogen()   { SpawnElementAtRandomPosition(Element.ElementType.Hydrogen, 1); }
    public void SpawnHelium()     { SpawnElementAtRandomPosition(Element.ElementType.Helium, 1); }
    public void SpawnBeryllium()  { SpawnElementAtRandomPosition(Element.ElementType.Beryllium, 1); }
    public void SpawnCarbon()     { SpawnElementAtRandomPosition(Element.ElementType.Carbon, 1); }
    public void SpawnTitanium()   { SpawnElementAtRandomPosition(Element.ElementType.Titanium, 1); }
    public void SpawnIron()       { SpawnElementAtRandomPosition(Element.ElementType.Iron, 1); }
    public void SpawnCopper()     { SpawnElementAtRandomPosition(Element.ElementType.Copper, 1); }
    public void SpawnCalcium()    { SpawnElementAtRandomPosition(Element.ElementType.Calcium, 1); }
    public void SpawnBarium()     { SpawnElementAtRandomPosition(Element.ElementType.Barium, 1); }
    public void SpawnSilicon()    { SpawnElementAtRandomPosition(Element.ElementType.Silicon, 1); }
    public void SpawnAluminum()   { SpawnElementAtRandomPosition(Element.ElementType.Aluminum, 1); }
    public void SpawnMagnesium()  { SpawnElementAtRandomPosition(Element.ElementType.Magnesium, 1); }

    private void FirstHydrogen()
    {
        _spawnButtons[0].interactable = true;
        _spawnButtons[0].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[0];
    }
    private void FirstHelium()
    {
        _spawnButtons[1].interactable = true;
        _spawnButtons[1].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[1];
    }
    private void FirstBeryllium()
    {
        _spawnButtons[2].interactable = true;
        _spawnButtons[2].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[2];
    }
    private void FirstCarbon()
    {
        _spawnButtons[3].interactable = true;
        _spawnButtons[3].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[3];
    }
    private void FirstTitanium()
    {
        _spawnButtons[4].interactable = true;
        _spawnButtons[4].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[4];
    }
    private void FirstIron()
    {
        _spawnButtons[5].interactable = true;
        _spawnButtons[5].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[5];
    }
    private void FirstCopper()
    {
        _spawnButtons[6].interactable = true;
        _spawnButtons[6].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[6];
    }
    private void FirstCalcium()
    {
        _spawnButtons[7].interactable = true;
        _spawnButtons[7].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[7];
    }
    private void FirstBarium()
    {
        _spawnButtons[8].interactable = true;
        _spawnButtons[8].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[8];
    }
    private void FirstSilicon()
    {
        _spawnButtons[9].interactable = true;
        _spawnButtons[9].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[9];
    }
    private void FirstAluminum()
    {
        _spawnButtons[10].interactable = true;
        _spawnButtons[10].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[10];
    }
    private void FirstMagnesium()
    {
        _spawnButtons[11].interactable = true;
        _spawnButtons[11].targetGraphic.GetComponent<Image>().sprite = _spawnButtonSprites[11];
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
