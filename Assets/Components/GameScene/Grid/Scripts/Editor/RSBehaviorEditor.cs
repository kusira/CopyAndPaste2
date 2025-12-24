using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RSBehavior))]
public class RSBehaviorEditor : Editor
{
    private SerializedProperty dashLinePrefabProperty;
    private SerializedProperty invalidColorProperty;
    private SerializedProperty selectionLTProperty;
    private SerializedProperty selectionRTProperty;
    private SerializedProperty selectionLBProperty;
    private SerializedProperty selectionRBProperty;

    private void OnEnable()
    {
        dashLinePrefabProperty = serializedObject.FindProperty("dashLinePrefab");
        invalidColorProperty = serializedObject.FindProperty("invalidColor");
        selectionLTProperty = serializedObject.FindProperty("selectionLT");
        selectionRTProperty = serializedObject.FindProperty("selectionRT");
        selectionLBProperty = serializedObject.FindProperty("selectionLB");
        selectionRBProperty = serializedObject.FindProperty("selectionRB");
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

        EditorGUILayout.Space();

        // Selection Corners
        EditorGUILayout.LabelField("Selection Corners", EditorStyles.boldLabel);
        if (selectionLTProperty != null)
        {
            EditorGUILayout.PropertyField(selectionLTProperty, new GUIContent("Selection LT", "左上のSelectionオブジェクトをアサインします"));
        }
        if (selectionRTProperty != null)
        {
            EditorGUILayout.PropertyField(selectionRTProperty, new GUIContent("Selection RT", "右上のSelectionオブジェクトをアサインします"));
        }
        if (selectionLBProperty != null)
        {
            EditorGUILayout.PropertyField(selectionLBProperty, new GUIContent("Selection LB", "左下のSelectionオブジェクトをアサインします"));
        }
        if (selectionRBProperty != null)
        {
            EditorGUILayout.PropertyField(selectionRBProperty, new GUIContent("Selection RB", "右下のSelectionオブジェクトをアサインします"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}

