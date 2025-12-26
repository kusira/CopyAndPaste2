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
            // CurrentGameStatusも更新
            RefreshCurrentGameStatus();
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
        int previousSelectedIndex = selectedStageIndex;
        if (reorderableList.index != selectedStageIndex)
        {
            selectedStageIndex = reorderableList.index;
            // 選択が変更された場合、CurrentGameStatusも更新
            if (previousSelectedIndex != selectedStageIndex)
            {
                RefreshCurrentGameStatus();
            }
        }
        else if (reorderableList.index != previousSelectedIndex)
        {
            // ReorderableListのインデックスが変更された場合も更新
            selectedStageIndex = reorderableList.index;
            RefreshCurrentGameStatus();
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

        // ワールドラベルの編集
        string newWorldLabel = EditorGUILayout.TextField("ワールドラベル", stageData.worldLabel);
        if (newWorldLabel != stageData.worldLabel)
        {
            Undo.RecordObject(database, "ワールドラベルを変更");
            stageData.worldLabel = newWorldLabel;
            EditorUtility.SetDirty(database);
            RefreshGrid();
        }

        EditorGUILayout.Space();

        // GridParentのScaleの編集
        EditorGUILayout.LabelField("GridParent Scale", EditorStyles.boldLabel);
        float newScaleXY = EditorGUILayout.FloatField("Scale (X, Y)", stageData.gridParentScaleXY);
        if (newScaleXY != stageData.gridParentScaleXY)
        {
            Undo.RecordObject(database, "GridParent Scaleを変更");
            stageData.gridParentScaleXY = newScaleXY;
            EditorUtility.SetDirty(database);
            RefreshGrid();
        }
        EditorGUILayout.Space();

        // チュートリアル表示タイプの編集
        EditorGUILayout.LabelField("チュートリアル表示設定", EditorStyles.boldLabel);
        StageDatabase.TutorialDisplayType newTutorialType = (StageDatabase.TutorialDisplayType)EditorGUILayout.EnumPopup(
            "チュートリアル表示タイプ", 
            stageData.tutorialDisplayType);
        if (newTutorialType != stageData.tutorialDisplayType)
        {
            Undo.RecordObject(database, "チュートリアル表示タイプを変更");
            stageData.tutorialDisplayType = newTutorialType;
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
            ResizeGrid(stageData.massStatus, gridWidth, gridHeight, true); // MassStatusは"."で埋める
            ResizeGrid(stageData.rockStatus, gridWidth, gridHeight, false); // RockStatusは空文字列
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
            ClearGrid(stageData.massStatus, true); // MassStatusは"."で埋める
            EditorUtility.SetDirty(database);
            RefreshGrid();
        }
        if (GUILayout.Button("RockStatusをクリア"))
        {
            Undo.RecordObject(database, "RockStatusをクリア");
            ClearGrid(stageData.rockStatus, false); // RockStatusは空文字列
            EditorUtility.SetDirty(database);
            RefreshGrid();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // RSItemの編集
        EditorGUILayout.LabelField("RSItem", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("i個目のアイテムのサイズはH_i*W_iで指定されます", MessageType.Info);

        // アイテム数の設定
        // シンプルなIntFieldではなく、スクロールビュー内での追加削除UIにしたほうが良いかもしれないが、
        // 既存のコードを尊重してIntFieldのままでも機能はする。
        int itemCount = EditorGUILayout.IntField("アイテム数", stageData.RSItems.Count);
        if (itemCount != stageData.RSItems.Count)
        {
            Undo.RecordObject(database, "RSItem数を変更");
            while (stageData.RSItems.Count < itemCount)
            {
                stageData.RSItems.Add(new StageDatabase.RSItemData());
            }
            while (stageData.RSItems.Count > itemCount)
            {
                stageData.RSItems.RemoveAt(stageData.RSItems.Count - 1);
            }
            EditorUtility.SetDirty(database);
            RefreshItemGenerator();
        }

        EditorGUILayout.Space();

        // 各アイテムのサイズを編集
        for (int i = 0; i < stageData.RSItems.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"アイテム {i}:", GUILayout.Width(80));

            StageDatabase.RSItemData item = stageData.RSItems[i];
            if (item == null)
            {
                item = new StageDatabase.RSItemData();
                stageData.RSItems[i] = item;
            }

            // H<数字入力>×W<数字入力>の形式で表示
            EditorGUILayout.LabelField("H", GUILayout.Width(15));
            int newHeight = EditorGUILayout.IntField(item.height, GUILayout.Width(50));
            EditorGUILayout.LabelField("×", GUILayout.Width(20));
            EditorGUILayout.LabelField("W", GUILayout.Width(15));
            int newWidth = EditorGUILayout.IntField(item.width, GUILayout.Width(50));

            newHeight = Mathf.Max(1, newHeight);
            newWidth = Mathf.Max(1, newWidth);

            bool sizeChanged = newHeight != item.height || newWidth != item.width;

            if (sizeChanged)
            {
                Undo.RecordObject(database, $"RSItem[{i}]を変更");
                item.height = newHeight;
                item.width = newWidth;
                EditorUtility.SetDirty(database);
                RefreshItemGenerator();
            }

            EditorGUILayout.EndHorizontal();

            // タイプの選択（ラジオボタン）
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("タイプ:", GUILayout.Width(80));
            
            StageDatabase.RSItemType newType = item.type;
            bool typeChanged = false;
            
            // Normalラジオボタン
            bool isNormal = EditorGUILayout.Toggle(newType == StageDatabase.RSItemType.Normal, GUILayout.Width(20));
            EditorGUILayout.LabelField("Normal", GUILayout.Width(60));
            
            // Pickaxeラジオボタン
            bool isPickaxe = EditorGUILayout.Toggle(newType == StageDatabase.RSItemType.Pickaxe, GUILayout.Width(20));
            EditorGUILayout.LabelField("Pickaxe", GUILayout.Width(60));
            
            // Gravityラジオボタン
            bool isGravity = EditorGUILayout.Toggle(newType == StageDatabase.RSItemType.Gravity, GUILayout.Width(20));
            EditorGUILayout.LabelField("Gravity", GUILayout.Width(60));
            
            // ラジオボタンの状態を更新
            if (isNormal && newType != StageDatabase.RSItemType.Normal)
            {
                newType = StageDatabase.RSItemType.Normal;
                typeChanged = true;
            }
            else if (isPickaxe && newType != StageDatabase.RSItemType.Pickaxe)
            {
                newType = StageDatabase.RSItemType.Pickaxe;
                typeChanged = true;
            }
            else if (isGravity && newType != StageDatabase.RSItemType.Gravity)
            {
                newType = StageDatabase.RSItemType.Gravity;
                typeChanged = true;
            }

            if (typeChanged)
            {
                Undo.RecordObject(database, $"RSItem[{i}]のタイプを変更");
                item.type = newType;
                EditorUtility.SetDirty(database);
                RefreshItemGenerator();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }
    }

    private void DrawGridEditor(StageDatabase database, List<StageDatabase.RowData> grid, int width, int height, string label)
    {
        // ResizeGridの呼び出しを削除 - これが原因でデータがリセットされていました
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

        EditorGUILayout.BeginVertical();

        // セルサイズ（正方形）
        const float cellSize = 30f;

        // 列ヘッダー
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUILayout.Width(50));
        for (int w = 0; w < width; w++)
        {
            EditorGUILayout.LabelField(w.ToString(), GUILayout.Width(cellSize), GUILayout.Height(cellSize));
        }
        EditorGUILayout.EndHorizontal();

        // グリッドの各行
        for (int h = height - 1; h >= 0; h--)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(h.ToString(), GUILayout.Width(50), GUILayout.Height(cellSize));

            // 行が存在するかチェック
            if (h < grid.Count && grid[h] != null && grid[h].columns != null)
            {
                for (int w = 0; w < width; w++)
                {
                    if (w < grid[h].columns.Count)
                    {
                        string value = grid[h].columns[w] ?? "";
                        string newValue = EditorGUILayout.TextField(value, GUILayout.Width(cellSize), GUILayout.Height(cellSize));

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
                        EditorGUILayout.TextField("", GUILayout.Width(cellSize), GUILayout.Height(cellSize));
                    }
                }
            }
            else
            {
                // 行が存在しない場合は空のセルを表示
                for (int w = 0; w < width; w++)
                {
                    EditorGUILayout.TextField("", GUILayout.Width(cellSize), GUILayout.Height(cellSize));
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

    private void ResizeGrid(List<StageDatabase.RowData> grid, int width, int height, bool fillWithDot = false)
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
            
            // 既存のセルが空文字列の場合は、fillWithDotがtrueなら"."で埋める
            for (int w = 0; w < grid[h].columns.Count; w++)
            {
                if (fillWithDot && string.IsNullOrEmpty(grid[h].columns[w]))
                {
                    grid[h].columns[w] = ".";
                }
            }
            
            // 幅が足りない場合は追加
            while (grid[h].columns.Count < width)
            {
                grid[h].columns.Add(fillWithDot ? "." : "");
            }
            while (grid[h].columns.Count > width)
            {
                grid[h].columns.RemoveAt(grid[h].columns.Count - 1);
            }
        }
    }

    private void ClearGrid(List<StageDatabase.RowData> grid, bool fillWithDot = false)
    {
        for (int h = 0; h < grid.Count; h++)
        {
            if (grid[h] != null && grid[h].columns != null)
            {
                for (int w = 0; w < grid[h].columns.Count; w++)
                {
                    grid[h].columns[w] = fillWithDot ? "." : "";
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

    private void RefreshItemGenerator()
    {
        StickyNotesGenerator generator = FindFirstObjectByType<StickyNotesGenerator>();
        if (generator != null)
        {
            generator.GenerateItems();
        }
    }

    private void RefreshSpriteSwitchers()
    {
        SpriteSwitcher[] switchers = FindObjectsByType<SpriteSwitcher>(FindObjectsSortMode.None);
        foreach (var switcher in switchers)
        {
            if (switcher != null)
            {
                // RefreshSprite()を使用して強制的にスプライトを再適用
                switcher.RefreshSprite();
            }
        }
    }

    private void RefreshCurrentGameStatus()
    {
        CurrentGameStatus currentGameStatus = FindFirstObjectByType<CurrentGameStatus>();
        if (currentGameStatus != null)
        {
            var stageDatabase = currentGameStatus.GetStageDatabase();
            if (stageDatabase != null)
            {
                // 範囲チェック
                if (selectedStageIndex >= 0 && selectedStageIndex < stageDatabase.GetStageCount())
                {
                    // SetCurrentStageIndexを使用してステージを更新
                    currentGameStatus.SetCurrentStageIndex(selectedStageIndex);
                    Debug.Log($"CurrentGameStatusのステージを{selectedStageIndex}に更新しました");
                    
                    // グリッドとアイテムを再生成、スプライトを更新
                    RefreshGrid();
                    RefreshItemGenerator();
                    RefreshSpriteSwitchers();
                }
            }
        }
    }
}

