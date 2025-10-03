using UnityEngine;

public class ChildButton : MonoBehaviour
{
    [SerializeField] private CraftingZone _craftingZone; // Reference to the CraftingZone script

    private void OnMouseDown()
    {
        // Change the sprite's color to indicate a click
        GetComponent<SpriteRenderer>().color = Color.gray;

        // Call the Craft method
        if (_craftingZone != null)
        {
            _craftingZone.Craft();
        }
    }

    private void OnMouseUp()
    {
        // Revert the sprite's color when the mouse button is released
        GetComponent<SpriteRenderer>().color = Color.white;
    }
}