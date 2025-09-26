using UnityEngine;

[CreateAssetMenu(menuName = "Chemistry/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public ScriptableObject inputA; // ElementData or CompoundData
    public ScriptableObject inputB; // ElementData or CompoundData
    public ScriptableObject output; // ElementData, CompoundData, or MineralData
}