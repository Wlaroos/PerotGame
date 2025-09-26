using UnityEngine;
using UnityEngine.Rendering;

public class DragAndDrop : MonoBehaviour
{
    [SerializeField] private float _dragSpeed = 25f; // Speed of interpolation
    private Vector2 _mouseOffset;
    private Element _element;
    private Compound _compound;
    private Mineral _mineral;
    private Collider2D _collider;
    private SortingGroup _sg;

    [Header("Prefabs")]
    [SerializeField] private GameObject elementPrefab;
    [SerializeField] private GameObject compoundPrefab;
    [SerializeField] private GameObject mineralPrefab;

    [Header("Effects")]
    [SerializeField] private GameObject failParticles;

    private void Awake()
    {
        _element = GetComponent<Element>();
        _compound = GetComponent<Compound>();
        _mineral = GetComponent<Mineral>();

        if (_element == null && _compound == null && _mineral == null)
        {
            Debug.LogError("DragAndDrop: Missing Element, Compound, or Mineral component");
        }

        _sg = GetComponent<SortingGroup>();
    }

    private Vector2 GetMouseWorldPosition()
    {
        // Convert screen position to world position in 2D
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseDown()
    {
        // Calculate the offset between the object's position and the mouse position
        _mouseOffset = (Vector2)transform.position - GetMouseWorldPosition();
        if (_sg != null)
            _sg.sortingLayerName = "Dragging";
    }

    private void OnMouseDrag()
    {
        // Calculate the target position based on the mouse position and offset
        Vector2 targetPosition = GetMouseWorldPosition() + _mouseOffset;

        // Smoothly interpolate the object's position towards the target position
        transform.position = Vector2.Lerp(transform.position, targetPosition, _dragSpeed * Time.deltaTime);
    }

    private void OnMouseUp()
    {
        if (_collider != null)
        {
            GameObject otherObj = _collider.gameObject;

            // Get data ScriptableObjects for both objects
            Element elementA = GetComponent<Element>();
            Element elementB = otherObj.GetComponent<Element>();
            Compound compoundA = GetComponent<Compound>();
            Compound compoundB = otherObj.GetComponent<Compound>();
            Mineral mineralA = GetComponent<Mineral>();
            Mineral mineralB = otherObj.GetComponent<Mineral>();

            ScriptableObject dataA =
                (ScriptableObject)elementA?.data ??
                (ScriptableObject)compoundA?.data ??
                (ScriptableObject)mineralA?.data;

            ScriptableObject dataB =
                (ScriptableObject)elementB?.data ??
                (ScriptableObject)compoundB?.data ??
                (ScriptableObject)mineralB?.data;

            // Try crafting
            ScriptableObject result = CraftingManager.Instance.TryCraft(dataA, dataB);

            if (result != null)
            {
                // Instantiate the result prefab and assign the ScriptableObject data
                Vector3 spawnPosition = (transform.position + otherObj.transform.position) / 2f;

                if (result is MineralData mineralData)
                {
                    GameObject mineralObj = Instantiate(mineralPrefab, spawnPosition, Quaternion.identity);
                    Mineral mineralComponent = mineralObj.GetComponent<Mineral>();
                    if (mineralComponent != null)
                    {
                        mineralComponent.data = mineralData;
                    }
                }
                else if (result is CompoundData compoundData)
                {
                    GameObject compoundObj = Instantiate(compoundPrefab, spawnPosition, Quaternion.identity);
                    Compound compoundComponent = compoundObj.GetComponent<Compound>();
                    if (compoundComponent != null)
                    {
                        compoundComponent.data = compoundData;
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
                        elementComponent.SetNumberSprites(elementComponent.isotopeNumber);
                    }
                }

                Destroy(otherObj);
                Destroy(gameObject);
            }
            else
            {
                // Separation logic: always separate on failed craft
                Vector3 separationDirection = (otherObj.transform.position - transform.position);
                if (separationDirection == Vector3.zero)
                {
                    separationDirection = Random.insideUnitCircle.normalized;
                }
                else
                {
                    separationDirection = separationDirection.normalized;
                }
                float separationDistance = 0.33f;
                if (TryGetComponent(out Collider colA) && otherObj.TryGetComponent(out Collider colB))
                {
                    separationDistance += (colA.bounds.size.magnitude + colB.bounds.size.magnitude) / 2;
                }
                transform.position -= separationDirection * separationDistance;
                otherObj.transform.position += separationDirection * separationDistance;

                // Particle effect at midpoint
                Vector3 failPosition = (transform.position + otherObj.transform.position) / 2f;
                if (failParticles != null)
                {
                    Instantiate(failParticles, failPosition, Quaternion.identity);
                }
            }
        }
        if (_sg != null)
            _sg.sortingLayerName = "Default";
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        _collider = collision;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_collider == collision)
        {
            _collider = null;
        }
    }

    private void Update()
    {
        // Delete this GameObject on right mouse button click while mouse is over it
        if (Input.GetMouseButtonDown(1)) // 1 = right mouse button
        {
            // Check if mouse is over this object using a raycast
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mousePos);
            if (hit != null && hit.gameObject == gameObject)
            {
                Destroy(gameObject);
            }
        }
    }
}
