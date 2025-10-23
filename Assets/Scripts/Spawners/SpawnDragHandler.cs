using UnityEngine;
using UnityEngine.EventSystems;

// Attach to spawn buttons to allow drag-to-spawn and drop at mouse position
public class SpawnDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ElementSpawner _elementSpawner;
    private CompoundSpawner _compoundSpawner;
    private ElementData _elementData;
    private CompoundData _compoundData;
    private GameObject _draggedInstance;

    // Call from ElementSpawner when wiring up the button
    public void Init(ElementSpawner spawner, ElementData data)
    {
        _elementSpawner = spawner;
        _elementData = data;
        _compoundSpawner = null;
        _compoundData = null;
    }

    // Call from CompoundSpawner when wiring up the button
    public void Init(CompoundSpawner spawner, CompoundData data)
    {
        _compoundSpawner = spawner;
        _compoundData = data;
        _elementSpawner = null;
        _elementData = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if ((_elementSpawner == null || _elementData == null) && (_compoundSpawner == null || _compoundData == null))
            return;

        Vector3 worldPos = ScreenToWorld(eventData.position);

        if (_elementSpawner != null && _elementData != null)
        {
            _draggedInstance = _elementSpawner.SpawnElementAtPosition(_elementData, -1, worldPos);
            _elementSpawner.SetDragging(true);
        }
        else if (_compoundSpawner != null && _compoundData != null)
        {
            _draggedInstance = _compoundSpawner.SpawnCompoundAtPosition(_compoundData, worldPos);
            _compoundSpawner.SetDragging(true);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_draggedInstance == null) return;

        Vector3 worldPos = ScreenToWorld(eventData.position);
        _draggedInstance.transform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Leave the spawned object where the player dropped it.
        // If you want to cancel the spawn when dropped outside the spawn area, add checks here.

        _draggedInstance = null;
        if (_elementSpawner != null)
            _elementSpawner.SetDragging(false);
        if (_compoundSpawner != null)
            _compoundSpawner.SetDragging(false);
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        // distance from camera to world z=0 plane
        float z = -cam.transform.position.z;
        Vector3 screenPoint = new Vector3(screenPos.x, screenPos.y, z);
        return cam.ScreenToWorldPoint(screenPoint);
    }
}