using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using TMPro;

// Spawns compound objects and manages compound spawn buttons
public class CompoundSpawner : MonoBehaviour
{
    private static CompoundSpawner _instance;
    public static CompoundSpawner Instance => _instance;

    [SerializeField] private GameObject compoundPrefab;
    [SerializeField] private Image _spawnArea;
    [SerializeField] private Button[] _spawnButtons;
    [SerializeField] private Sprite _hiddenButtonSprite;
    [SerializeField] private Vector2 _spawnAreaSize = new Vector2(850, 350);
    [SerializeField] private Vector2 _spawnAreaCenter = new Vector2(0, -350);
    [SerializeField] private bool _unlockAllCompounds = false;
    [SerializeField] private GameObject _silicateDropDown;

    private CompoundData[] _compoundDataList;
    private Sprite[] _spawnButtonSprites;
    private CompoundData[] _buttonDataMap; // map per-button assigned SO
    private bool _isDragging;
    public bool IsDragging => _isDragging;
    public void SetDragging(bool value) => _isDragging = value;

    // Silicate dropdown state
    private Dropdown _silicateDropdownComp;
    private TMP_Dropdown _silicateTmpDropdown;
    private Sprite[] _silicateOptionSprites;
    private int _silicateButtonIndex = -1;
    private CompoundData _currentSilicateVariant;

    private void Awake()
    {
        SetupSingleton();
        EnsureArrays();
        CacheButtonSpritesAndLock();
        LoadCompoundDataIfNeeded();
        CacheDefaultSilicateVariant();
        SetupSpawnButtons();
        CacheSilicateButtonIndex();
        HookSilicateDropdown();
    }

    private void SetupSingleton()
    {
        if (_instance != null && _instance != this) Destroy(gameObject);
        else _instance = this;
    }

    private void EnsureArrays()
    {
        if (_spawnButtons == null) _spawnButtons = new Button[0];
        _spawnButtonSprites = new Sprite[_spawnButtons.Length];
        _buttonDataMap = new CompoundData[_spawnButtons.Length];
    }

    private void CacheButtonSpritesAndLock()
    {
        for (int i = 0; i < _spawnButtons.Length; i++)
        {
            var btn = _spawnButtons[i];
            if (btn == null) continue;
            var img = btn.targetGraphic as Image;
            _spawnButtonSprites[i] = img != null ? img.sprite : null;
            if (_spawnButtonSprites[i] == null)
                Debug.LogError($"CompoundSpawner: Button at index {i} does not have a sprite.");
            btn.interactable = false;
            if (img != null) img.sprite = _hiddenButtonSprite;
        }
    }

    private void LoadCompoundDataIfNeeded()
    {
        if (_compoundDataList == null || _compoundDataList.Length == 0)
        {
            var loaded = Resources.LoadAll<CompoundData>("SOs/Compounds");
            if (loaded != null && loaded.Length > 0) _compoundDataList = loaded;
        }
    }

    private void CacheDefaultSilicateVariant()
    {
        if (_compoundDataList == null) return;
        _currentSilicateVariant = System.Array.Find(_compoundDataList,
            c => c != null && c.name.IndexOf("Silicate", System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void SetupSpawnButtons()
    {
        for (int i = 0; i < _spawnButtons.Length; i++)
        {
            Button btn = _spawnButtons[i];
            if (btn == null) continue;
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
                _buttonDataMap[i] = matchedData; // record mapping
                btn.onClick.RemoveAllListeners();
                CompoundData dataCopy = matchedData; // capture local for closure safety
                btn.onClick.AddListener(() => OnSpawnButtonClicked(dataCopy));

                SpawnDragHandler dragHandler = btn.gameObject.GetComponent<SpawnDragHandler>() ?? btn.gameObject.AddComponent<SpawnDragHandler>();
                dragHandler.Init(this, dataCopy);
            }
            else
            {
                Debug.LogWarning($"CompoundSpawner: No CompoundData found matching button name '{btnName}'.");
            }
        }
    }

    private CompoundData FindMatchingCompound(string buttonName)
    {
        if (_compoundDataList == null || string.IsNullOrEmpty(buttonName)) return null;
        return System.Array.Find(_compoundDataList, c =>
            c != null && buttonName.IndexOf(GetCompoundBaseName(c.name), System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void CacheSilicateButtonIndex()
    {
        _silicateButtonIndex = System.Array.FindIndex(_spawnButtons, b => b != null && b.name.IndexOf("Silicate", System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void HookSilicateDropdown()
    {
        if (_silicateDropDown == null) return;

        _silicateDropdownComp = _silicateDropDown.GetComponent<Dropdown>();
        if (TryHookUnityDropdown()) return;

        _silicateTmpDropdown = _silicateDropDown.GetComponent<TMP_Dropdown>();
        if (TryHookTmpDropdown()) return;
    }

    private bool TryHookUnityDropdown()
    {
        if (_silicateDropdownComp == null || _silicateDropdownComp.options == null || _silicateDropdownComp.options.Count == 0) return false;
        _silicateOptionSprites = new Sprite[_silicateDropdownComp.options.Count];
        for (int i = 0; i < _silicateDropdownComp.options.Count; i++) _silicateOptionSprites[i] = _silicateDropdownComp.options[i].image;
        _silicateDropdownComp.onValueChanged.AddListener(OnSilicateDropdownChanged);
        OnSilicateDropdownChanged(_silicateDropdownComp.value);
        return true;
    }

    private bool TryHookTmpDropdown()
    {
        if (_silicateTmpDropdown == null || _silicateTmpDropdown.options == null || _silicateTmpDropdown.options.Count == 0) return false;
        _silicateOptionSprites = new Sprite[_silicateTmpDropdown.options.Count];
        for (int i = 0; i < _silicateTmpDropdown.options.Count; i++) _silicateOptionSprites[i] = _silicateTmpDropdown.options[i].image;
        _silicateTmpDropdown.onValueChanged.AddListener(OnSilicateDropdownChanged);
        OnSilicateDropdownChanged(_silicateTmpDropdown.value);
        return true;
    }

    private void OnSpawnButtonClicked(CompoundData data)
    {
        if (_isDragging) return;
        SpawnCompoundAtRandomPosition(data);
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Start()
    {
        if (_unlockAllCompounds) UnlockAllButtons();
    }

    public GameObject SpawnCompoundAtRandomPosition(CompoundData data)
    {
        Vector3 worldPos = GetRandomSpawnPosition();
        return SpawnCompoundAtPosition(data, worldPos);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Rect rect = _spawnArea.rectTransform.rect;
        float halfW = Mathf.Min(_spawnAreaSize.x * 0.5f, rect.width * 0.5f);
        float halfH = Mathf.Min(_spawnAreaSize.y * 0.5f, rect.height * 0.5f);
        Vector2 center = rect.center + _spawnAreaCenter;
        Vector2 rand = new Vector2(Random.Range(center.x - halfW, center.x + halfW), Random.Range(center.y - halfH, center.y + halfH));
        return _spawnArea.rectTransform.TransformPoint(rand);
    }

    public GameObject SpawnCompoundAtPosition(CompoundData data, Vector3 position)
    {
        if (compoundPrefab == null)
        {
            Debug.LogError("CompoundSpawner: Cannot spawn compound, prefab is not assigned.");
            return null;
        }

        GameObject newCompound = Instantiate(compoundPrefab, position, Quaternion.identity);
        var comp = newCompound.GetComponent<Compound>();
        if (comp == null)
        {
            Debug.LogError("CompoundSpawner: Spawned prefab does not have a Compound component.");
            Destroy(newCompound);
            return null;
        }

        comp.data = data;
        comp.UpdateDataVisuals();
        DraggableHolder.Instance?.AddDraggable(newCompound);
        return newCompound;
    }

    // Compact wrappers for existing named spawn methods (safe for UI binding)
    private void SpawnByIndex(int i)
    {
        if (_compoundDataList != null && i >= 0 && i < _compoundDataList.Length && _compoundDataList[i] != null)
            SpawnCompoundAtRandomPosition(_compoundDataList[i]);
    }

    public void SpawnOxide() => SpawnByIndex(0);
    public void SpawnCarbonate() => SpawnByIndex(1);
    public void SpawnPhosphate() => SpawnByIndex(2);
    public void SpawnSulfate() => SpawnByIndex(4);
    public void SpawnSilicate()
    {
        if (_currentSilicateVariant != null) { SpawnCompoundAtRandomPosition(_currentSilicateVariant); return; }
        var first = System.Array.Find(_compoundDataList, c => c != null && c.name.IndexOf("Silicate", System.StringComparison.OrdinalIgnoreCase) >= 0);
        if (first != null) SpawnCompoundAtRandomPosition(first);
    }
    public void SpawnSilicate0() => SpawnByIndex(3);
    public void SpawnSilicate1() => SpawnByIndex(5);
    public void SpawnSilicate2() => SpawnByIndex(6);
    public void SpawnSilicate3() => SpawnByIndex(7);
    public void SpawnSilicate4() => SpawnByIndex(8);

    public void UnlockAllButtons()
    {
        for (int i = 0; i < _spawnButtons.Length; i++)
        {
            var btn = _spawnButtons[i];
            if (btn == null) continue;
            btn.interactable = true;
            var img = btn.targetGraphic as Image ?? btn.image;
            if (img != null && _spawnButtonSprites != null && i < _spawnButtonSprites.Length && _spawnButtonSprites[i] != null)
            {
                img.sprite = _spawnButtonSprites[i];
            }

            // Use mapped data to pick the color if available
            var mapped = (i >= 0 && i < _buttonDataMap.Length) ? _buttonDataMap[i] : null;
            if (img != null)
            {
                img.color = (mapped != null)
                    ? SOHelpers.GetColorFromData(mapped)
                    : (_compoundDataList != null && i < _compoundDataList.Length && _compoundDataList[i] != null
                        ? SOHelpers.GetColorFromData(_compoundDataList[i])
                        : Color.white);
            }
        }
    }

    // Silicate dropdown -> updates selected variant, button sprite & drag handler
    private void OnSilicateDropdownChanged(int optionIndex)
    {
        if (_compoundDataList == null || _spawnButtons == null || _spawnButtons.Length == 0) return;

        var silicates = System.Array.FindAll(_compoundDataList, c => c != null && c.name.IndexOf("Silicate", System.StringComparison.OrdinalIgnoreCase) >= 0);
        if (silicates == null || silicates.Length == 0) return;

        var map = new Dictionary<int, CompoundData>();
        for (int i = 0; i < silicates.Length; i++)
        {
            var sd = silicates[i];
            var v = GetVariantSuffixIndex(sd.name);
            if (v.HasValue) map[v.Value] = sd;
            else if (!map.ContainsKey(0)) map[0] = sd;
        }

        CompoundData selected = null;
        if (!map.TryGetValue(optionIndex, out selected))
        {
            int idx = Mathf.Clamp(optionIndex, 0, silicates.Length - 1);
            selected = silicates[idx];
        }
        if (selected == null) { Debug.LogWarning($"CompoundSpawner: Silicate variant for dropdown index {optionIndex} not found."); return; }
        _currentSilicateVariant = selected;

        int btnIndex = _silicateButtonIndex;
        if (btnIndex < 0 || btnIndex >= _spawnButtons.Length)
        {
            btnIndex = System.Array.FindIndex(_spawnButtons, b => b != null && b.name.IndexOf("Silicate", System.StringComparison.OrdinalIgnoreCase) >= 0);
            _silicateButtonIndex = btnIndex;
            if (btnIndex < 0) return;
        }

        Button silicateButton = _spawnButtons[btnIndex];
        if (silicateButton == null) return;

        Sprite optionSprite = (_silicateOptionSprites != null && optionIndex >= 0 && optionIndex < _silicateOptionSprites.Length) ? _silicateOptionSprites[optionIndex] : null;
        Image buttonImage = silicateButton.image ?? silicateButton.targetGraphic as Image;
        if (optionSprite != null && buttonImage != null)
        {
            buttonImage.sprite = optionSprite;
            if (_spawnButtonSprites != null && btnIndex >= 0 && btnIndex < _spawnButtonSprites.Length) _spawnButtonSprites[btnIndex] = optionSprite;
        }
        else if (optionSprite == null)
        {
            Debug.LogWarning($"CompoundSpawner: Dropdown option {optionIndex} has no image assigned.");
        }

        // set the button color and update the per-button mapping
        if (buttonImage != null)
        {
            buttonImage.color = SOHelpers.GetColorFromData(selected);
        }
        if (btnIndex >= 0 && btnIndex < _buttonDataMap.Length)
        {
            _buttonDataMap[btnIndex] = selected;
        }

        silicateButton.onClick.RemoveAllListeners();
        CompoundData dataCopy = selected;
        silicateButton.onClick.AddListener(() => OnSpawnButtonClicked(dataCopy));

        SpawnDragHandler drag = silicateButton.gameObject.GetComponent<SpawnDragHandler>();
        if (drag != null) drag.Init(this, selected);

        Debug.Log($"CompoundSpawner: Silicate variant set to '{selected.name}' for dropdown index {optionIndex}.");
    }

    private string GetCompoundBaseName(string soName)
    {
        if (string.IsNullOrEmpty(soName)) return soName;
        int first = soName.IndexOf('_');
        string basePart = first >= 0 ? soName.Substring(first + 1) : soName;
        var m = Regex.Match(basePart, @"^(.*?)(?:_[0-9]+)?$");
        return m.Success && m.Groups.Count > 1 ? m.Groups[1].Value : basePart;
    }

    private int? GetVariantSuffixIndex(string soName)
    {
        if (string.IsNullOrEmpty(soName)) return null;
        var m = Regex.Match(soName, @"_([0-9]+)$");
        if (m.Success && int.TryParse(m.Groups[1].Value, out int v)) return v;
        return null;
    }

    private void OnDrawGizmos()
    {
        if (_spawnArea == null) return;
        var rt = _spawnArea.rectTransform;
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Gizmos.color = Color.green;
        for (int i = 0; i < 4; i++) Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);

        float halfW = Mathf.Min(_spawnAreaSize.x * 0.5f, rt.rect.width * 0.5f);
        float halfH = Mathf.Min(_spawnAreaSize.y * 0.5f, rt.rect.height * 0.5f);
        Vector3 centerLocal = new Vector3(_spawnAreaCenter.x, _spawnAreaCenter.y, 0);
        Vector3 min = rt.TransformPoint(centerLocal + new Vector3(-halfW, -halfH, 0));
        Vector3 max = rt.TransformPoint(centerLocal + new Vector3(halfW, halfH, 0));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(min.x, min.y, 0), new Vector3(max.x, min.y, 0));
        Gizmos.DrawLine(new Vector3(max.x, min.y, 0), new Vector3(max.x, max.y, 0));
        Gizmos.DrawLine(new Vector3(max.x, max.y, 0), new Vector3(min.x, max.y, 0));
        Gizmos.DrawLine(new Vector3(min.x, max.y, 0), new Vector3(min.x, min.y, 0));
    }
}