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
    public ScriptableObject inputF; // Sixth ingredient (element or compound)
    public ScriptableObject inputG; // Seventh ingredient (element or compound)
    public ScriptableObject inputH; // Eighth ingredient (element or compound)
    [Space]
    public ScriptableObject output; // Result (element, compound, or mineral)
    public enum ProductType
    {
        Test,
        Element,
        Compound,
        Mineral,
    }
    [Space]
    public ProductType productType;

}