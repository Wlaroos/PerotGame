using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Element))]
public class DragAndDrop : MonoBehaviour
{
    [SerializeField] private float _dragSpeed = 25f; // Speed of interpolation
    private Vector2 _mouseOffset;
    private Element _element;
    private Collider2D _collider;
    private SortingGroup _sg;

    private void Awake()
    {
        _element = GetComponent<Element>();
        if (_element == null)
        {
            Debug.LogError("DragAndDrop: Missing Element component.");
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
            Element otherElement = _collider.GetComponent<Element>();
            if (otherElement != null)
            {
                // Attempt to craft a new element
                bool crafted = CraftingManager.Instance.TryCraft(_element, otherElement);
                if (crafted)
                {
                    Destroy(gameObject);
                    Destroy(otherElement.gameObject);
                }
            }
        }

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
}
