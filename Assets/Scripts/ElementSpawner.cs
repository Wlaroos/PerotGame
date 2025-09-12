using UnityEngine;

public class ElementSpawner : MonoBehaviour
{
    private static ElementSpawner _instance;
    public static ElementSpawner Instance => _instance;

    [SerializeField] private GameObject elementPrefab;
    public GameObject ElementPrefab => elementPrefab;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void Start()
    {
        if (elementPrefab == null)
        {
            Debug.LogWarning("ElementSpawner: No element prefab assigned.");
        }
    }

    private GameObject SpawnElement(Element.ElementType type, int isotopeNumber = 1)
    {
        if (elementPrefab == null)
        {
            Debug.LogError("ElementSpawner: Cannot spawn element, prefab is not assigned.");
            return null;
        }

        GameObject newElement = Instantiate(elementPrefab, transform.position, Quaternion.identity);
        Element elementComponent = newElement.GetComponent<Element>();
        if (elementComponent != null)
        {
            elementComponent.SetElement(type, isotopeNumber);
        }
        else
        {
            Debug.LogError("ElementSpawner: Spawned prefab does not have an Element component.");
            Destroy(newElement);
            return null;
        }

        return newElement;
    }


    public void SpawnHydrogen()
    {
        SpawnElement(Element.ElementType.Hydrogen, 2);
    }
    public void SpawnHelium()
    {
        SpawnElement(Element.ElementType.Helium, 4);
    }
    public void SpawnBeryllium()
    {
        SpawnElement(Element.ElementType.Beryllium, 8);
    }
    public void SpawnCarbon()
    {
        SpawnElement(Element.ElementType.Carbon, 12);
    }
}
