using UnityEngine;
using UnityEngine.UI;

public class ChildButton : MonoBehaviour
{
    [SerializeField] private CraftingZone _craftingZone; // Reference to the CraftingZone script

    private void OnMouseDown()
    {
        // Change the sprite's color to indicate a click
        GetComponent<Image>().color = Color.gray;

        // Call the Craft method
        if (_craftingZone != null)
        {
            _craftingZone.Craft();
        }
    }

    private void OnMouseUp()
    {
        // Revert the sprite's color when the mouse button is released
        GetComponent<Image>().color = Color.white;
    }
}