using UnityEngine;

[RequireComponent(typeof(Element))]
public class DragAndDrop : MonoBehaviour
{
    private Vector2 mouseOffset;
    [SerializeField] private float dragSpeed = 10f; // Speed of interpolation

    private Element element;

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the other object has an Element component
        Element otherElement = collision.gameObject.GetComponent<Element>();
        if (otherElement != null)
        {
            TryCraftElement(otherElement);
        }
    }

    private void TryCraftElement(Element otherElement)
    {
        // Ensure both elements are of the same type
        if (element.Type == otherElement.Type)
        {
            switch (element.Type)
            {
                case Element.ElementType.Hydrogen:
                    if (element.IsotopeNumber == 1 && otherElement.IsotopeNumber == 1)
                    {
                        CraftNewElement(Element.ElementType.Hydrogen, 2);
                    }
                    else if (element.IsotopeNumber == 2 && otherElement.IsotopeNumber == 2)
                    {
                        CraftNewElement(Element.ElementType.Helium, 3);
                    }
                    break;

                case Element.ElementType.Helium:
                    if (element.IsotopeNumber == 3 && otherElement.IsotopeNumber == 3)
                    {
                        CraftNewElement(Element.ElementType.Helium, 4);
                    }
                    else if (element.IsotopeNumber == 4 && otherElement.IsotopeNumber == 4)
                    {
                        CraftNewElement(Element.ElementType.Beryllium, 8);
                    }
                    break;

                case Element.ElementType.Beryllium:
                    if (element.IsotopeNumber == 8 && otherElement.Type == Element.ElementType.Helium && otherElement.IsotopeNumber == 4)
                    {
                        CraftNewElement(Element.ElementType.Carbon, 12);
                    }
                    break;
            }
        }
    }

    private void CraftNewElement(Element.ElementType newType, int newIsotopeNumber)
    {
        // Update the current element to the new type and isotope number
        element.SetElement(newType, newIsotopeNumber);

        // Destroy the other element
        Destroy(gameObject);
    }
}
