using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RSItemGenarator))]
public class RSItemGenaratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        RSItemGenarator generator = (RSItemGenarator)target;

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
