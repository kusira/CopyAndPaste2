using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RangeSelectorBehavior))]
public class RangeSelectorBehaviorEditor : Editor
{
    private SerializedProperty dashLinePrefabProperty;
    private SerializedProperty invalidColorProperty;

    private void OnEnable()
    {
        dashLinePrefabProperty = serializedObject.FindProperty("dashLinePrefab");
        invalidColorProperty = serializedObject.FindProperty("invalidColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Prefabs
        EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
        if (dashLinePrefabProperty != null)
        {
            EditorGUILayout.PropertyField(dashLinePrefabProperty, new GUIContent("Dash Line Prefab", "コピー時の矩形を囲む点線のPrefabをアサインします"));
        }

        EditorGUILayout.Space();

        // Color
        EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
        if (invalidColorProperty != null)
        {
            EditorGUILayout.PropertyField(invalidColorProperty, new GUIContent("Invalid Color"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}

