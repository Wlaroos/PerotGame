using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    public List<CraftingRecipe> recipes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public ScriptableObject TryCraft(ScriptableObject a, ScriptableObject b)
    {
        foreach (var recipe in recipes)
        {
            if ((recipe.inputA == a && recipe.inputB == b) || (recipe.inputA == b && recipe.inputB == a))
            {
                return recipe.output;
            }
        }
        return null;
    }
}