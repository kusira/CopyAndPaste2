using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RangeSelectorItemGenarator))]
public class RangeSelectorItemGenaratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        RangeSelectorItemGenarator generator = (RangeSelectorItemGenarator)target;

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
