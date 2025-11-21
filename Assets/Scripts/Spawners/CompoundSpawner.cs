using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;
using System.Collections.Generic; // for Dictionary
using TMPro; // support TMP_Dropdown if used in scene

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

    [SerializeField] private GameObject _silicateDropDown;

    // Track whether the player is currently dragging a spawned compound (to suppress click-spawns)
    private bool _isDragging = false;
    public bool IsDragging => _isDragging;
    public void SetDragging(bool value) => _isDragging = value;

    // silicate dropdown
    private Dropdown _silicateDropdownComp;
    private TMP_Dropdown _silicateTmpDropdown; // TMP support
    private Sprite[] _silicateOptionSprites;
    private int _silicateButtonIndex = -1;
    // currently selected silicate variant (set by dropdown)
    private CompoundData _currentSilicateVariant;

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
        if (_spawnButtons == null) _spawnButtons = new Button[0];
        _spawnButtonSprites = new Sprite[_spawnButtons.Length];
        for (int i = 0; i < _spawnButtons.Length; i++)
        {
            var img = _spawnButtons[i].targetGraphic?.GetComponent<Image>();
            _spawnButtonSprites[i] = img != null ? img.sprite : null;
            if (_spawnButtonSprites[i] == null)
            {
                Debug.LogError($"CompoundSpawner: Button at index {i} does not have a sprite.");
            }
            if (_spawnButtons[i] != null)
            {
                _spawnButtons[i].interactable = false;
                if (img != null)
                    img.sprite = _hiddenButtonSprite;
            }
        }

        // Load compound data from Resources (if not assigned in inspector)
        if (_compoundDataList == null || _compoundDataList.Length == 0)
        {
            CompoundData[] loadedCompounds = Resources.LoadAll<CompoundData>("SOs/Compounds");
            if (loadedCompounds != null && loadedCompounds.Length > 0)
                _compoundDataList = loadedCompounds;
        }

        // default current silicate variant to the first Silicate found (so SpawnSilicate() works even without a dropdown)
        if (_compoundDataList != null && _compoundDataList.Length > 0)
        {
            var firstSil = System.Array.Find(_compoundDataList, c => c != null && c.name.IndexOf("Silicate", System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (firstSil != null) _currentSilicateVariant = firstSil;
        }

        // Set up button listeners so each button spawns the right compound
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

        // Find silicate button index (used when dropdown changes)
        _silicateButtonIndex = System.Array.FindIndex(_spawnButtons, b => b != null && b.name.IndexOf("Silicate", System.StringComparison.OrdinalIgnoreCase) >= 0);

        // Hook up silicate dropdown (if provided). Support both Unity UI Dropdown and TMP_Dropdown.
        if (_silicateDropDown != null)
        {
            // try standard Dropdown first
            _silicateDropdownComp = _silicateDropDown.GetComponent<Dropdown>();
            if (_silicateDropdownComp != null && _silicateDropdownComp.options != null && _silicateDropdownComp.options.Count > 0)
            {
                _silicateOptionSprites = new Sprite[_silicateDropdownComp.options.Count];
                for (int i = 0; i < _silicateDropdownComp.options.Count; i++)
                    _silicateOptionSprites[i] = _silicateDropdownComp.options[i].image;

                _silicateDropdownComp.onValueChanged.AddListener(OnSilicateDropdownChanged);
                OnSilicateDropdownChanged(_silicateDropdownComp.value);
            }
            else
            {
                // try TMP_Dropdown
                _silicateTmpDropdown = _silicateDropDown.GetComponent<TMP_Dropdown>();
                if (_silicateTmpDropdown != null && _silicateTmpDropdown.options != null && _silicateTmpDropdown.options.Count > 0)
                {
                    _silicateOptionSprites = new Sprite[_silicateTmpDropdown.options.Count];
                    for (int i = 0; i < _silicateTmpDropdown.options.Count; i++)
                        _silicateOptionSprites[i] = _silicateTmpDropdown.options[i].image;

                    _silicateTmpDropdown.onValueChanged.AddListener(OnSilicateDropdownChanged);
                    OnSilicateDropdownChanged(_silicateTmpDropdown.value);
                }
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
        // If there's a DraggableHolder and it's full, show the full popup and do not create anything.
        if (DraggableHolder.Instance != null && DraggableHolder.Instance.IsFull)
        {
            DraggableHolder.Instance.FullPopup();
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
    public void SpawnSulfate()
    {
        if (_compoundDataList.Length > 3 && _compoundDataList[3] != null)
            SpawnCompoundAtRandomPosition(_compoundDataList[3]);
    }

    public void SpawnSilicate()
    {
        // spawn the currently-selected silicate variant if available
        if (_currentSilicateVariant != null)
        {
            SpawnCompoundAtRandomPosition(_currentSilicateVariant);
            return;
        }

        // fallback: try the previous fixed index logic (first silicate in list)
        if (_compoundDataList != null)
        {
            var firstSil = System.Array.Find(_compoundDataList, c => c != null && c.name.IndexOf("Silicate", System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (firstSil != null)
            {
                SpawnCompoundAtRandomPosition(firstSil);
                return;
            }
        }
    }

    public void SpawnSilicate0()
    {
        if (_compoundDataList.Length > 4 && _compoundDataList[4] != null)
            SpawnCompoundAtRandomPosition(_compoundDataList[4]);
    }
    public void SpawnSilicate1()
    {
        if (_compoundDataList.Length > 5 && _compoundDataList[5] != null)
            SpawnCompoundAtRandomPosition(_compoundDataList[5]);
    }
    public void SpawnSilicate2()
    {
        if (_compoundDataList.Length > 6 && _compoundDataList[6] != null)
            SpawnCompoundAtRandomPosition(_compoundDataList[6]);
    }
    public void SpawnSilicate3()
    {
        if (_compoundDataList.Length > 7 && _compoundDataList[7] != null)
            SpawnCompoundAtRandomPosition(_compoundDataList[7]);
    }
    public void SpawnSilicate4()
    {
        if (_compoundDataList.Length > 8 && _compoundDataList[8] != null)
            SpawnCompoundAtRandomPosition(_compoundDataList[8]);
    }

    // Unlock all buttons at once (call from UI)
    public void UnlockAllButtons()
    {
        for (int i = 0; i < _spawnButtons.Length; i++)
        {
            if (_spawnButtons[i] == null) continue;
            _spawnButtons[i].interactable = true;
            var img = _spawnButtons[i].targetGraphic?.GetComponent<Image>();
            if (img != null && _spawnButtonSprites != null && i < _spawnButtonSprites.Length && _spawnButtonSprites[i] != null)
                img.sprite = _spawnButtonSprites[i];
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

    // Extracts the base name of a compound from its SO name (e.g., "C_Carbonate" -> "Carbonate", "C_Silicate_0" -> "Silicate")
    private string GetCompoundBaseName(string soName)
    {
        if (string.IsNullOrEmpty(soName))
            return soName;

        // Remove leading prefix up to first underscore (C_ etc.)
        int firstUnderscore = soName.IndexOf('_');
        string basePart = firstUnderscore >= 0 ? soName.Substring(firstUnderscore + 1) : soName;

        // Remove trailing numeric suffix like _0, _1 etc.
        var m = Regex.Match(basePart, @"^(.*?)(?:_[0-9]+)?$");
        if (m.Success && m.Groups.Count > 1)
            return m.Groups[1].Value;

        return basePart;
    }

    // Returns nullable variant index if the SO name ends with _N
    private int? GetVariantSuffixIndex(string soName)
    {
        if (string.IsNullOrEmpty(soName)) return null;
        var m = Regex.Match(soName, @"_([0-9]+)$");
        if (m.Success)
        {
            if (int.TryParse(m.Groups[1].Value, out int v)) return v;
        }
        return null;
    }

    // Called when the silicate dropdown selection changes (uses Dropdown.OptionData.image as sprite)
    private void OnSilicateDropdownChanged(int optionIndex)
    {
        if (_compoundDataList == null || _spawnButtons == null || _spawnButtons.Length == 0)
            return;

        // collect available silicate variants from the compound data list
        CompoundData[] silicates = System.Array.FindAll(_compoundDataList, c => c != null && c.name.IndexOf("Silicate", System.StringComparison.OrdinalIgnoreCase) >= 0);
        if (silicates == null || silicates.Length == 0)
            return;

        // Map variants by their numeric suffix when present (so dropdown index 0 => _0, etc.)
        var variantMap = new Dictionary<int, CompoundData>();
        for (int i = 0; i < silicates.Length; i++)
        {
            var sd = silicates[i];
            int? idx = GetVariantSuffixIndex(sd.name);
            if (idx.HasValue)
            {
                // prefer explicit mapping
                variantMap[idx.Value] = sd;
            }
            else
            {
                // no suffix: place into first available slot if nothing else maps to 0
                if (!variantMap.ContainsKey(0))
                    variantMap[0] = sd;
            }
        }

        // Choose the selected variant by optionIndex if present, otherwise fallback to same-order or first
        CompoundData selectedData = null;
        if (!variantMap.TryGetValue(optionIndex, out selectedData))
        {
            // try to pick by index in the silicates array if possible
            int chosen = Mathf.Clamp(optionIndex, 0, silicates.Length - 1);
            selectedData = silicates[chosen];
        }

        // set current variant so SpawnSilicate() and other callers use the selection
        if (selectedData != null)
            _currentSilicateVariant = selectedData;

        if (selectedData == null)
        {
            Debug.LogWarning($"CompoundSpawner: Silicate variant for dropdown index {optionIndex} not found.");
            return;
        }

        // find the silicate button (either cached index or search by name)
        int btnIndex = _silicateButtonIndex;
        if (btnIndex < 0 || btnIndex >= _spawnButtons.Length)
        {
            btnIndex = System.Array.FindIndex(_spawnButtons, b => b != null && b.name.IndexOf("Silicate", System.StringComparison.OrdinalIgnoreCase) >= 0);
            _silicateButtonIndex = btnIndex;
            if (btnIndex < 0) return;
        }

        Button silicateButton = _spawnButtons[btnIndex];
        if (silicateButton == null) return;

        // update button sprite from dropdown option image if available
        Sprite optionSprite = (_silicateOptionSprites != null && optionIndex >= 0 && optionIndex < _silicateOptionSprites.Length)
            ? _silicateOptionSprites[optionIndex]
            : null;

        // Use Button.image when available (safer) and fall back to targetGraphic
        Image buttonImage = silicateButton.image ?? silicateButton.targetGraphic as Image;
        if (optionSprite != null && buttonImage != null)
        {
            buttonImage.sprite = optionSprite;
            if (_spawnButtonSprites != null && btnIndex >= 0 && btnIndex < _spawnButtonSprites.Length)
                _spawnButtonSprites[btnIndex] = optionSprite; // keep spawn-button's stored sprite in sync
        }
        else if (optionSprite == null)
        {
            Debug.LogWarning($"CompoundSpawner: Dropdown option {optionIndex} has no image assigned.");
        }

        // update click listeners to spawn the selected variant
        silicateButton.onClick.RemoveAllListeners();
        CompoundData dataCopy = selectedData;
        silicateButton.onClick.AddListener(() => OnSpawnButtonClicked(dataCopy));

        // update the SpawnDragHandler attached to the button so dragging produces the selected variant
        SpawnDragHandler dragHandler = silicateButton.gameObject.GetComponent<SpawnDragHandler>();
        if (dragHandler != null)
        {
            dragHandler.Init(this, selectedData);
        }

        // small debug trace (remove/comment out when stable)
        Debug.Log($"CompoundSpawner: Silicate variant set to '{selectedData.name}' for dropdown index {optionIndex}.");
    }
}