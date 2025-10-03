using System.Collections.Generic;
using UnityEngine;

public class CraftingZone : MonoBehaviour
{
    private List<GameObject> _objectsInZone = new List<GameObject>();

    // Called when an object enters the zone
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_objectsInZone.Contains(collision.gameObject))
        {
            _objectsInZone.Add(collision.gameObject);
        }
    }

    // Called when an object exits the zone
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_objectsInZone.Contains(collision.gameObject))
        {
            _objectsInZone.Remove(collision.gameObject);
        }
    }

    // Get the data of all objects in the zone
    public List<ScriptableObject> GetIngredients()
    {
        List<ScriptableObject> ingredients = new List<ScriptableObject>();
        foreach (var obj in _objectsInZone)
        {
            var element = obj.GetComponent<Element>();
            if (element != null) ingredients.Add(element.data);

            var compound = obj.GetComponent<Compound>();
            if (compound != null) ingredients.Add(compound.data);

            var mineral = obj.GetComponent<Mineral>();
            if (mineral != null) ingredients.Add(mineral.data);
        }
        return ingredients;
    }

    // Trigger crafting when the button is pressed
    public void Craft()
    {
        List<ScriptableObject> ingredients = GetIngredients();
        if (ingredients.Count > 0)
        {
            Vector3 spawnPosition = transform.position;
            GameObject craftedObj = CraftingManager.Instance.TryCraft(ingredients, spawnPosition);

            if (craftedObj != null)
            {
                // Collect objects to destroy in a separate list
                List<GameObject> objectsToDestroy = new List<GameObject>(_objectsInZone);

                // Destroy the used objects
                foreach (var obj in objectsToDestroy)
                {
                    Destroy(obj);
                }

                // Clear the original list after destruction
                _objectsInZone.Clear();
            }
            else
            {
                Debug.Log("Crafting failed: No matching recipe.");
            }
        }
    }
}