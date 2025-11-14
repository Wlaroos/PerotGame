using System.Collections.Generic;
using UnityEngine;

public class CraftingZone : MonoBehaviour
{
    private List<GameObject> _objectsInZone = new List<GameObject>();

    // multi-press crafting state
    private bool _craftingInProgress = false;
    private int _requiredPresses = 0;
    private int _currentPresses = 0;
    private List<GameObject> _objectsSnapshot = null;
    private List<ScriptableObject> _snapshotIngredients = null;

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
        if (ingredients.Count == 0)
        {
            ResetCraftingState();
            return;
        }

        // If we are not already in a multi-press sequence, probe whether these ingredients form a valid recipe.
        if (!_craftingInProgress)
        {
            // Probe craft by asking the manager for a matching recipe (no side effects)
            var probeRecipe = CraftingManager.Instance != null ? CraftingManager.Instance.FindMatchingRecipe(ingredients) : null;
            if (probeRecipe != null)
            {
                // Valid recipe found -> start multi-press sequence
                _craftingInProgress = true;
                _currentPresses = 1;
                _requiredPresses = 5;
                _objectsSnapshot = new List<GameObject>(_objectsInZone);
                _snapshotIngredients = new List<ScriptableObject>(ingredients);

                MoveObjectsCloser(_objectsSnapshot);
                if (_currentPresses >= _requiredPresses)
                {
                    FinalizeCraft(_snapshotIngredients, _objectsSnapshot);
                }
            }
            else
            {
                Debug.Log("Crafting failed: No matching recipe.");
                ResetCraftingState();
            }
        }
        else
        {
            // Already in a multi-press sequence
            _currentPresses++;
            // Use the snapshot so players cannot change ingredients mid-way
            if (_objectsSnapshot == null || _objectsSnapshot.Count == 0)
            {
                // fallback to current zone if snapshot was lost
                _objectsSnapshot = new List<GameObject>(_objectsInZone);
            }

            MoveObjectsCloser(_objectsSnapshot);

            if (_currentPresses >= _requiredPresses)
            {
                FinalizeCraft(_snapshotIngredients ?? ingredients, _objectsSnapshot);
            }
            else
            {
                //Debug.Log($"Crafting in progress: press {_requiredPresses - _currentPresses} more time(s).");
            }
        }
    }

    private void MoveObjectsCloser(List<GameObject> objects)
    {
        if (objects == null || objects.Count == 0) return;

        // compute center of existing objects
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (var obj in objects)
        {
            if (obj != null)
            {
                center += obj.transform.position;
                count++;
            }
        }
        if (count == 0) return;
        center /= count;

        // move each object a fraction toward the center; fraction chosen so they converge after required presses
        float t = _requiredPresses > 0 ? 1f / _requiredPresses : 0.25f;
        foreach (var obj in objects)
        {
            if (obj == null) continue;
            obj.transform.position = Vector3.Lerp(obj.transform.position, center, t);
        }
    }

    private void FinalizeCraft(List<ScriptableObject> ingredients, List<GameObject> objectsToConsume)
    {
        Vector3 spawnPosition = ComputeCenterOf(objectsToConsume) ?? transform.position;
        // Unparent the objects first so they no longer count toward the DraggableHolder child count.
        // Preserve original parents so we can restore them if crafting fails.
        List<Transform> originalParents = new List<Transform>();
        foreach (var obj in objectsToConsume)
        {
            originalParents.Add(obj != null ? obj.transform.parent : null);
            if (obj != null)
            {
                obj.transform.SetParent(null);
            }
        }

        // Now attempt to craft the result. DraggableHolder will receive the instantiated object if available.
        GameObject craftedObj = CraftingManager.Instance != null ? CraftingManager.Instance.TryCraft(ingredients, spawnPosition) : null;

        if (craftedObj != null)
        {
            // Successful craft -> remove the consumed objects
            foreach (var obj in objectsToConsume)
            {
                if (obj != null)
                {
                    Destroy(obj);
                    _objectsInZone.Remove(obj);
                }
            }

            ResetCraftingState();
        }
        else
        {
            // Craft failed -> restore original parenting so objects remain in the same logical place
            for (int i = 0; i < objectsToConsume.Count; i++)
            {
                var obj = objectsToConsume[i];
                var parent = originalParents[i];
                if (obj != null)
                {
                    obj.transform.SetParent(parent);
                }
            }

            Debug.Log("Crafting failed at finalization: No matching recipe.");
            ResetCraftingState();
        }
    }

    private Vector3? ComputeCenterOf(List<GameObject> objects)
    {
        if (objects == null || objects.Count == 0) return null;
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (var obj in objects)
        {
            if (obj != null)
            {
                center += obj.transform.position;
                count++;
            }
        }
        if (count == 0) return null;
        return center / count;
    }

    private void ResetCraftingState()
    {
        _craftingInProgress = false;
        _requiredPresses = 0;
        _currentPresses = 0;
        _objectsSnapshot = null;
        _snapshotIngredients = null;
    }
}