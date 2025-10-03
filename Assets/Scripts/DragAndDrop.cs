using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// Lets you drag, drop, and combine objects in the game
public class DragAndDrop : MonoBehaviour
{
    [SerializeField] private float _dragSpeed = 25f; // How fast the object follows the mouse
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
        Vector2 targetPosition = GetMouseWorldPosition() + _mouseOffset;
        transform.position = Vector2.Lerp(transform.position, targetPosition, _dragSpeed * Time.deltaTime);
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

    // When you let go of the mouse
    private void OnMouseUp()
    {
        if (_collider != null)
        {
            GameObject otherObj = _collider.gameObject;

            // Get this object's data
            ScriptableObject dataA = (ScriptableObject)_element?.data ?? (ScriptableObject)_compound?.data ?? (ScriptableObject)_mineral?.data;
            // Get the other object's data
            ScriptableObject dataB = GetDataFromGameObject(otherObj);

            List<ScriptableObject> ingredients = new List<ScriptableObject> { dataA, dataB };

            // Try to craft a new object from the two
            Vector3 spawnPosition = (transform.position + otherObj.transform.position) / 2f;

            GameObject craftedObj = CraftingManager.Instance.TryCraft(ingredients, spawnPosition);

            if (craftedObj != null)
            {
                // Remove the old objects
                Destroy(otherObj);
                Destroy(gameObject);
            }
            else
            {
                if (otherObj.TryGetComponent<DragAndDrop>(out _))
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

                    // Play fail effect at the midpoint
                    Vector3 failPosition = (transform.position + otherObj.transform.position) / 2f;
                    EffectManager.Instance.PlayFailEffect(failPosition);
                }
            }
        }
        // Put object back to normal draw order
        if (_sg != null)
            _sg.sortingLayerName = "Default";
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
}
