using System.Collections.Generic;
using UnityEngine;

public class CraftingZone : MonoBehaviour
{
    private List<GameObject> _objectsInZone = new List<GameObject>();

    // multi-press crafting state
    private bool _craftingInProgress = false;
    [SerializeField] private int _requiredPresses = 0;
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
            // Probe craft by calling TryCraft at an offscreen position and immediately destroying the result.
            Vector3 probePos = transform.position + Vector3.up * 1000f;
            GameObject probe = CraftingManager.Instance.TryCraft(ingredients, probePos);
            if (probe != null)
            {
                // Valid recipe found -> start multi-press sequence
                Destroy(probe);

                _craftingInProgress = true;
                _currentPresses = 1;
                _objectsSnapshot = new List<GameObject>(_objectsInZone);
                _snapshotIngredients = new List<ScriptableObject>(ingredients);

                MoveObjectsCloser(_objectsSnapshot);
                if (_currentPresses >= _requiredPresses)
                {
                    FinalizeCraft(_snapshotIngredients, _objectsSnapshot);
                }
                else
                {
                    Debug.Log($"Crafting started: press {_requiredPresses - _currentPresses} more time(s).");
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
                Debug.Log($"Crafting in progress: press {_requiredPresses - _currentPresses} more time(s).");
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
        GameObject craftedObj = CraftingManager.Instance.TryCraft(ingredients, spawnPosition);

        if (craftedObj != null)
        {
            // Destroy the used objects (use snapshot to avoid modifying original while iterating)
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