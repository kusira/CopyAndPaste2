using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridGenerator))]
public class GridGeneratorEditor : Editor
{
    private FieldInfo massParentField;
    private FieldInfo rockParentField;
    private FieldInfo currentGameStatusField;
    private FieldInfo stageDatabaseField;
    private Vector2 scrollPosition;

    private void OnEnable()
    {
        // リフレクションでフィールドを取得
        massParentField = typeof(GridGenerator).GetField("massParent", BindingFlags.NonPublic | BindingFlags.Instance);
        rockParentField = typeof(GridGenerator).GetField("rockParent", BindingFlags.NonPublic | BindingFlags.Instance);
        currentGameStatusField = typeof(GridGenerator).GetField("currentGameStatus", BindingFlags.NonPublic | BindingFlags.Instance);
        stageDatabaseField = typeof(GridGenerator).GetField("stageDatabase", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public override void OnInspectorGUI()
    {
        // デフォルトのInspectorを表示
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GridGenerator generator = (GridGenerator)target;

        // リアルタイムでグリッド情報を表示
        DrawGridInfo(generator);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

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

    /// <summary>
    /// グリッド情報をリアルタイムで表示します
    /// </summary>
    private void DrawGridInfo(GridGenerator generator)
    {
        EditorGUILayout.LabelField("現在のグリッド情報", EditorStyles.boldLabel);

        // CurrentGameStatusとStageDatabaseを取得
        CurrentGameStatus currentGameStatus = null;
        if (currentGameStatusField != null)
        {
            currentGameStatus = currentGameStatusField.GetValue(generator) as CurrentGameStatus;
        }

        StageDatabase stageDatabase = null;
        if (stageDatabaseField != null)
        {
            stageDatabase = stageDatabaseField.GetValue(generator) as StageDatabase;
        }

        if (stageDatabase == null)
        {
            EditorGUILayout.HelpBox("StageDatabaseがGridGeneratorに設定されていません", MessageType.Warning);
            return;
        }

        int stageIndex = 0;
        if (currentGameStatus != null)
        {
            stageIndex = currentGameStatus.GetCurrentStageIndex();
        }

        StageDatabase.StageData stageData = null;
        if (currentGameStatus != null)
        {
            stageData = currentGameStatus.GetCurrentStageData();
        }

        // CurrentGameStatusから取得できなかった場合（未アサイン時など）は直接Databaseから取得
        if (stageData == null)
        {
             stageData = stageDatabase.GetStageData(stageIndex);
        }
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
                    EditorGUILayout.LabelField(display, GUILayout.Width(100));
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
                        EditorGUILayout.LabelField(display, GUILayout.Width(100));
                    }
                }
                else
                {
                    for (int w = 0; w < width; w++)
                    {
                        EditorGUILayout.LabelField(" ", GUILayout.Width(100));
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

