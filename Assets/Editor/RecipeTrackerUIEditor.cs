using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(RecipeTrackerUI))]
public class RecipeTrackerUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var ui = target as RecipeTrackerUI;
        if (ui == null) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        if (GUILayout.Button("Preview Recipes from Resources"))
        {
            Undo.RecordObject(ui, "Preview Recipes");
            ui.PreviewPopulateFromResources();
            EditorUtility.SetDirty(ui);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        }

        if (GUILayout.Button("Clear Preview"))
        {
            Undo.RecordObject(ui, "Clear Recipe List");
            ui.ClearList();
            EditorUtility.SetDirty(ui);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        }
    }
}
