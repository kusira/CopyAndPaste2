using System.Reflection;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridGenerator))]
public class GridGeneratorEditor : Editor
{
    private FieldInfo massParentField;
    private FieldInfo rockParentField;

    private void OnEnable()
    {
        // リフレクションでフィールドを取得
        massParentField = typeof(GridGenerator).GetField("massParent", BindingFlags.NonPublic | BindingFlags.Instance);
        rockParentField = typeof(GridGenerator).GetField("rockParent", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public override void OnInspectorGUI()
    {
        // デフォルトのInspectorを表示
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GridGenerator generator = (GridGenerator)target;

        EditorGUILayout.LabelField("デバッグ", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // グリッド再生成ボタン
        if (GUILayout.Button("グリッドを再生成", GUILayout.Height(30)))
        {
            // エディタモードでクリア
            ClearGridInEditor(generator);
            
            // グリッドを生成
            generator.GenerateGrid();
            
            Debug.Log("グリッドを再生成しました");
        }

        EditorGUILayout.Space();

        // グリッドクリアボタン
        if (GUILayout.Button("グリッドをクリア", GUILayout.Height(30)))
        {
            ClearGridInEditor(generator);
            Debug.Log("グリッドをクリアしました");
        }
    }

    /// <summary>
    /// エディタモードでグリッドをクリアします（永久ループを防ぐため）
    /// </summary>
    private void ClearGridInEditor(GridGenerator generator)
    {
        // リフレクションでフィールドの値を取得
        Transform massParent = (Transform)massParentField.GetValue(generator);
        Transform rockParent = (Transform)rockParentField.GetValue(generator);

        // MassParentの子オブジェクトをクリア
        Transform massParentTransform = massParent != null ? massParent : generator.transform;
        int massChildCount = massParentTransform.childCount;
        for (int i = massChildCount - 1; i >= 0; i--)
        {
            DestroyImmediate(massParentTransform.GetChild(i).gameObject);
        }

        // RockParentの子オブジェクトをクリア（MassParentと異なる場合のみ）
        Transform rockParentTransform = rockParent != null ? rockParent : generator.transform;
        if (rockParentTransform != massParentTransform)
        {
            int rockChildCount = rockParentTransform.childCount;
            for (int i = rockChildCount - 1; i >= 0; i--)
            {
                DestroyImmediate(rockParentTransform.GetChild(i).gameObject);
            }
        }
    }
}
