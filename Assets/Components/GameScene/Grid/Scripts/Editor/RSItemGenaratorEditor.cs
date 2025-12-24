using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StickyNotesGenerator))]
public class StickyNotesGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        StickyNotesGenerator generator = (StickyNotesGenerator)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Generator Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("アイテム生成"))
        {
            generator.GenerateItems();
        }

        if (GUILayout.Button("アイテムクリア"))
        {
            generator.ClearItems();
        }
    }
}
