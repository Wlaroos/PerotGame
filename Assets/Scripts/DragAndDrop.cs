using UnityEngine;

[RequireComponent(typeof(Element))]
public class DragAndDrop : MonoBehaviour
{
    private Vector2 mouseOffset;
    [SerializeField] private float dragSpeed = 10f; // Speed of interpolation

    private Element element;
    private Collider2D currentCollision;

    private void Awake()
    {
        element = GetComponent<Element>();
        if (element == null)
        {
            Debug.LogError("DragAndDrop: Missing Element component.");
        }
    }

    private Vector2 GetMouseWorldPosition()
    {
        // Convert screen position to world position in 2D
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseDown()
    {
        // Calculate the offset between the object's position and the mouse position
        mouseOffset = (Vector2)transform.position - GetMouseWorldPosition();
    }

    private void OnMouseDrag()
    {
        // Calculate the target position based on the mouse position and offset
        Vector2 targetPosition = GetMouseWorldPosition() + mouseOffset;

        // Smoothly interpolate the object's position towards the target position
        transform.position = Vector2.Lerp(transform.position, targetPosition, dragSpeed * Time.deltaTime);
    }

    private void OnMouseUp()
    {
        if (currentCollision != null)
        {
            Element otherElement = currentCollision.GetComponent<Element>();
            if (otherElement != null)
            {
                // Attempt to craft a new element
                bool crafted = CraftingManager.Instance.TryCraft(element, otherElement);
                if (crafted)
                {
                    Destroy(gameObject);
                    Destroy(otherElement.gameObject);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        currentCollision = collision;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (currentCollision == collision)
        {
            currentCollision = null;
        }
    }
}
