using System.Collections.Generic;
using UnityEngine;

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

    public bool TryCraft(Element element1, Element element2)
    {
        string key = $"{element1.Type}{element1.IsotopeNumber}+{element2.Type}{element2.IsotopeNumber}";
        if (craftingRecipes.TryGetValue(key, out var result))
        {
            ElementSpawner.Instance.SpawnElement(result.Item1, result.Item2);
            return true;
        }

        Debug.Log("Crafting failed: No matching recipe.");
        return false;
    }
}