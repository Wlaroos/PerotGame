using UnityEngine;
using UnityEngine.Rendering;

public class DragAndDrop : MonoBehaviour
{
    [SerializeField] private float _dragSpeed = 25f; // Speed of interpolation
    private Vector2 _mouseOffset;
    private Element _element;
    private Compound _compound;
    private Mineral _mineral;
    private Collider2D _collider;
    private SortingGroup _sg;

    private void Awake()
    {
        _element = GetComponent<Element>();
        _compound = GetComponent<Compound>();
        _mineral = GetComponent<Mineral>();

        if (_element == null && _compound == null && _mineral == null)
        {
            Debug.LogError("DragAndDrop: Missing Element, Compound, or Mineral component");
        }

        _sg = GetComponent<SortingGroup>();
    }

    private Vector2 GetMouseWorldPosition()
    {
        // Convert screen position to world position in 2D
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseDown()
    {
        // Calculate the offset between the object's position and the mouse position
        _mouseOffset = (Vector2)transform.position - GetMouseWorldPosition();
        if (_sg != null)
            _sg.sortingLayerName = "Dragging";
    }

    private void OnMouseDrag()
    {
        // Calculate the target position based on the mouse position and offset
        Vector2 targetPosition = GetMouseWorldPosition() + _mouseOffset;

        // Smoothly interpolate the object's position towards the target position
        transform.position = Vector2.Lerp(transform.position, targetPosition, _dragSpeed * Time.deltaTime);
    }

    private void OnMouseUp()
    {
        if (_collider != null)
        {
            GameObject otherObj = _collider.gameObject;
            bool crafted = false;

            // Try element crafting
            if (_element != null && otherObj.GetComponent<Element>() != null && CraftingManager.Instance != null)
            {
                crafted = CraftingManager.Instance.TryCraft(_element, otherObj.GetComponent<Element>());
            }
            // Try mineral crafting (generalized: any two objects)
            else if (MineralCraftingManager.Instance != null)
            {
                crafted = MineralCraftingManager.Instance.TryCraft(gameObject, otherObj);
            }

            if (crafted)
            {
                _collider = null; // Clear reference before destroying
                Destroy(otherObj);
                Destroy(gameObject); // Destroy this object last
            }
        }

        if (_sg != null)
            _sg.sortingLayerName = "Default";
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        _collider = collision;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_collider == collision)
        {
            _collider = null;
        }
    }

    private void Update()
    {
        // Delete this GameObject on right mouse button click while mouse is over it
        if (Input.GetMouseButtonDown(1)) // 1 = right mouse button
        {
            // Check if mouse is over this object using a raycast
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mousePos);
            if (hit != null && hit.gameObject == gameObject)
            {
                Destroy(gameObject);
            }
        }
    }
}
