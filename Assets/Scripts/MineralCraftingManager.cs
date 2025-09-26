using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MineralCraftingManager : MonoBehaviour
{
    private static MineralCraftingManager _instance;
    public static MineralCraftingManager Instance => _instance;

    [Header("Prefabs & Effects")]
    [SerializeField] private GameObject _craftParticles;
    [SerializeField] private GameObject _failParticles;
    [SerializeField] private GameObject mineralPrefab;

    [Header("Crafting Events")]
    public UnityEvent OnSilicateCrafted = new UnityEvent();
    public UnityEvent OnIronCarbonateCrafted = new UnityEvent();
    public UnityEvent OnIronPhosphateCrafted = new UnityEvent();
    public UnityEvent OnAluminumOxideCrafted = new UnityEvent();

    private HashSet<string> craftedMinerals = new HashSet<string>();

    // Define the types for non-element compounds
    public enum CompoundType
    {
        Oxide,
        Carbonate,
        Phosphate,
        Silicate,
        Sulfate
    }

    // Recipes: (Element, Compound) => (MineralName)
    private Dictionary<string, string> mineralRecipes = new Dictionary<string, string>
    {
        { "Silicon+Oxide", "Silicate" },
        { "Iron+Carbonate", "IronCarbonate" },
        { "Iron+Phosphate", "IronPhosphate" },
        { "Aluminum+Oxide", "AluminumOxide" }
    };

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

    // Remove the TryCraft(Element, CompoundType) overload if not needed elsewhere

    public bool TryCraft(GameObject objA, GameObject objB)
    {
        // Try to find an element and a compound in either object
        Element element = objA.GetComponent<Element>() ?? objB.GetComponent<Element>();
        Compound compound = objA.GetComponent<Compound>() ?? objB.GetComponent<Compound>();

        string key = null;
        if (element != null && compound != null)
        {
            key = $"{element.Type}+{(CompoundType)compound.Type}";
        }

        if (key != null && mineralRecipes.TryGetValue(key, out string mineralName))
        {
            // Crafting success: spawn mineral at midpoint
            Vector3 spawnPosition = (objA.transform.position + objB.transform.position) / 2f;

            if (System.Enum.TryParse(mineralName, out Mineral.MineralType mineralType))
            {
                if (mineralPrefab == null)
                {
                    Debug.LogError("MineralCraftingManager: mineralPrefab is not assigned.");
                }
                else
                {
                    GameObject mineralObj = Instantiate(mineralPrefab, spawnPosition, Quaternion.identity);
                    Mineral mineralComponent = mineralObj.GetComponent<Mineral>();
                    if (mineralComponent != null)
                    {
                        mineralComponent.SetMineral(mineralType);
                    }
                    else
                    {
                        Debug.LogError("MineralCraftingManager: Spawned mineralPrefab does not have a Mineral component.");
                    }
                }
            }
            else
            {
                Debug.LogError($"Failed to parse {mineralName} to Mineral.MineralType.");
                return false;
            }

            // Particle effect
            if (_craftParticles != null)
            {
                Instantiate(_craftParticles, spawnPosition, _craftParticles.transform.rotation);
            }

            // Only invoke event once per mineral
            if (!craftedMinerals.Contains(mineralName))
            {
                craftedMinerals.Add(mineralName);
                InvokeMineralEvent(mineralName);
            }

            return true;
        }

        // Crafting failed: always separate objA and objB
        Vector3 separationDirection = (objB.transform.position - objA.transform.position);
        if (separationDirection == Vector3.zero)
        {
            // Pick a random direction if objects are perfectly overlapped
            separationDirection = Random.insideUnitCircle.normalized;
        }
        else
        {
            separationDirection = separationDirection.normalized;
        }
        float separationDistance = 0.33f;
        if (objA.TryGetComponent(out Collider colA) && objB.TryGetComponent(out Collider colB))
        {
            separationDistance += (colA.bounds.size.magnitude + colB.bounds.size.magnitude) / 2;
        }
        objA.transform.position -= separationDirection * separationDistance;
        objB.transform.position += separationDirection * separationDistance;

        // Particle effect at midpoint
        Vector3 failPosition = (objA.transform.position + objB.transform.position) / 2f;
        if (_failParticles != null)
        {
            Instantiate(_failParticles, failPosition, _failParticles.transform.rotation);
        }
        return false;
    }

    private void InvokeMineralEvent(string mineralName)
    {
        switch (mineralName)
        {
            case "Silicate":
                OnSilicateCrafted?.Invoke();
                break;
            case "IronCarbonate":
                OnIronCarbonateCrafted?.Invoke();
                break;
            case "IronPhosphate":
                OnIronPhosphateCrafted?.Invoke();
                break;
            case "AluminumOxide":
                OnAluminumOxideCrafted?.Invoke();
                break;
        }
    }
}