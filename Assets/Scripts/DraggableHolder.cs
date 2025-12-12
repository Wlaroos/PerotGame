using UnityEngine;

public class DraggableHolder : MonoBehaviour
{
    public static DraggableHolder Instance { get; private set; }

    [SerializeField] private int _maxDraggables = 10;
    public int MaxDraggables => _maxDraggables;
    public bool IsFull => transform.childCount >= _maxDraggables;

    private void Awake()
    {
        // Set up singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void AddDraggable(GameObject draggable)
    {
        // If the holder is full, remove the oldest child
        if (IsFull)
        {
            RemoveOldestChild();
        }

        // Add the new draggable as a child
        draggable.transform.SetParent(transform);
    }

    private void RemoveOldestChild()
    {
        if (transform.childCount > 0)
        {
            var oldestChild = transform.GetChild(0).gameObject;
            Destroy(oldestChild);
        }
    }

    public void DeleteAllChildren(bool useImmediate = false)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (useImmediate)
                DestroyImmediate(child);
            else
                Destroy(child);
        }
    }

    public void FullPopup()
    {
        // Implement UI popup logic here
        Debug.Log("DraggableHolder is full!");
    }
}
