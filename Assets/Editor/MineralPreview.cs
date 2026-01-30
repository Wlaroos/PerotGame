using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[CustomEditor(typeof(MineralData))]
public class MineralPreview : Editor
{
    MineralData _mineralSO;
    private Sprite _sprite;
    private Sprite _bigSprite;
    private Color _color;

    void OnEnable()
    {
        _mineralSO = (MineralData)target;
        UpdatePreviewData();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (_mineralSO == null)
        {
            return;
        }

        // Check if any property has changed
        if (GUI.changed)
        {
            UpdatePreviewData();
            EditorUtility.SetDirty(_mineralSO); // Mark the object as dirty
            Repaint(); // Repaint the inspector to reflect changes
        }

        Texture2D sprite = AssetPreview.GetAssetPreview(_sprite);
        Texture2D bigSprite = AssetPreview.GetAssetPreview(_bigSprite);

        // Apply color tint
        Color originalColor = GUI.color; // Save the original GUI color
        GUI.color = _color; // Set the color to the desired tint

        // Draw the sprite with the color applied
        if (sprite != null)
        {
            GUILayout.Label("", GUILayout.Width(100), GUILayout.Height(100)); // Reserve space for the first sprite
            GUI.DrawTexture(GUILayoutUtility.GetLastRect(), sprite, ScaleMode.ScaleToFit);
        }

        // Restore the original GUI color before the next draw
        GUI.color = originalColor;

        if (bigSprite != null)
        {
            GUILayout.Label("", GUILayout.Width(100), GUILayout.Height(100)); // Reserve space for the second sprite
            GUI.DrawTexture(GUILayoutUtility.GetLastRect(), bigSprite, ScaleMode.ScaleToFit);
        }
    }

    private void UpdatePreviewData()
    {
        _sprite = _mineralSO.mineralSprite;
        _bigSprite = _mineralSO.mineralBigSprite;
        _color = _mineralSO.defaultColor;
    }
}
