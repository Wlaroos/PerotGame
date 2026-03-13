using UnityEngine;
using UnityEngine.EventSystems;

public class SpawnDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ElementSpawner _elementSpawner;
    private CompoundSpawner _compoundSpawner;
    private ElementData _elementData;
    private CompoundData _compoundData;
    private GameObject _draggedInstance;

    [Header("Main Area Panel (screen-space)")]
    // Updated to match DragAndDrop values
    [SerializeField] private float _clampX = 5.325f;
    [SerializeField] private float _clampY = 5f;

    public void Init(ElementSpawner spawner, ElementData data)
    {
        _elementSpawner = spawner;
        _elementData = data;
        _compoundSpawner = null;
        _compoundData = null;
    }

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

        if (_draggedInstance != null && _draggedInstance.TryGetComponent<DragAndDrop>(out var dd))
        {
            dd.StartExternalDrag(worldPos);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_draggedInstance == null) return;

        Vector3 worldPos = ScreenToWorld(eventData.position);
        if (_draggedInstance.TryGetComponent<DragAndDrop>(out var dd))
        {
            dd.UpdateExternalDrag(worldPos);
        }
        else
        {
            _draggedInstance.transform.position = worldPos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_draggedInstance == null) return;

        if (_draggedInstance.TryGetComponent<DragAndDrop>(out var dd))
        {
            dd.EndExternalDrag();
        }
        else
        {
            _draggedInstance.transform.position = ClampToMainArea(_draggedInstance.transform.position);
        }

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

        float z = -cam.transform.position.z;
        Vector3 screenPoint = new Vector3(screenPos.x, screenPos.y, z);
        return cam.ScreenToWorldPoint(screenPoint);
    }

    private Vector3 ClampToMainArea(Vector3 worldPosition)
    {
        Camera cam = Camera.main;

        // Get the object's size
        Vector3 objectSize = Vector3.zero;
        if (_draggedInstance != null && _draggedInstance.TryGetComponent(out Collider2D collider))
        {
            objectSize = collider.bounds.size;
        }

        // Calculate the clamped boundaries, considering the object's size
        float halfWidth = objectSize.x / 2f;
        float halfHeight = objectSize.y / 2f;

        Vector3 bottomLeft = new Vector3(-_clampX + halfWidth, -_clampY + halfHeight, 0);
        Vector3 topRight = new Vector3(_clampX - halfWidth, _clampY - halfHeight, 0);

        Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);
        screenPos.x = Mathf.Clamp(screenPos.x, cam.WorldToScreenPoint(bottomLeft).x, cam.WorldToScreenPoint(topRight).x);
        screenPos.y = Mathf.Clamp(screenPos.y, cam.WorldToScreenPoint(bottomLeft).y, cam.WorldToScreenPoint(topRight).y);

        return cam.ScreenToWorldPoint(screenPos);
    }
}