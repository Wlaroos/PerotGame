using UnityEngine;
using UnityEngine.Rendering;

// Lets you drag, drop, and combine objects in the game
public class DragAndDrop : MonoBehaviour
{
    [SerializeField] private float _dragSpeed = 25f; // How fast the object follows the mouse
    private Vector2 _mouseOffset; // Distance from mouse to object when dragging starts
    private Element _element;     // Reference if this is an element
    private Compound _compound;   // Reference if this is a compound
    private Mineral _mineral;     // Reference if this is a mineral
    private Collider2D _collider; // The collider we're overlapping with
    private SortingGroup _sg;     // For controlling draw order

    [Header("Prefabs")]
    [SerializeField] private GameObject elementPrefab;   // Prefab for new elements
    [SerializeField] private GameObject compoundPrefab;  // Prefab for new compounds
    [SerializeField] private GameObject mineralPrefab;   // Prefab for new minerals

    [Header("Effects")]
    [SerializeField] private GameObject craftParticles;  // Effect when crafting works
    [SerializeField] private GameObject failParticles;   // Effect when crafting fails

    // Runs when the object is created
    private void Awake()
    {
        // Get references to possible components
        _element = GetComponent<Element>();
        _compound = GetComponent<Compound>();
        _mineral = GetComponent<Mineral>();

        // Warn if none are found
        if (_element == null && _compound == null && _mineral == null)
        {
            Debug.LogError("DragAndDrop: Missing Element, Compound, or Mineral component");
        }

        // Get sorting group for draw order
        _sg = GetComponent<SortingGroup>();
    }

    // Gets the mouse position in world space
    private Vector2 GetMouseWorldPosition()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    // When you click on the object
    private void OnMouseDown()
    {
        // Remember how far the mouse is from the object's center
        _mouseOffset = (Vector2)transform.position - GetMouseWorldPosition();
        // Bring object to front while dragging
        if (_sg != null)
            _sg.sortingLayerName = "Dragging";
    }

    // While dragging the object
    private void OnMouseDrag()
    {
        // Move object smoothly to follow the mouse
        Vector2 targetPosition = GetMouseWorldPosition() + _mouseOffset;
        transform.position = Vector2.Lerp(transform.position, targetPosition, _dragSpeed * Time.deltaTime);
    }

    // Gets the data (element, compound, or mineral) from another object
    private ScriptableObject GetDataFromGameObject(GameObject obj)
    {
        var element = obj.GetComponent<Element>();
        if (element != null) return element.data;
        var compound = obj.GetComponent<Compound>();
        if (compound != null) return compound.data;
        var mineral = obj.GetComponent<Mineral>();
        if (mineral != null) return mineral.data;
        return null;
    }

    // When you let go of the mouse
    private void OnMouseUp()
    {
        if (_collider != null)
        {
            GameObject otherObj = _collider.gameObject;

            // Get this object's data
            ScriptableObject dataA = (ScriptableObject)_element?.data ?? (ScriptableObject)_compound?.data ?? (ScriptableObject)_mineral?.data;
            // Get the other object's data
            ScriptableObject dataB = GetDataFromGameObject(otherObj);

            // Try to craft a new object from the two
            ScriptableObject result = CraftingManager.Instance.TryCraft(dataA, dataB);

            // Where to spawn the result (between the two objects)
            Vector3 spawnPosition = (transform.position + otherObj.transform.position) / 2f;

            if (result != null)
            {
                // If crafting worked, spawn the right type of object
                if (result is MineralData mineralData)
                {
                    GameObject mineralObj = Instantiate(mineralPrefab, spawnPosition, Quaternion.identity);
                    Mineral mineralComponent = mineralObj.GetComponent<Mineral>();
                    if (mineralComponent != null)
                    {
                        mineralComponent.data = mineralData;
                        mineralComponent.UpdateDataVisuals();
                    }
                }
                else if (result is CompoundData compoundData)
                {
                    GameObject compoundObj = Instantiate(compoundPrefab, spawnPosition, Quaternion.identity);
                    Compound compoundComponent = compoundObj.GetComponent<Compound>();
                    if (compoundComponent != null)
                    {
                        compoundComponent.data = compoundData;
                        compoundComponent.UpdateDataVisuals();
                    }
                }
                else if (result is ElementData elementData)
                {
                    GameObject elementObj = Instantiate(elementPrefab, spawnPosition, Quaternion.identity);
                    Element elementComponent = elementObj.GetComponent<Element>();
                    if (elementComponent != null)
                    {
                        elementComponent.data = elementData;
                        elementComponent.isotopeNumber = elementData.defaultIsotopeNumber;
                        elementComponent.UpdateDataVisuals();
                    }
                }

                // Play crafting effect
                if (craftParticles != null)
                {
                    Instantiate(craftParticles, spawnPosition, Quaternion.Euler(-90, 0, 0));
                }

                // Remove the old objects
                Destroy(otherObj);
                Destroy(gameObject);
            }
            else
            {
                // If crafting failed, push the objects apart
                Vector3 separationDirection = (otherObj.transform.position - transform.position);
                if (separationDirection == Vector3.zero)
                {
                    // If they're on top of each other, pick a random direction
                    separationDirection = Random.insideUnitCircle.normalized;
                }
                else
                {
                    separationDirection = separationDirection.normalized;
                }
                float separationDistance = 0.33f;
                // Make sure they don't overlap, based on their size
                if (TryGetComponent(out Collider colA) && otherObj.TryGetComponent(out Collider colB))
                {
                    separationDistance += (colA.bounds.size.magnitude + colB.bounds.size.magnitude) / 2;
                }
                // Move both objects away from each other
                transform.position -= separationDirection * separationDistance;
                otherObj.transform.position += separationDirection * separationDistance;

                // Play fail effect at the midpoint
                Vector3 failPosition = (transform.position + otherObj.transform.position) / 2f;
                if (failParticles != null)
                {
                    Instantiate(failParticles, failPosition, Quaternion.Euler(-90, 0, 0));
                }
            }
        }
        // Put object back to normal draw order
        if (_sg != null)
            _sg.sortingLayerName = "Default";
    }

    // When this object touches another collider
    private void OnTriggerEnter2D(Collider2D collision)
    {
        _collider = collision;
    }

    // When this object stops touching a collider
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_collider == collision)
        {
            _collider = null;
        }
    }

    // Checks for right-click to delete this object
    private void Update()
    {
        // If right mouse button is pressed
        if (Input.GetMouseButtonDown(1)) // 1 = right mouse button
        {
            // Check if mouse is over this object
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mousePos);
            if (hit != null && hit.gameObject == gameObject)
            {
                Destroy(gameObject);
            }
        }
    }
}
