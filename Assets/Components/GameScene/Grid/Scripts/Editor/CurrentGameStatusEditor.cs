using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CurrentGameStatus))]
public class CurrentGameStatusEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CurrentGameStatus status = (CurrentGameStatus)target;

        // エディタでも最新の値を読み込む
        status.LoadMaxReachedStageIndex();
        
        // serializedObjectを更新
        serializedObject.Update();

        // デフォルトのInspectorを表示
        DrawDefaultInspector();
        
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.LabelField("到達ステージ管理", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 到達したステージの最大値を表示
        int maxReached = status.GetMaxReachedStageIndex();
        EditorGUILayout.LabelField($"到達したステージの最大値: {maxReached}");

        EditorGUILayout.Space();

        // リセットボタン
        if (GUILayout.Button("到達ステージ最大値をリセット", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "到達ステージ最大値をリセット",
                "到達したステージの最大値をリセットしますか？",
                "リセット",
                "キャンセル"))
            {
                status.ResetMaxReachedStageIndex();
                EditorUtility.SetDirty(status);
            }
        }
    }
}

