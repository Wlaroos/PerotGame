using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Assertions.Must;

public class ElementSpawner : MonoBehaviour
{
    private static ElementSpawner _instance;
    public static ElementSpawner Instance => _instance;

    [SerializeField] private GameObject elementPrefab;
    [SerializeField] private Image _spawnArea;
    [SerializeField] private Button[] _spawnButtons;
    [SerializeField] private Sprite _hiddenButtonSprite;
    [SerializeField] private Vector2 _spawnAreaSize = new Vector2(100, 100);
    [SerializeField] private Vector2 _spawnAreaCenter = Vector2.zero;
    [SerializeField] private bool _unlockAllElements = false;

    private List<ElementData> _elementDataList = new List<ElementData>();
    private Sprite[] _spawnButtonSprites;
    private ElementData[] _buttonDataMap;
    private bool _isDragging;

    public bool IsDragging => _isDragging;
    public void SetDragging(bool value) => _isDragging = value;

    private void Awake()
    {
        SetupSingleton();
        InitializeButtons();
        LoadElementData();
        SetupButtonListeners();

        if (_unlockAllElements) UnlockAllButtons();
    }

    private void SetupSingleton()
    {
        if (_instance != null && _instance != this) Destroy(gameObject);
        else _instance = this;
    }

    private void InitializeButtons()
    {
        if (_spawnButtons == null) _spawnButtons = new Button[0];
        _spawnButtonSprites = new Sprite[_spawnButtons.Length];
        _buttonDataMap = new ElementData[_spawnButtons.Length];

        for (int i = 0; i < _spawnButtons.Length; i++)
        {
            var button = _spawnButtons[i];
            if (button == null) continue;
            var img = button.targetGraphic?.GetComponent<Image>();
            _spawnButtonSprites[i] = img != null ? img.sprite : null;
            button.interactable = false;
            if (img != null) img.sprite = _hiddenButtonSprite;
        }
    }

    private void LoadElementData()
    {
        _elementDataList.Clear();
        _elementDataList.AddRange(Resources.LoadAll<ElementData>("SOs/Elements"));
    }

    private void SetupButtonListeners()
    {
        for (int i = 0; i < _spawnButtons.Length; i++)
        {
            var button = _spawnButtons[i];
            if (button == null) continue;

            var matchedData = FindMatchingElementData(button.name);
            if (matchedData != null)
            {
                _buttonDataMap[i] = matchedData;
                SetupButton(button, matchedData);
            }
            else
            {
                Debug.LogWarning($"ElementSpawner: No ElementData found matching button name '{button.name}'.");
            }
        }
    }

    private ElementData FindMatchingElementData(string buttonName)
    {
        return _elementDataList.Find(data =>
            data != null && buttonName.IndexOf(SOHelpers.StripCommonPrefix(data.name), System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void SetupButton(Button button, ElementData data)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSpawnButtonClicked(button, data));

        var dragHandler = button.gameObject.GetComponent<SpawnDragHandler>() ?? button.gameObject.AddComponent<SpawnDragHandler>();
        dragHandler.Init(this, data);
    }

    private void OnSpawnButtonClicked(Button button, ElementData data)
    {
        //if (!_isDragging) SpawnElementAtRandomPosition(data);
        button.transform.localScale = new Vector3(1f, 1f, 1f);
        if (!_isDragging) StartCoroutine(FlipTile(button, data));
    }

    private IEnumerator FlipTile(Button button, ElementData data)
    {
        float elapsedTime = 0f;
        float flipDuration = 0.25f;
        Image childImage = button.transform.GetChild(0).gameObject.GetComponent<Image>();

        while (elapsedTime < flipDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / flipDuration);
            float scaleX = Mathf.Lerp(1f, 0f, t);

            button.transform.localScale = new Vector3(scaleX, 1f, 1f);
            yield return null;
        }

        if (childImage != null)
        {
            Sprite current = childImage.sprite;
            if (current == data.altElementSprite)
            {
                childImage.sprite = data.elementSprite;
            }
            else
            {
                childImage.sprite = data.altElementSprite;
            }
        }

        elapsedTime = 0f;

        while (elapsedTime < flipDuration)        
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / flipDuration);
            float scaleX = Mathf.Lerp(0f, 1f, t);
            button.transform.localScale = new Vector3(scaleX, 1f, 1f);
            yield return null;
        }
    }

    public GameObject SpawnElementAtRandomPosition(ElementData data, int isotopeNumber = -1)
    {
        Vector3 worldPosition = GetRandomSpawnPosition();
        return SpawnElementAtPosition(data, isotopeNumber, worldPosition);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Rect rect = _spawnArea.rectTransform.rect;
        float halfWidth = Mathf.Min(_spawnAreaSize.x * 0.5f, rect.width * 0.5f);
        float halfHeight = Mathf.Min(_spawnAreaSize.y * 0.5f, rect.height * 0.5f);
        Vector2 rectCenter = rect.center + _spawnAreaCenter;

        Vector2 randomPosition = new Vector2(
            Random.Range(rectCenter.x - halfWidth, rectCenter.x + halfWidth),
            Random.Range(rectCenter.y - halfHeight, rectCenter.y + halfHeight)
        );

        return _spawnArea.rectTransform.TransformPoint(randomPosition);
    }

    public GameObject SpawnElementAtPosition(ElementData data, int isotopeNumber, Vector3 position)
    {
        if (elementPrefab == null)
        {
            Debug.LogError("ElementSpawner: Cannot spawn element, prefab is not assigned.");
            return null;
        }

        GameObject newElement = Instantiate(elementPrefab, position, Quaternion.identity);
        if (!InitializeElement(newElement, data, isotopeNumber)) return null;

        DraggableHolder.Instance?.AddDraggable(newElement);
        return newElement;
    }

    private bool InitializeElement(GameObject element, ElementData data, int isotopeNumber)
    {
        var elementComponent = element.GetComponent<Element>();
        if (elementComponent == null)
        {
            Debug.LogError("ElementSpawner: Spawned prefab does not have an Element component.");
            Destroy(element);
            return false;
        }

        elementComponent.data = data;
        elementComponent.isotopeNumber = isotopeNumber < 0 ? data.defaultIsotopeNumber : isotopeNumber;
        elementComponent.UpdateDataVisuals();
        return true;
    }

    public void UnlockAllButtons()
    {
        for (int i = 0; i < _spawnButtons.Length; i++)
        {
            var button = _spawnButtons[i];
            if (button == null) continue;
            button.interactable = true;

            var img = button.targetGraphic?.GetComponent<Image>();
            if (img != null)
            {
                // Use mapped data for color if present (more robust than using matching by index)
                var mapped = (i >= 0 && i < _buttonDataMap.Length) ? _buttonDataMap[i] : null;
                if (mapped != null)
                {
                    img.sprite = _spawnButtonSprites[i];
                    img.color = SOHelpers.GetColorFromData(mapped);
                }
                else
                {
                    // fallback: attempt to use element list at same index
                    img.sprite = _spawnButtonSprites[i];
                    img.color = (i < _elementDataList.Count && _elementDataList[i] != null)
                        ? SOHelpers.GetColorFromData(_elementDataList[i])
                        : Color.white;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (_spawnArea == null) return;

        DrawGizmoRect(_spawnArea.rectTransform, Color.green);
        DrawGizmoRect(_spawnArea.rectTransform, Color.red, _spawnAreaSize, _spawnAreaCenter);
    }

    private void DrawGizmoRect(RectTransform rectTransform, Color color, Vector2? sizeOverride = null, Vector2? centerOffset = null)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Gizmos.color = color;
        for (int i = 0; i < 4; i++) Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);

        if (sizeOverride.HasValue)
        {
            Vector2 size = sizeOverride.Value;
            Vector2 center = rectTransform.rect.center + (centerOffset ?? Vector2.zero);

            float halfWidth = Mathf.Min(size.x * 0.5f, rectTransform.rect.width * 0.5f);
            float halfHeight = Mathf.Min(size.y * 0.5f, rectTransform.rect.height * 0.5f);

            Vector3 min = rectTransform.TransformPoint(center + new Vector2(-halfWidth, -halfHeight));
            Vector3 max = rectTransform.TransformPoint(center + new Vector2(halfWidth, halfHeight));

            Gizmos.DrawLine(new Vector3(min.x, min.y, 0), new Vector3(max.x, min.y, 0));
            Gizmos.DrawLine(new Vector3(max.x, min.y, 0), new Vector3(max.x, max.y, 0));
            Gizmos.DrawLine(new Vector3(max.x, max.y, 0), new Vector3(min.x, max.y, 0));
            Gizmos.DrawLine(new Vector3(min.x, max.y, 0), new Vector3(min.x, min.y, 0));
        }
    }
}
