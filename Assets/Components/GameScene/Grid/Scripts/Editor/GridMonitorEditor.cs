using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridMonitor))]
public class GridMonitorEditor : Editor
{
    private FieldInfo currentGameStatusField;
    private FieldInfo progressManagerField;
    private Vector2 scrollPosition;

    private void OnEnable()
    {
        // リフレクションでフィールドを取得
        currentGameStatusField = typeof(GridMonitor).GetField("currentGameStatus", BindingFlags.NonPublic | BindingFlags.Instance);
        progressManagerField = typeof(GridMonitor).GetField("ProgressManager", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public override void OnInspectorGUI()
    {
        // デフォルトのInspectorを表示
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GridMonitor monitor = (GridMonitor)target;

        // リアルタイムでグリッド情報を表示
        DrawGridInfo(monitor);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.LabelField("デバッグ", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 監視状態をリセットボタン
        if (GUILayout.Button("監視状態をリセット", GUILayout.Height(30)))
        {
            monitor.ResetMonitor();
            Debug.Log("監視状態をリセットしました");
        }

        EditorGUILayout.Space();

        // Progress再計算ボタン
        if (GUILayout.Button("Progressを再計算", GUILayout.Height(30)))
        {
            monitor.RecalculateProgress();
            Debug.Log("Progressを再計算しました");
        }
    }

    /// <summary>
    /// グリッド情報をリアルタイムで表示します
    /// </summary>
    private void DrawGridInfo(GridMonitor monitor)
    {
        EditorGUILayout.LabelField("現在のグリッド情報", EditorStyles.boldLabel);

        // CurrentGameStatusを取得
        CurrentGameStatus currentGameStatus = null;
        if (currentGameStatusField != null)
        {
            currentGameStatus = currentGameStatusField.GetValue(monitor) as CurrentGameStatus;
        }

        if (currentGameStatus == null)
        {
            EditorGUILayout.HelpBox("CurrentGameStatusがGridMonitorに設定されていません", MessageType.Warning);
            return;
        }

        StageDatabase.StageData stageData = currentGameStatus.GetCurrentStageData();
        if (stageData == null)
        {
            EditorGUILayout.HelpBox("ステージデータが取得できません", MessageType.Warning);
            return;
        }

        List<StageDatabase.RowData> massStatus = stageData.massStatus;
        List<StageDatabase.RowData> rockStatus = stageData.rockStatus;

        if (massStatus == null || massStatus.Count == 0)
        {
            EditorGUILayout.HelpBox("MassStatusが設定されていません", MessageType.Warning);
            return;
        }

        int height = massStatus.Count;
        int width = massStatus[0] != null && massStatus[0].columns != null ? massStatus[0].columns.Count : 0;

        EditorGUILayout.LabelField($"グリッドサイズ: {width} × {height}");

        // セルサイズ（正方形）
        const float cellSize = 30f;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("MassStatus", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        
        for (int h = height - 1; h >= 0; h--)
        {
            EditorGUILayout.BeginHorizontal();
            
            if (massStatus[h] != null && massStatus[h].columns != null)
            {
                for (int w = 0; w < width; w++)
                {
                    string value = w < massStatus[h].columns.Count ? massStatus[h].columns[w] : "";
                    string display = string.IsNullOrEmpty(value) ? " " : value;
                    EditorGUILayout.LabelField(display, GUILayout.Width(cellSize), GUILayout.Height(cellSize));
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("RockStatus", EditorStyles.boldLabel);
        Vector2 scrollPosition2 = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        scrollPosition = scrollPosition2;
        
        if (rockStatus != null && rockStatus.Count > 0)
        {
            for (int h = height - 1; h >= 0; h--)
            {
                EditorGUILayout.BeginHorizontal();
                
                if (h < rockStatus.Count && rockStatus[h] != null && rockStatus[h].columns != null)
                {
                    for (int w = 0; w < width; w++)
                    {
                        string value = w < rockStatus[h].columns.Count ? rockStatus[h].columns[w] : "";
                        string display = string.IsNullOrEmpty(value) ? " " : value;
                        EditorGUILayout.LabelField(display, GUILayout.Width(cellSize), GUILayout.Height(cellSize));
                    }
                }
                else
                {
                    for (int w = 0; w < width; w++)
                    {
                        EditorGUILayout.LabelField(" ", GUILayout.Width(cellSize), GUILayout.Height(cellSize));
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("RockStatusが設定されていません", MessageType.Info);
        }
        EditorGUILayout.EndScrollView();
    }
}

