using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;

// Lets you drag, drop, and combine objects in the game
public class DragAndDrop : MonoBehaviour
{
    [SerializeField] private float _dragSpeed = 25f; // How fast the object follows the mouse
    
    [Header("Main Area Panel (screen-space)")]
    [SerializeField] private float _clampX = 5.325f;
    [SerializeField] private float _clampY = 5f;

    private Vector2 _mouseOffset; // Distance from mouse to object when dragging starts
    private Element _element;     // Reference if this is an element
    private Compound _compound;   // Reference if this is a compound
    private Mineral _mineral;     // Reference if this is a mineral
    private Collider2D _collider; // The collider we're overlapping with
    private SortingGroup _sg;     // For controlling draw order

    private InputSystem_Actions _inputActions;

    // Runs when the object is created
    private void Awake()
    {
        // Get references to possible components
        _element = GetComponent<Element>();
        _compound = GetComponent<Compound>();
        _mineral = GetComponent<Mineral>();

        // Warn if none are found
        if (_element == null && _compound == null && _mineral == null)
        {
            Debug.LogError("DragAndDrop: Missing Element, Compound, or Mineral component");
        }

        // Get sorting group for draw order
        _sg = GetComponent<SortingGroup>();
    }

    private void OnEnable()
    {
        _inputActions = new InputSystem_Actions();
        _inputActions.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Disable();
    }

    // Gets the mouse position in world space
    private Vector2 GetMouseWorldPosition()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    // When you click on the object
    private void OnMouseDown()
    {
        // Remember how far the mouse is from the object's center
        _mouseOffset = (Vector2)transform.position - GetMouseWorldPosition();
        // Bring object to front while dragging
        if (_sg != null)
            _sg.sortingLayerName = "Dragging";
    }

    // While dragging the object
    private void OnMouseDrag()
    {
        // Move object smoothly to follow the mouse
        Vector2 mouseWorld = GetMouseWorldPosition() + _mouseOffset;

        Vector3 current = transform.position;
        Vector3 desired = new Vector3(mouseWorld.x, mouseWorld.y, current.z);
        Vector3 interpolated = Vector3.Lerp(current, desired, _dragSpeed * Time.deltaTime);

        transform.position = ClampToMainArea(interpolated);
    }

    // External drag state (used when the object is spawned by UI drag handlers)
    private bool _externalDragging = false;

    // Start dragging from an external controller (SpawnDragHandler)
    public void StartExternalDrag(Vector3 worldPos)
    {
        _mouseOffset = (Vector2)transform.position - (Vector2)worldPos;
        if (_sg != null)
            _sg.sortingLayerName = "Dragging";
        _externalDragging = true;
    }

    // Update position while externally dragged
    public void UpdateExternalDrag(Vector3 worldPos)
    {
        if (!_externalDragging) return;
        transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
    }

    // End external drag and perform release logic
    public void EndExternalDrag()
    {
        if (!_externalDragging) return;

        _externalDragging = false;

        transform.position = ClampToMainArea(transform.position);

        Release();

        if (_sg != null)
            _sg.sortingLayerName = "Default";
    }

    // Gets the data (element, compound, or mineral) from another object
    private ScriptableObject GetDataFromGameObject(GameObject obj)
    {
        var element = obj.GetComponent<Element>();
        if (element != null) return element.data;
        var compound = obj.GetComponent<Compound>();
        if (compound != null) return compound.data;
        var mineral = obj.GetComponent<Mineral>();
        if (mineral != null) return mineral.data;
        return null;
    }

    private void OnMouseUp()
    {
        Release();

        // Put object back to normal draw order
        if (_sg != null)
            _sg.sortingLayerName = "Default";
    }

    // Shared release logic used both by mouse and external drags
    private void Release()
    {
        if (_collider != null)
        {
            GameObject otherObj = _collider.gameObject;

            // Ensure both stay inside the main area panel
            transform.position = ClampToMainArea(transform.position);
            otherObj.transform.position = ClampToMainArea(otherObj.transform.position);

            // Get this object's data
            ScriptableObject dataA = (ScriptableObject)_element?.data ?? (ScriptableObject)_compound?.data ?? (ScriptableObject)_mineral?.data;
            // Get the other object's data
            ScriptableObject dataB = GetDataFromGameObject(otherObj);

            if (dataB == null)
            {
                if (otherObj.name == "TrashButtonBG")
                {
                    Destroy(gameObject);
                }

                return;
            }

            List<ScriptableObject> ingredients = new List<ScriptableObject> { dataA, dataB };

            // Compute spawn position first
            Vector3 spawnPosition = (transform.position + otherObj.transform.position) / 2f;

            var manager = CraftingManager.Instance;

            // Preserve original parents so we can restore them if crafting fails
            Transform originalParentA = transform.parent;
            Transform originalParentB = otherObj != null ? otherObj.transform.parent : null;

            // Unparent both objects so they no longer count toward the DraggableHolder
            transform.SetParent(null);
            if (otherObj != null) otherObj.transform.SetParent(null);

            // Attempt to craft
            GameObject craftedObj = manager != null ? manager.TryCraft(ingredients, spawnPosition) : null;

            if (craftedObj != null)
            {
                // Successful craft -- remove the consumed objects
                if (otherObj != null) Destroy(otherObj);
                Destroy(gameObject);
            }
            else
            {
                // Craft failed -- restore original parents and run original failure behavior
                transform.SetParent(originalParentA);
                if (otherObj != null) otherObj.transform.SetParent(originalParentB);

                if (otherObj != null && otherObj.TryGetComponent<DragAndDrop>(out _))
                {
                    // If crafting failed, push the objects apart
                    Vector3 separationDirection = (otherObj.transform.position - transform.position);
                    if (separationDirection == Vector3.zero)
                    {
                        // If they're on top of each other, pick a random direction
                        separationDirection = Random.insideUnitCircle.normalized;
                    }
                    else
                    {
                        separationDirection = separationDirection.normalized;
                    }
                    float separationDistance = 0.33f;
                    // Make sure they don't overlap, based on their size
                    if (TryGetComponent(out Collider colA) && otherObj.TryGetComponent(out Collider colB))
                    {
                        separationDistance += (colA.bounds.size.magnitude + colB.bounds.size.magnitude) / 2;
                    }
                    // Move both objects away from each other
                    transform.position -= separationDirection * separationDistance;
                    otherObj.transform.position += separationDirection * separationDistance;

                    // Ensure both stay inside the main area panel
                    transform.position = ClampToMainArea(transform.position);
                    otherObj.transform.position = ClampToMainArea(otherObj.transform.position);

                    // Play fail effect at the midpoint
                    Vector3 failPosition = (transform.position + otherObj.transform.position) / 2f;
                    EffectManager.Instance.PlayFailEffect(failPosition);
                }
            }
        }
    }

    // When this object touches another collider
    private void OnTriggerEnter2D(Collider2D collision)
    {
        _collider = collision;
    }

    // When this object stops touching a collider
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_collider == collision)
        {
            _collider = null;
        }
    }

    // Checks for right-click to delete this object
    private void Update()
    {
        // Checking for right mouse button using the new input system
        if (_inputActions.UI.RightClick.WasPressedThisFrame())
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(mousePos);
            if (hit != null && hit.gameObject == gameObject)
            {
                Destroy(gameObject);
            }
        }
    }

    private Vector3 ClampToMainArea(Vector3 worldPosition)
    {
        Camera cam = Camera.main;

        // Get the object's size
        Vector3 objectSize = Vector3.zero;
        if (TryGetComponent(out Collider2D collider))
        {
            objectSize = collider.bounds.size;
        }

        // Calculate the clamped boundaries, considering the object's size
        float halfWidth = objectSize.x / 2f;
        float halfHeight = objectSize.y / 2f;

        Vector3 bottomLeft = new Vector3(-_clampX + halfWidth, -_clampY + halfHeight, 0);
        Vector3 topRight = new Vector3(_clampX - halfWidth, _clampY - halfHeight, 0);

        Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);
        screenPos.x = Mathf.Clamp(screenPos.x, cam.WorldToScreenPoint(bottomLeft).x, cam.WorldToScreenPoint(topRight).x);
        screenPos.y = Mathf.Clamp(screenPos.y, cam.WorldToScreenPoint(bottomLeft).y, cam.WorldToScreenPoint(topRight).y);

        return cam.ScreenToWorldPoint(screenPos);
    }
}
