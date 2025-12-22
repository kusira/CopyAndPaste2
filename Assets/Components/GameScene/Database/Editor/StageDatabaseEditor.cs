using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StageDatabase))]
public class StageDatabaseEditor : Editor
{
    private Vector2 scrollPosition;
    private int selectedStageIndex = 0;

    private UnityEditorInternal.ReorderableList reorderableList;

    private string GetSessionStateKey()
    {
        return $"StageDatabaseEditor_SelectedStageIndex_{target.GetInstanceID()}";
    }

    private void OnEnable()
    {
        StageDatabase database = (StageDatabase)target;
        reorderableList = new UnityEditorInternal.ReorderableList(serializedObject, serializedObject.FindProperty("stages"), true, true, true, true);

        // SessionStateから選択状態を復元
        selectedStageIndex = SessionState.GetInt(GetSessionStateKey(), 0);
        
        // 範囲チェック
        if (database.stages != null && selectedStageIndex >= database.stages.Count)
        {
            selectedStageIndex = 0;
        }

        // リストの選択状態に反映
        reorderableList.index = selectedStageIndex;

        reorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "ステージ一覧");
        };

        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty stageNameProp = element.FindPropertyRelative("stageName");
            string stageName = stageNameProp.stringValue;
            
            rect.y += 2;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), 
                $"Stage {index + 1}: {stageName}");
        };

        reorderableList.onSelectCallback = (UnityEditorInternal.ReorderableList list) =>
        {
            selectedStageIndex = list.index;
            // 選択変更時に保存
            SessionState.SetInt(GetSessionStateKey(), selectedStageIndex);
        };
    }

    public override void OnInspectorGUI()
    {
        StageDatabase database = (StageDatabase)target;
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("ステージデータベース", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // ReorderableListの表示
        reorderableList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();

        if (database.stages.Count == 0)
        {
            EditorGUILayout.HelpBox("ステージがありません。ステージを追加してください。", MessageType.Info);
            return;
        }

        // 選択インデックスの検証
        if (selectedStageIndex < 0 || selectedStageIndex >= database.stages.Count)
        {
            selectedStageIndex = 0;
        }
        
        // ReorderableListの選択状態と同期（選択されていなければ、リスト側を選択状態にする）
        if (reorderableList.index != selectedStageIndex)
        {
            reorderableList.index = selectedStageIndex;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // 以下、詳細編集画面
        // 既存のコードの多くを流用するが、ターゲットは database.stages[selectedStageIndex]
        StageDatabase.StageData stageData = database.stages[selectedStageIndex];

        EditorGUILayout.LabelField($"選択中のステージ: {stageData.stageName} (Index: {selectedStageIndex})", EditorStyles.boldLabel);

        // ステージ名の編集
        string newStageName = EditorGUILayout.TextField("ステージ名", stageData.stageName);
        if (newStageName != stageData.stageName)
        {
            Undo.RecordObject(database, "ステージ名を変更");
            stageData.stageName = newStageName;
            EditorUtility.SetDirty(database);
        }

        EditorGUILayout.Space();

        // グリッドサイズの設定
        EditorGUILayout.LabelField("グリッドサイズ", EditorStyles.boldLabel);
        
        // 現在のグリッドサイズを取得
        int currentWidth = stageData.massStatus.Count > 0 && stageData.massStatus[0] != null 
            ? stageData.massStatus[0].columns.Count 
            : 0;
        int currentHeight = stageData.massStatus.Count;
        
        // グリッドサイズの入力フィールド
        int gridWidth = EditorGUILayout.IntField("幅 (W)", currentWidth);
        int gridHeight = EditorGUILayout.IntField("高さ (H)", currentHeight);
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);

        // サイズが変更されたらリアルタイムで適用
        if (gridWidth != currentWidth || gridHeight != currentHeight)
        {
            Undo.RecordObject(database, "グリッドサイズを変更");
            ResizeGrid(stageData.massStatus, gridWidth, gridHeight);
            ResizeGrid(stageData.rockStatus, gridWidth, gridHeight);
            EditorUtility.SetDirty(database);
            RefreshGrid();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // MassStatusの編集（更新されたサイズを使用）
        EditorGUILayout.LabelField("MassStatus ('.'でMassを配置)", EditorStyles.boldLabel);
        DrawGridEditor(database, stageData.massStatus, gridWidth, gridHeight, "Mass");

        EditorGUILayout.Space();

        // RockStatusの編集（更新されたサイズを使用）
        EditorGUILayout.LabelField("RockStatus ('#'でRockを配置)", EditorStyles.boldLabel);
        DrawGridEditor(database, stageData.rockStatus, gridWidth, gridHeight, "Rock");

        EditorGUILayout.Space();

        // クリアボタン
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("MassStatusをクリア"))
        {
            Undo.RecordObject(database, "MassStatusをクリア");
            ClearGrid(stageData.massStatus);
            EditorUtility.SetDirty(database);
            RefreshGrid();
        }
        if (GUILayout.Button("RockStatusをクリア"))
        {
            Undo.RecordObject(database, "RockStatusをクリア");
            ClearGrid(stageData.rockStatus);
            EditorUtility.SetDirty(database);
            RefreshGrid();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // RangeSelectorItemの編集
        EditorGUILayout.LabelField("RangeSelectorItem", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("i個目のアイテムのサイズはH_i*W_iで指定されます", MessageType.Info);

        // アイテム数の設定
        // シンプルなIntFieldではなく、スクロールビュー内での追加削除UIにしたほうが良いかもしれないが、
        // 既存のコードを尊重してIntFieldのままでも機能はする。
        int itemCount = EditorGUILayout.IntField("アイテム数", stageData.rangeSelectorItems.Count);
        if (itemCount != stageData.rangeSelectorItems.Count)
        {
            Undo.RecordObject(database, "RangeSelectorItem数を変更");
            while (stageData.rangeSelectorItems.Count < itemCount)
            {
                stageData.rangeSelectorItems.Add(new StageDatabase.RangeSelectorItemData());
            }
            while (stageData.rangeSelectorItems.Count > itemCount)
            {
                stageData.rangeSelectorItems.RemoveAt(stageData.rangeSelectorItems.Count - 1);
            }
            EditorUtility.SetDirty(database);
        }

        EditorGUILayout.Space();

        // 各アイテムのサイズを編集
        for (int i = 0; i < stageData.rangeSelectorItems.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"アイテム {i}:", GUILayout.Width(80));

            StageDatabase.RangeSelectorItemData item = stageData.rangeSelectorItems[i];
            if (item == null)
            {
                item = new StageDatabase.RangeSelectorItemData();
                stageData.rangeSelectorItems[i] = item;
            }

            // H<数字入力>×W<数字入力>の形式で表示
            EditorGUILayout.LabelField("H", GUILayout.Width(15));
            int newHeight = EditorGUILayout.IntField(item.height, GUILayout.Width(50));
            EditorGUILayout.LabelField("×", GUILayout.Width(20));
            EditorGUILayout.LabelField("W", GUILayout.Width(15));
            int newWidth = EditorGUILayout.IntField(item.width, GUILayout.Width(50));

            newHeight = Mathf.Max(1, newHeight);
            newWidth = Mathf.Max(1, newWidth);

            if (newHeight != item.height || newWidth != item.width)
            {
                Undo.RecordObject(database, $"RangeSelectorItem[{i}]を変更");
                item.height = newHeight;
                item.width = newWidth;
                EditorUtility.SetDirty(database);
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawGridEditor(StageDatabase database, List<StageDatabase.RowData> grid, int width, int height, string label)
    {
        // ResizeGridの呼び出しを削除 - これが原因でデータがリセットされていました
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

        EditorGUILayout.BeginVertical();

        // 列ヘッダー
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.Width(50));
        for (int w = 0; w < width; w++)
        {
            EditorGUILayout.LabelField(w.ToString(), GUILayout.Width(30), GUILayout.Height(20));
        }
        EditorGUILayout.EndHorizontal();

        // グリッドの各行
        for (int h = height - 1; h >= 0; h--)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(h.ToString(), GUILayout.Width(50), GUILayout.Height(20));

            // 行が存在するかチェック
            if (h < grid.Count && grid[h] != null && grid[h].columns != null)
            {
                for (int w = 0; w < width; w++)
                {
                    if (w < grid[h].columns.Count)
                    {
                        string value = grid[h].columns[w] ?? "";
                        string newValue = EditorGUILayout.TextField(value, GUILayout.Width(60), GUILayout.Height(20));

                        if (newValue != value)
                        {
                            Undo.RecordObject(database, $"{label}Statusを編集");
                            grid[h].columns[w] = newValue;
                            EditorUtility.SetDirty(database);
                            RefreshGrid();
                        }
                    }
                    else
                    {
                        // 幅が足りない場合は空のセルを表示
                        EditorGUILayout.TextField("", GUILayout.Width(60), GUILayout.Height(20));
                    }
                }
            }
            else
            {
                // 行が存在しない場合は空のセルを表示
                for (int w = 0; w < width; w++)
                {
                    EditorGUILayout.TextField("", GUILayout.Width(60), GUILayout.Height(20));
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();

        // 説明テキスト
        EditorGUILayout.HelpBox(
            label == "Mass" 
                ? "'.' を入力するとMassが生成されます。空欄の場合は何も生成されません。" 
                : "'#' を入力するとRockが生成されます。空欄の場合は何も生成されません。",
            MessageType.Info);
    }

    private void ResizeGrid(List<StageDatabase.RowData> grid, int width, int height)
    {
        // 高さの調整
        while (grid.Count < height)
        {
            grid.Add(new StageDatabase.RowData());
        }
        while (grid.Count > height)
        {
            grid.RemoveAt(grid.Count - 1);
        }

        // 幅の調整
        for (int h = 0; h < grid.Count; h++)
        {
            if (grid[h] == null)
            {
                grid[h] = new StageDatabase.RowData();
            }
            if (grid[h].columns == null)
            {
                grid[h].columns = new List<string>();
            }
            while (grid[h].columns.Count < width)
            {
                grid[h].columns.Add("");
            }
            while (grid[h].columns.Count > width)
            {
                grid[h].columns.RemoveAt(grid[h].columns.Count - 1);
            }
        }
    }

    private void ClearGrid(List<StageDatabase.RowData> grid)
    {
        for (int h = 0; h < grid.Count; h++)
        {
            if (grid[h] != null && grid[h].columns != null)
            {
                for (int w = 0; w < grid[h].columns.Count; w++)
                {
                    grid[h].columns[w] = "";
                }
            }
        }
    }

    private void RefreshGrid()
    {
        GridGenerator generator = FindFirstObjectByType<GridGenerator>();
        if (generator != null)
        {
            generator.GenerateGrid();
        }
    }
}

