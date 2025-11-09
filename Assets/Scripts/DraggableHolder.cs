using UnityEngine;

public class DraggableHolder : MonoBehaviour
{
    public static DraggableHolder Instance { get; private set; }

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
}
