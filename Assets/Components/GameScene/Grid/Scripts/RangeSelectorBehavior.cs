using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class RangeSelectorBehavior : MonoBehaviour
{
    private Camera mainCamera;
    private CurrentGameStatus currentGameStatus;
    private GridGenerator gridGenerator;

    private int gridWidth = 0;
    private int gridHeight = 0;
    private Vector3 gridParentPosition;
    private Vector3 gridOffset;

    // コピーされたRockパターン（中心からのオフセット）
    private readonly List<RangeSelectorHelper.CopiedRockData> copiedOffsets = new List<RangeSelectorHelper.CopiedRockData>();
    private readonly List<RangeSelectorHelper.CopiedRockData> rotatedOffsets = new List<RangeSelectorHelper.CopiedRockData>();
    private bool hasCopy = false;
    private int rotationIndex = 0; // 0,1,2,3 = 0,90,180,270
    private Vector3 initialScale;

    // デバッグ表示用
    [SerializeField] private bool debugHasCopy = false;
    [SerializeField] private int debugCopiedCount = 0;
    [SerializeField] private int debugRotationIndex = 0;
    [SerializeField] private string debugStateMessage = "未コピー";
    [SerializeField, HideInInspector] private List<RangeSelectorHelper.CopiedRockData> debugSnapshotOffsets = new List<RangeSelectorHelper.CopiedRockData>();
    [SerializeField, HideInInspector] private int debugMinX = 0;
    [SerializeField, HideInInspector] private int debugMaxX = 0;
    [SerializeField, HideInInspector] private int debugMinY = 0;
    [SerializeField, HideInInspector] private int debugMaxY = 0;
    [SerializeField, HideInInspector] private int debugSelMinX = 0;
    [SerializeField, HideInInspector] private int debugSelMaxX = 0;
    [SerializeField, HideInInspector] private int debugSelMinY = 0;
    [SerializeField, HideInInspector] private int debugSelMaxY = 0;

    // プレビュー用
    private GameObject rockPreviewPrefab;
    private readonly List<GameObject> previewObjects = new List<GameObject>();
    private bool previewDirty = true;
    private int lastPreviewCenterX = int.MinValue;
    private int lastPreviewCenterY = int.MinValue;
    private int lastPreviewRotationIndex = -1;
    private bool lastCanPaste = true;

    // 色制御
    private SpriteRenderer spriteRenderer;
    private Color normalColor = Color.white;
    [SerializeField] private Color invalidColor = Color.red;

    private void Start()
    {
        // メインカメラを取得
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = Object.FindFirstObjectByType<Camera>();
        }

        // 参照系を取得
        currentGameStatus = Object.FindFirstObjectByType<CurrentGameStatus>();
        gridGenerator = Object.FindFirstObjectByType<GridGenerator>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
        }

        // GridGeneratorからRockPrefabを取得（プレビューに使用）
        if (gridGenerator != null)
        {
            FieldInfo rockField = typeof(GridGenerator).GetField("rockPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rockField != null)
            {
                rockPreviewPrefab = rockField.GetValue(gridGenerator) as GameObject;
            }
        }

        // グリッド情報を取得
        UpdateGridInfo();

        // 初期スケールを保存
        initialScale = transform.localScale;
    }

    private void Update()
    {
        FollowMouseCursor();
        HandleInput();
    }

    /// <summary>
    /// グリッド情報を更新します
    /// </summary>
    private void UpdateGridInfo()
    {
        StageDatabase.StageData stageData = GetStageData();
        if (stageData != null && stageData.massStatus != null && stageData.massStatus.Count > 0)
        {
            gridHeight = stageData.massStatus.Count;
            if (stageData.massStatus[0] != null && stageData.massStatus[0].columns != null)
            {
                gridWidth = stageData.massStatus[0].columns.Count;
            }
        }

        // MassParentの位置を取得（GridGeneratorから取得）
        Transform massParent = null;
        if (gridGenerator == null)
        {
            gridGenerator = Object.FindFirstObjectByType<GridGenerator>();
        }

        if (gridGenerator != null)
        {
            FieldInfo massParentField = typeof(GridGenerator).GetField("massParent", BindingFlags.NonPublic | BindingFlags.Instance);
            if (massParentField != null)
            {
                Transform massParentTransform = (Transform)massParentField.GetValue(gridGenerator);
                massParent = massParentTransform != null ? massParentTransform : gridGenerator.transform;
            }
        }

        if (massParent != null)
        {
            gridParentPosition = massParent.position;
            // GridGeneratorと同じオフセット計算
            gridOffset = new Vector3(-(gridWidth - 1) * 0.5f, -(gridHeight - 1) * 0.5f, 0f);
        }
    }

    /// <summary>
    /// マウスカーソルを追跡し、グリッドにスナップします
    /// </summary>
    private void FollowMouseCursor()
    {
        if (mainCamera == null || gridWidth == 0 || gridHeight == 0)
        {
            return;
        }

        var mouse = Mouse.current;
        if (mouse == null) return;

        // マウス位置をワールド座標に変換
        Vector3 mouseScreenPosition = mouse.position.ReadValue();
        mouseScreenPosition.z = Mathf.Abs(mainCamera.transform.position.z); // カメラからの距離
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        mouseWorldPosition.z = transform.position.z; // Z座標は維持

        // グリッドにスナップ
        Vector3 snappedPosition = SnapToGrid(mouseWorldPosition);

        // グリッドの範囲内かチェック
        // グリッド範囲内に収まるようにクランプ
        transform.position = ClampToGrid(snappedPosition);

        // 選択矩形をデバッグ更新（コピー状態に依存せず常に）
        UpdateSelectionBoundsFromTransform();

        // コピー中であれば、現在位置でプレビューとバリデーションを更新
        if (hasCopy)
        {
            UpdatePreviewAndValidity();
        }
    }

    /// <summary>
    /// 入力処理（左クリックコピー、ホイール回転、右クリック貼り付け）
    /// </summary>
    /// <summary>
    /// 入力処理（右クリックでコピー/ペースト、左クリックでキャンセル/削除、ホイール回転）
    /// </summary>
    private void HandleInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // 左クリック (Action: Copy or Paste)
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (hasCopy)
            {
                // ペースト
                Debug.Log("左クリック：現在保持しているRockパターンを貼り付けします");
                TryPaste();
            }
            else
            {
                // コピー
                Debug.Log("左クリック：現在の範囲内のRockをコピーします");
                CopyCurrentRegion();
            }
        }

        // 右クリック (Cancel: Clear Copy or Delete Object)
        if (mouse.rightButton.wasPressedThisFrame)
        {
            if (hasCopy)
            {
                // コピー解除
                Debug.Log("右クリック：コピー状態を解除します");
                ClearCopyState();
            }
            else
            {
                // RangeSelector削除
                Debug.Log("右クリック：RangeSelectorを削除します");
                Destroy(gameObject);
            }
        }

        // ホイールで回転（90度単位）
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Debug.Log($"マウスホイール：Rockパターンを回転します（スクロール値={scroll}）");

            if (scroll > 0f)
            {
                rotationIndex = (rotationIndex + 1) % 4;
            }
            else if (scroll < 0f)
            {
                rotationIndex = (rotationIndex + 3) % 4;
            }

            Debug.Log($"マウスホイール：現在の回転インデックス={rotationIndex}");
            // RangeSelector本体の見た目の向きも変更
            UpdateSelectorRotation();

            previewDirty = true;
            UpdatePreviewAndValidity();
        }
    }

    /// <summary>
    /// コピー状態をクリアして初期状態に戻します
    /// </summary>
    private void ClearCopyState()
    {
        hasCopy = false;
        copiedOffsets.Clear();
        rotatedOffsets.Clear();
        rotationIndex = 0;
        
        // 見た目をリセット
        UpdateSelectorRotation();
        
        // プレビュー消去
        ClearPreviewChildren();
        UpdateDebugSnapshot(copiedOffsets); // 空リストで更新
        SetValidColor(true); // 通常色に戻す
        
        UpdateDebugState("未コピー", 0, 0, false);
    }

    /// <summary>
    /// 現在の範囲にあるRock状態をコピー
    /// </summary>
    private void CopyCurrentRegion()
    {
        if (currentGameStatus == null)
        {
            Debug.LogWarning("CurrentGameStatusが見つかりませんでした");
            return;
        }
        StageDatabase.StageData stageData = GetStageData();
        if (stageData == null || stageData.rockStatus == null)
        {
            Debug.LogWarning("RockStatusが設定されていません");
            return;
        }

        UpdateGridInfo();
        if (gridWidth == 0 || gridHeight == 0)
        {
            Debug.LogWarning("グリッド情報が正しく取得できていません");
            return;
        }

        // RangeSelectorのサイズ（セル数）を取得
        Vector3 scale = transform.localScale;
        initialScale = scale; // 回転用に初期スケールを保存
        int selWidth = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(scale.x)));
        int selHeight = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(scale.y)));

        // RangeSelectorの中心がどのセルかを計算
        Vector3 localPos = transform.position - gridParentPosition;
        // GridGeneratorと同じ計算式: position = parentPosition + (offsetX + w, offsetY + h)
        // 逆算: w = localPos.x - offsetX, h = localPos.y - offsetY
        float floatCenterX = localPos.x - gridOffset.x;
        float floatCenterY = localPos.y - gridOffset.y;
        
        int centerX = Mathf.FloorToInt(floatCenterX + 0.5f);
        int centerY = Mathf.FloorToInt(floatCenterY + 0.5f);

        Debug.Log($"コピー開始: RangeSelector位置({transform.position.x}, {transform.position.y}), ローカル位置({localPos.x}, {localPos.y}), オフセット({gridOffset.x}, {gridOffset.y}), 中心セル({centerX}, {centerY}), サイズ({selWidth}, {selHeight})");

        // 選択範囲の矩形（グリッドインデックス）
        float halfW = (selWidth - 1) * 0.5f;
        float halfH = (selHeight - 1) * 0.5f;
        int minX = Mathf.RoundToInt(floatCenterX - halfW);
        int maxX = Mathf.RoundToInt(floatCenterX + halfW);
        int minY = Mathf.RoundToInt(floatCenterY - halfH);
        int maxY = Mathf.RoundToInt(floatCenterY + halfH);

        Debug.Log($"コピー範囲: ({minX}, {minY}) ～ ({maxX}, {maxY})");

        // グリッド範囲外ならコピーしない
        if (minX < 0 || minY < 0 || maxX >= gridWidth || maxY >= gridHeight)
        {
            Debug.LogWarning("コピー範囲がグリッド外です");
            return;
        }

        // ヘルパー関数でRockパターンをコピー
        RangeSelectorHelper.CopyRockPattern(
            stageData,
            minX, minY, maxX, maxY,
            centerX, centerY,
            copiedOffsets);

        rotationIndex = 0;
        hasCopy = copiedOffsets.Count > 0;

        if (!hasCopy)
        {
            Debug.Log("コピー対象のRockがありませんでした");
            UpdateDebugState("コピーなし", 0, rotationIndex, false);
        }
        else
        {
            Debug.Log($"Rockパターンをコピーしました。セル数: {copiedOffsets.Count}");
            UpdateDebugState($"コピー完了: {copiedOffsets.Count}セル", copiedOffsets.Count, rotationIndex, true);
        }

        // コピー時は初期向き（rotationIndex=0）に合わせて見た目もリセット
        UpdateSelectorRotation();

        previewDirty = true;
        UpdatePreviewAndValidity();
    }

    /// <summary>
    /// 現在のコピー状態と位置に応じてプレビューとバリデーションを更新
    /// </summary>
    private void UpdatePreviewAndValidity()
    {
        if (!hasCopy)
        {
            ClearPreviewChildren();
            UpdateDebugSnapshot(rotatedOffsets);
            SetValidColor(true);
            return;
        }

        // 現在の中心セル
        Vector3 localPos = transform.position - gridParentPosition;
        float floatCenterX = localPos.x - gridOffset.x;
        float floatCenterY = localPos.y - gridOffset.y;
        int centerX = Mathf.FloorToInt(floatCenterX + 0.5f);
        int centerY = Mathf.FloorToInt(floatCenterY + 0.5f);

        // 変化がなければプレビュー再生成をスキップ
        if (!previewDirty && centerX == lastPreviewCenterX && centerY == lastPreviewCenterY && rotationIndex == lastPreviewRotationIndex)
        {
            SetValidColor(lastCanPaste);
            return;
        }

        ClearPreviewChildren();

        // プレビューと同時に有効性チェック
        bool canPaste = true;

        StageDatabase.StageData stageData = GetStageData();
        if (stageData == null)
        {
            SetValidColor(false);
            return;
        }

        // Mass/Rock配列の存在チェック
        List<StageDatabase.RowData> massStatus = stageData.massStatus;
        List<StageDatabase.RowData> rockStatus = stageData.rockStatus;

        if (massStatus == null || rockStatus == null)
        {
            SetValidColor(false);
            return;
        }

        // 回転済みオフセットをヘルパーで計算
        RangeSelectorHelper.RotateOffsets(copiedOffsets, rotationIndex, rotatedOffsets);

        Transform previewParent = transform.parent != null ? transform.parent : transform;

        foreach (var data in rotatedOffsets)
        {
            Vector2Int o = data.offset;
            int gx = centerX + o.x;
            int gy = centerY + o.y;

            // グリッド範囲外ならプレビューを表示しない
            if (gx < 0 || gy < 0 || gx >= gridWidth || gy >= gridHeight)
            {
                canPaste = false;
                continue;
            }

            // Massが存在するか（そこにマスがない場合はNG）
            if (gy >= massStatus.Count || massStatus[gy] == null || massStatus[gy].columns == null ||
                gx >= massStatus[gy].columns.Count)
            {
                canPaste = false;
            }
            else
            {
                string cellValue = massStatus[gy].columns[gx];
                char baseChar;
                List<string> keys = new List<string>(); // ダミー
                RangeSelectorHelper.ParseCell(cellValue, out baseChar, keys);

                if (baseChar != '.')
                {
                    canPaste = false;
                }
            }

            // 既にRockがある場合はNG
            if (gy < rockStatus.Count && rockStatus[gy] != null && rockStatus[gy].columns != null &&
                gx < rockStatus[gy].columns.Count)
            {
                string rv = rockStatus[gy].columns[gx];
                char baseChar;
                List<string> dummyKeys = new List<string>();
                RangeSelectorHelper.ParseCell(rv, out baseChar, dummyKeys);

                if (baseChar == '#')
                {
                    canPaste = false;
                }
            }

            // プレビュー用オブジェクトを生成（自身の兄弟として作成）
            if (rockPreviewPrefab != null)
            {
                Vector3 worldPreviewPos = GridIndexToWorld(gx, gy, transform.position.z);
                GameObject preview = Instantiate(rockPreviewPrefab, worldPreviewPos, Quaternion.identity, previewParent);
                previewObjects.Add(preview);

                var sr = preview.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = 0.5f;
                    sr.color = c;
                }

                // プレビューにもパターンを適用
                var assigner = preview.GetComponent<RockPatternAssigner>();
                if (assigner != null)
                {
                    assigner.ApplyPatterns(data.value);
                }
            }
        }

        SetValidColor(canPaste);
        lastCanPaste = canPaste;
        lastPreviewCenterX = centerX;
        lastPreviewCenterY = centerY;
        lastPreviewRotationIndex = rotationIndex;
        previewDirty = false;

        UpdateDebugSnapshot(rotatedOffsets);
    }

    /// <summary>
    /// 左クリック時に貼り付けを試みる
    /// </summary>
    private void TryPaste()
    {
        if (!hasCopy || currentGameStatus == null)
        {
            UpdateDebugState("貼り付け失敗: コピーなし", copiedOffsets.Count, rotationIndex, false);
            return;
        }

        StageDatabase.StageData stageData = GetStageData();
        if (stageData == null)
        {
            return;
        }

        List<StageDatabase.RowData> massStatus = stageData.massStatus;
        List<StageDatabase.RowData> rockStatus = stageData.rockStatus;
        if (massStatus == null || rockStatus == null)
        {
            return;
        }

        // 現在の中心セル
        Vector3 localPos = transform.position - gridParentPosition;
        float floatCenterX = localPos.x - gridOffset.x;
        float floatCenterY = localPos.y - gridOffset.y;
        int centerX = Mathf.FloorToInt(floatCenterX + 0.5f);
        int centerY = Mathf.FloorToInt(floatCenterY + 0.5f);

        // 回転済みオフセットが無ければ更新
        if (rotatedOffsets.Count == 0)
        {
            RangeSelectorHelper.RotateOffsets(copiedOffsets, rotationIndex, rotatedOffsets);
        }

        // まずはヘルパー関数で有効性チェック（オーバーラップやマス無しを確認）
        bool canPaste = RangeSelectorHelper.CanPaste(
            rotatedOffsets,
            centerX, centerY,
            gridWidth, gridHeight,
            massStatus, rockStatus);

        if (!canPaste)
        {
            Debug.Log("この位置には貼り付けできません");
            SetValidColor(false);
            UpdateDebugState("貼り付け失敗: 配置不可", copiedOffsets.Count, rotationIndex, true);
            return;
        }

        // 貼り付け処理：ヘルパー関数でRockStatusを書き換え
        RangeSelectorHelper.ApplyPaste(rotatedOffsets, centerX, centerY, rockStatus);

        Debug.Log("Rockパターンを貼り付けました");
        UpdateDebugState("貼り付け完了", copiedOffsets.Count, rotationIndex, true);
        previewDirty = true;

        // グリッドを再生成して見た目を更新
        if (gridGenerator == null)
        {
            gridGenerator = Object.FindFirstObjectByType<GridGenerator>();
        }
        if (gridGenerator != null)
        {
            gridGenerator.GenerateGrid();
        }

        // プレビュー更新
        UpdatePreviewAndValidity();
    }

    /// <summary>
    /// オフセットを90度単位で回転
    /// </summary>
    private Vector2Int RotateOffset(Vector2Int offset, int rot)
    {
        switch (rot % 4)
        {
            case 1: // 90度
                return new Vector2Int(offset.y, -offset.x);
            case 2: // 180度
                return new Vector2Int(-offset.x, -offset.y);
            case 3: // 270度
                return new Vector2Int(-offset.y, offset.x);
            default: // 0度
                return offset;
        }
    }

    /// <summary>
    /// プレビュー用の子オブジェクトを全削除
    /// </summary>
    private void ClearPreviewChildren()
    {
        for (int i = previewObjects.Count - 1; i >= 0; i--)
        {
            if (previewObjects[i] != null)
            {
                Destroy(previewObjects[i]);
            }
        }
        previewObjects.Clear();
    }

    /// <summary>
    /// 有効/無効に応じて色を変更
    /// </summary>
    private void SetValidColor(bool isValid)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.color = isValid ? normalColor : invalidColor;
    }

    /// <summary>
    /// デバッグ表示用の状態を更新します
    /// </summary>
    private void UpdateDebugState(string message, int copiedCount, int rotIndex, bool hasCopied)
    {
        debugStateMessage = message;
        debugCopiedCount = copiedCount;
        debugRotationIndex = rotIndex;
        debugHasCopy = hasCopied;
    }

    /// <summary>
    /// RangeSelector本体の回転（見た目）をrotationIndexに合わせて更新します
    /// </summary>
    /// <summary>
    /// RangeSelector本体の回転（見た目）をrotationIndexに合わせて更新します
    /// 都合上、Z回転ではなくTransformのスケール変更（XY入れ替え）で表現します
    /// </summary>
    private void UpdateSelectorRotation()
    {
        // 0,1,2,3 → 0°,90°,180°,270°
        int rot = ((rotationIndex % 4) + 4) % 4;

        // 90度または270度の場合は縦横を入れ替える
        if (rot % 2 != 0)
        {
            transform.localScale = new Vector3(initialScale.y, initialScale.x, initialScale.z);
        }
        else
        {
            transform.localScale = initialScale;
        }

        // 回転自体は常に0
        transform.rotation = Quaternion.identity;
    }

    /// <summary>
    /// 選択中の矩形（グリッド座標）を更新
    /// </summary>
    private void UpdateSelectionBoundsFromTransform()
    {
        // セレクターサイズ（セル数）
        Vector3 scale = transform.localScale;
        int selWidth = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(scale.x)));
        int selHeight = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(scale.y)));

        // 中心セル（GridGeneratorの座標系に合わせる）
        Vector3 localPos = transform.position - gridParentPosition;
        float floatCenterX = localPos.x - gridOffset.x;
        float floatCenterY = localPos.y - gridOffset.y;

        float halfW = (selWidth - 1) * 0.5f;
        float halfH = (selHeight - 1) * 0.5f;
        debugSelMinX = Mathf.RoundToInt(floatCenterX - halfW);
        debugSelMaxX = Mathf.RoundToInt(floatCenterX + halfW);
        debugSelMinY = Mathf.RoundToInt(floatCenterY - halfH);
        debugSelMaxY = Mathf.RoundToInt(floatCenterY + halfH);
    }

    /// <summary>
    /// デバッグ用に現在のコピー形状を保存します
    /// </summary>
    private void UpdateDebugSnapshot(List<RangeSelectorHelper.CopiedRockData> offsets)
    {
        debugSnapshotOffsets.Clear();
        debugSnapshotOffsets.AddRange(offsets);

        if (offsets.Count == 0)
        {
            debugMinX = debugMaxX = debugMinY = debugMaxY = 0;
            return;
        }

        debugMinX = debugMaxX = offsets[0].offset.x;
        debugMinY = debugMaxY = offsets[0].offset.y;
        foreach (var data in offsets)
        {
            Vector2Int o = data.offset;
            if (o.x < debugMinX) debugMinX = o.x;
            if (o.x > debugMaxX) debugMaxX = o.x;
            if (o.y < debugMinY) debugMinY = o.y;
            if (o.y > debugMaxY) debugMaxY = o.y;
        }
    }

    /// <summary>
    /// グリッドインデックスをワールド座標に変換します
    /// </summary>
    private Vector3 GridIndexToWorld(int gx, int gy, float z)
    {
        return gridParentPosition + new Vector3(gridOffset.x + gx, gridOffset.y + gy, z);
    }

    /// <summary>
    /// 位置をグリッドにスナップします（偶数サイズでは中心を(-0.5, -0.5)に補正）
    /// </summary>
    private Vector3 SnapToGrid(Vector3 worldPosition)
    {
        // グリッドの親位置を基準にローカル座標に変換
        Vector3 localPosition = worldPosition - gridParentPosition;

        // グリッドオフセット（左上基準への変換用）
        float ox = gridOffset.x;
        float oy = gridOffset.y;

        // グリッド座標系での位置（0, 0 がグリッド左下の中心）
        // セルの中心は 整数値 (0,0), (1,0) ... 
        
        // RangeSelectorのサイズを取得
        Vector3 selectorScale = transform.localScale;
        int selectorW = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(selectorScale.x)));
        int selectorH = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(selectorScale.y)));

        // セレクターの中心が、グリッドの整数座標(セルの中心)に来るべきか、半整数座標(セルの境界)に来るべきか
        // セレクター幅が奇数(1,3,5)なら、中心はセルの中心（整数）に合う
        // セレクター幅が偶数(2,4,6)なら、中心はセルの境界（半整数）に合う
        
        // X座標のスナップ
        float targetX;
        if (selectorW % 2 != 0)
        {
            // 奇数サイズ：整数座標にスナップ
            // localPosition.x - ox がグリッドインデックスに近い値
            float relativeX = localPosition.x - ox;
            targetX = Mathf.Round(relativeX) + ox;
        }
        else
        {
            // 偶数サイズ：半整数座標にスナップ
            float relativeX = localPosition.x - ox;
            targetX = Mathf.Floor(relativeX) + 0.5f + ox;
        }

        // Y座標のスナップ
        float targetY;
        if (selectorH % 2 != 0)
        {
            // 奇数サイズ：整数座標にスナップ
            float relativeY = localPosition.y - oy;
            targetY = Mathf.Round(relativeY) + oy;
        }
        else
        {
            // 偶数サイズ：半整数座標にスナップ
            float relativeY = localPosition.y - oy;
            targetY = Mathf.Floor(relativeY) + 0.5f + oy;
        }

        // ワールド座標に戻す
        return gridParentPosition + new Vector3(targetX, targetY, worldPosition.z);
    }

    /// <summary>
    /// グリッド範囲内に位置を制限（パディング考慮）
    /// </summary>
    private Vector3 ClampToGrid(Vector3 worldPosition)
    {
        Vector3 local = worldPosition - gridParentPosition;

        // セレクターの半サイズ
        Vector3 scale = transform.localScale;
        float halfW = Mathf.Abs(scale.x) * 0.5f;
        float halfH = Mathf.Abs(scale.y) * 0.5f;

        // グリッドの物理的な端（セルの外枠）
        // gridOffsetは(0,0)セルの中心。セル幅1なので、左端は -0.5
        float gridLeft = gridOffset.x - 0.5f;
        float gridBottom = gridOffset.y - 0.5f;
        float gridRight = gridOffset.x + gridWidth - 0.5f;
        float gridTop = gridOffset.y + gridHeight - 0.5f;

        // セレクター中心の可動範囲
        float minX = gridLeft + halfW;
        float maxX = gridRight - halfW;
        float minY = gridBottom + halfH;
        float maxY = gridTop - halfH;

        // セレクターがグリッドより大きい場合の考慮（中心に固定など）
        if (minX > maxX) minX = maxX = (gridLeft + gridRight) * 0.5f;
        if (minY > maxY) minY = maxY = (gridBottom + gridTop) * 0.5f;

        float cx = Mathf.Clamp(local.x, minX, maxX);
        float cy = Mathf.Clamp(local.y, minY, maxY);

        return gridParentPosition + new Vector3(cx, cy, worldPosition.z);
    }

    /// <summary>
    /// 現在のステージデータを取得します（ランタイムではDeepCopy済みを返す）
    /// </summary>
    private StageDatabase.StageData GetStageData()
    {
        // 1. CurrentGameStatus から取得（最優先：ランタイムDeepCopy）
        if (currentGameStatus == null)
        {
            currentGameStatus = Object.FindFirstObjectByType<CurrentGameStatus>();
        }
        if (currentGameStatus != null)
        {
            var runtimeData = currentGameStatus.GetCurrentStageData();
            if (runtimeData != null)
            {
                return runtimeData;
            }
        }

        // 2. CurrentGameStatus が持つ StageDatabase から直接取得（読み取り専用）
        if (currentGameStatus != null)
        {
            StageDatabase db = currentGameStatus.GetStageDatabase();
            if (db != null)
            {
                int idx = currentGameStatus.GetCurrentStageIndex();
                return db.GetStageData(idx);
            }
        }

        // 3. フォールバック：GridGenerator にアサインされている StageDatabase から取得
        if (gridGenerator == null)
        {
            gridGenerator = Object.FindFirstObjectByType<GridGenerator>();
        }
        if (gridGenerator != null)
        {
            FieldInfo dbField = typeof(GridGenerator).GetField("stageDatabase", BindingFlags.NonPublic | BindingFlags.Instance);
            if (dbField != null)
            {
                StageDatabase db = dbField.GetValue(gridGenerator) as StageDatabase;
                if (db != null)
                {
                    int idx = currentGameStatus != null ? currentGameStatus.GetCurrentStageIndex() : 0;
                    return db.GetStageData(idx);
                }
            }
        }

        Debug.LogWarning("ステージデータを取得できませんでした（RangeSelectorBehavior）");
        return null;
    }
}

