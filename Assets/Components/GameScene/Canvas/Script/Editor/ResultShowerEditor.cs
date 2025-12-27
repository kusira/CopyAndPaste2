using UnityEngine;
using UnityEditor;

/// <summary>
/// ResultShowerのカスタムエディタ
/// </summary>
[CustomEditor(typeof(ResultShower))]
public class ResultShowerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // デフォルトのインスペクタを表示
        DrawDefaultInspector();

        EditorGUILayout.Space();

        ResultShower resultShower = (ResultShower)target;

        // アニメーションを再現するボタン
        if (GUILayout.Button("アニメーションを再現", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                resultShower.ShowResult();
                Debug.Log("ResultShower: アニメーションを再現しました");
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "アニメーションを再現するには、再生モードで実行してください。", "OK");
            }
        }
    }
}

