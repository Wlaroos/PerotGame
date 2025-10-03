using UnityEngine;

// Stores a recipe for crafting (used as a ScriptableObject)
[CreateAssetMenu(menuName = "Chemistry/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public ScriptableObject inputA; // First ingredient (element or compound)
    public ScriptableObject inputB; // Second ingredient (element or compound)
    public ScriptableObject inputC; // Third ingredient (element or compound)
    public ScriptableObject inputD; // Fourth ingredient (element or compound)
    public ScriptableObject inputE; // Fifth ingredient (element or compound)
    public ScriptableObject output; // Result (element, compound, or mineral)
}