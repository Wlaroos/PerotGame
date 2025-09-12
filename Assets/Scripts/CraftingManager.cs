using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CraftingManager : MonoBehaviour
{
    private static CraftingManager _instance;
    public static CraftingManager Instance => _instance;

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

    private Dictionary<string, (Element.ElementType, int)> craftingRecipes = new Dictionary<string, (Element.ElementType, int)>
    {
        { "Hydrogen1+Hydrogen1", (Element.ElementType.Hydrogen, 2) },
        { "Hydrogen2+Hydrogen2", (Element.ElementType.Helium, 3) },
        { "Helium3+Helium3", (Element.ElementType.Helium, 4) },
        { "Helium4+Helium4", (Element.ElementType.Beryllium, 8) },
        { "Helium4+Helium4+Helium4", (Element.ElementType.Carbon, 12) },
        { "Beryllium8+Helium4", (Element.ElementType.Carbon, 12) }
    };

    // Unity Events for crafting specific elements
    public UnityEvent OnHydrogen2Crafted;
    public UnityEvent OnHelium4Crafted;
    public UnityEvent OnBeryllium8Crafted;
    public UnityEvent OnCarbon12Crafted;

    private HashSet<string> craftedElements = new HashSet<string>(); // Track crafted elements to invoke events only once

    public bool TryCraft(Element element1, Element element2)
    {
        // Generate keys for both possible orders
        string key1 = $"{element1.Type}{element1.IsotopeNumber}+{element2.Type}{element2.IsotopeNumber}";
        string key2 = $"{element2.Type}{element2.IsotopeNumber}+{element1.Type}{element1.IsotopeNumber}";

        // Check both keys in the crafting recipes
        if (craftingRecipes.TryGetValue(key1, out var result) || craftingRecipes.TryGetValue(key2, out result))
        {
            // Calculate the midpoint between the two elements
            Vector3 spawnPosition = (element1.transform.position + element2.transform.position) / 2;

            // Spawn the new element at the midpoint
            ElementSpawner.Instance.SpawnElementAtPosition(result.Item1, result.Item2, spawnPosition);

            // Check if the crafted element is being crafted for the first time
            string craftedKey = $"{result.Item1}{result.Item2}";
            if (!craftedElements.Contains(craftedKey))
            {
                craftedElements.Add(craftedKey);
                InvokeCraftingEvent(result.Item1, result.Item2);
            }

            return true;
        }

        Debug.Log("Crafting failed: No matching recipe.");
        return false;
    }

    private void InvokeCraftingEvent(Element.ElementType type, int isotopeNumber)
    {
        // Invoke the appropriate Unity Event based on the crafted element
        if (type == Element.ElementType.Hydrogen && isotopeNumber == 2)
        {
            OnHydrogen2Crafted?.Invoke();
        }
        else if (type == Element.ElementType.Helium && isotopeNumber == 4)
        {
            OnHelium4Crafted?.Invoke();
        }
        else if (type == Element.ElementType.Beryllium && isotopeNumber == 8)
        {
            OnBeryllium8Crafted?.Invoke();
        }
        else if (type == Element.ElementType.Carbon && isotopeNumber == 12)
        {
            OnCarbon12Crafted?.Invoke();
        }
    }
}