using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[CustomEditor(typeof(CompoundData))]

public class CompoundPreview : Editor
{
    CompoundData _compoundSO;
    private Sprite _sprite;
    private Color _color;

    void OnEnable()
    {
        _compoundSO = (CompoundData)target;
        _sprite = _compoundSO.compoundSprite;
        _color = _compoundSO.defaultColor;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (_compoundSO == null)
        {
            return;
        }

        Texture2D sprite = AssetPreview.GetAssetPreview(_sprite);

        // Define Image Size
        GUILayout.Label("", GUILayout.Width(100), GUILayout.Height(100));

        // Apply color tint
        Color originalColor = GUI.color; // Save the original GUI color
        GUI.color = _color; // Set the color to the desired tint

        // Draw the sprite with the color applied
        GUI.DrawTexture(GUILayoutUtility.GetLastRect(), sprite, ScaleMode.ScaleToFit);

        // Restore the original GUI color
        GUI.color = originalColor;
    }
}
