using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CraftingManager : MonoBehaviour
{
    private static CraftingManager _instance;
    public static CraftingManager Instance => _instance;

    [SerializeField] private GameObject _craftParticles;
    [SerializeField] private GameObject _failParticles;

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
            GameObject craftedElement = ElementSpawner.Instance.SpawnElementAtPosition(result.Item1, result.Item2, spawnPosition);

            // Instantiate particle effect at the spawn position
            if (_craftParticles != null)
            {
                ParticleSystem particle = Instantiate(_craftParticles, spawnPosition, _craftParticles.transform.rotation).GetComponent<ParticleSystem>();

                // Get the color from the crafted element's sprite renderer
                SpriteRenderer craftedSpriteRenderer = craftedElement.GetComponent<SpriteRenderer>();
                if (craftedSpriteRenderer != null)
                {
                    Color craftedColor = craftedSpriteRenderer.color;
                    var mainModule = particle.main;
                    mainModule.startColor = craftedColor; // Set the particle color to match the sprite
                }
                else
                {
                    Debug.LogWarning("CraftingManager: Crafted element does not have a SpriteRenderer.");
                }
            }
            else
            {
                Debug.LogWarning("CraftingManager: Particle effect prefab is not assigned.");
            }

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

        // Calculate the separation direction
        Vector3 separationDirection = (element2.transform.position - element1.transform.position).normalized;

        // Define a dynamic separation distance based on the element sizes or a base distance
        float separationDistance = 0.33f; // Base distance
        if (element1.TryGetComponent(out Collider element1Collider) && element2.TryGetComponent(out Collider element2Collider))
        {
            separationDistance += (element1Collider.bounds.size.magnitude + element2Collider.bounds.size.magnitude) / 2;
        }

        // Apply the separation
        element2.transform.position += separationDirection * separationDistance;
        element1.transform.position -= separationDirection * separationDistance;

        // Instantiate fail particle effect at the midpoint
        Vector3 failPosition = (element1.transform.position + element2.transform.position) / 2;
        if (_failParticles != null)
        {
            Instantiate(_failParticles, failPosition, _failParticles.transform.rotation);
        }
        else
        {
            Debug.LogWarning("CraftingManager: Fail particle effect prefab is not assigned.");
        }

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