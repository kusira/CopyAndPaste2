using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RSBehavior))]
public class RSBehaviorEditor : Editor
{
    private SerializedProperty dashLinePrefabProperty;
    private SerializedProperty invalidColorProperty;
    private SerializedProperty previewAlphaProperty;
    private SerializedProperty invalidAlphaProperty;
    private SerializedProperty selectionLTProperty;
    private SerializedProperty selectionRTProperty;
    private SerializedProperty selectionLBProperty;
    private SerializedProperty selectionRBProperty;

    private void OnEnable()
    {
        dashLinePrefabProperty = serializedObject.FindProperty("dashLinePrefab");
        invalidColorProperty = serializedObject.FindProperty("invalidColor");
        previewAlphaProperty = serializedObject.FindProperty("previewAlpha");
        invalidAlphaProperty = serializedObject.FindProperty("invalidAlpha");
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
        if (previewAlphaProperty != null)
        {
            EditorGUILayout.PropertyField(previewAlphaProperty, new GUIContent("Preview Alpha", "コピーしたもの（プレビュー）の透明度を指定します（0.0～1.0）"));
        }
        if (invalidAlphaProperty != null)
        {
            EditorGUILayout.PropertyField(invalidAlphaProperty, new GUIContent("Invalid Alpha", "Invalid状態になったときのRSオブジェクトの透明度を指定します（0.0～1.0）"));
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

