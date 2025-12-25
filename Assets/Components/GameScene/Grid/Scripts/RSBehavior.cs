using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class RSBehavior : MonoBehaviour
{
    private Camera mainCamera;
    private CurrentGameStatus currentGameStatus;
    private GridGenerator gridGenerator;

    private int gridWidth = 0;
    private int gridHeight = 0;
    private Vector3 gridParentPosition;
    private Vector3 gridOffset;

    // コピーされたRockパターン（中心からのオフセット）
    private readonly List<RSHelper.CopiedRockData> copiedOffsets = new List<RSHelper.CopiedRockData>();
    private readonly List<RSHelper.CopiedRockData> rotatedOffsets = new List<RSHelper.CopiedRockData>();

    private Vector2Int copiedSize = Vector2Int.one; // コピーした領域のサイズ (W, H)
    private bool hasCopy = false;
    private int rotationIndex = 0; // 0,1,2,3 = 0,90,180,270
    private Vector3 initialScale;
    private Vector3 dragOffset = Vector3.zero; // 回転時の位置ずれを吸収するためのマウスとのオフセット

    // プレビュー用
    private GameObject rockPreviewPrefab;
    private readonly List<GameObject> previewObjects = new List<GameObject>();
    private bool previewDirty = true;
    private int lastPreviewCenterX = int.MinValue;
    private int lastPreviewCenterY = int.MinValue;
    private int lastPreviewRotationIndex = -1;
    private bool lastCanPaste = true;

    // 点線表示用
    [Header("Prefabs")]
    [Tooltip("コピー時の矩形を囲む点線のPrefabをアサインします")]
    [SerializeField] private GameObject dashLinePrefab;
    private GameObject dashLineInstance;

    // 色制御
    [Header("Color")]
    [SerializeField] private Color invalidColor = Color.red;
    [Tooltip("コピーしたもの（プレビュー）の透明度を指定します（0.0～1.0）")]
    [SerializeField] [Range(0f, 1f)] private float previewAlpha = 0.5f;
    private SpriteRenderer spriteRenderer;
    private Color normalColor = Color.white;

    // 四隅のSelection
    [Header("Selection Corners")]
    [Tooltip("左上のSelectionオブジェクトをアサインします")]
    [SerializeField] private GameObject selectionLT;
    
    [Tooltip("右上のSelectionオブジェクトをアサインします")]
    [SerializeField] private GameObject selectionRT;
    
    [Tooltip("左下のSelectionオブジェクトをアサインします")]
    [SerializeField] private GameObject selectionLB;
    
    [Tooltip("右下のSelectionオブジェクトをアサインします")]
    [SerializeField] private GameObject selectionRB;

    // 各Selectionの初期スケール（元の大きさ）
    private Vector3 initialScaleLT = Vector3.one;
    private Vector3 initialScaleRT = Vector3.one;
    private Vector3 initialScaleLB = Vector3.one;
    private Vector3 initialScaleRB = Vector3.one;

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
            // 生成直後は2フレームだけ透明にする
            var transparent = normalColor;
            transparent.a = 0f;
            spriteRenderer.color = transparent;
            StartCoroutine(SetOpacityAfterFrames(2, normalColor));
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

        // 各Selectionの初期スケールを保存
        if (selectionLT != null)
        {
            initialScaleLT = selectionLT.transform.localScale;
        }
        if (selectionRT != null)
        {
            initialScaleRT = selectionRT.transform.localScale;
        }
        if (selectionLB != null)
        {
            initialScaleLB = selectionLB.transform.localScale;
        }
        if (selectionRB != null)
        {
            initialScaleRB = selectionRB.transform.localScale;
        }

        // 四隅のSelectionを配置
        SetupSelectionCorners();
    }

    /// <summary>
    /// 四隅のSelectionオブジェクトをRSの四隅に配置します
    /// </summary>
    private void SetupSelectionCorners()
    {
        // RSのサイズを取得
        Vector3 scale = transform.localScale;
        float halfWidth = Mathf.Abs(scale.x) * 0.5f;
        float halfHeight = Mathf.Abs(scale.y) * 0.5f;

        // 四隅の位置を計算（ローカル座標系）
        Vector3 center = transform.position;

        // 左上 (Left Top)
        if (selectionLT != null)
        {
            Vector3 ltPos = center + new Vector3(-halfWidth, halfHeight, 0f);
            selectionLT.transform.position = ltPos;
        }

        // 右上 (Right Top)
        if (selectionRT != null)
        {
            Vector3 rtPos = center + new Vector3(halfWidth, halfHeight, 0f);
            selectionRT.transform.position = rtPos;
        }

        // 左下 (Left Bottom)
        if (selectionLB != null)
        {
            Vector3 lbPos = center + new Vector3(-halfWidth, -halfHeight, 0f);
            selectionLB.transform.position = lbPos;
        }

        // 右下 (Right Bottom)
        if (selectionRB != null)
        {
            Vector3 rbPos = center + new Vector3(halfWidth, -halfHeight, 0f);
            selectionRB.transform.position = rbPos;
        }

        // SelectionのサイズをRSのサイズの逆数に設定
        UpdateSelectionSizes();
    }

    /// <summary>
    /// Selectionオブジェクトのサイズを元の大きさ×RSのサイズの逆数に設定します
    /// </summary>
    private void UpdateSelectionSizes()
    {
        // RSの現在のサイズを取得
        Vector3 scale = transform.localScale;
        float invX = scale.x != 0f ? 1f / Mathf.Abs(scale.x) : 1f;
        float invY = scale.y != 0f ? 1f / Mathf.Abs(scale.y) : 1f;

        // 各Selectionのサイズを更新（元の大きさ×逆数）
        if (selectionLT != null)
        {
            selectionLT.transform.localScale = new Vector3(initialScaleLT.x * invX, initialScaleLT.y * invY, initialScaleLT.z);
        }
        if (selectionRT != null)
        {
            selectionRT.transform.localScale = new Vector3(initialScaleRT.x * invX, initialScaleRT.y * invY, initialScaleRT.z);
        }
        if (selectionLB != null)
        {
            selectionLB.transform.localScale = new Vector3(initialScaleLB.x * invX, initialScaleLB.y * invY, initialScaleLB.z);
        }
        if (selectionRB != null)
        {
            selectionRB.transform.localScale = new Vector3(initialScaleRB.x * invX, initialScaleRB.y * invY, initialScaleRB.z);
        }
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
        if (gridGenerator == null)
        {
            gridGenerator = Object.FindFirstObjectByType<GridGenerator>();
        }

        StageDatabase.StageData stageData = GetStageData();
        var (width, height, parentPos, offset) = RSGridHelper.GetGridInfo(gridGenerator, stageData);
        
        gridWidth = width;
        gridHeight = height;
        gridParentPosition = parentPos;
        gridOffset = offset;
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

        // グリッドにスナップ（ドラッグオフセットを加味）
        Vector3 snapperTarget = mouseWorldPosition + dragOffset;
        
        // RSのサイズを取得
        Vector3 selectorScale = transform.localScale;
        int selectorW = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(selectorScale.x)));
        int selectorH = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(selectorScale.y)));
        
        Vector3 snappedPosition = RSGridHelper.SnapToGrid(snapperTarget, gridParentPosition, gridOffset, selectorW, selectorH);

        // グリッドの範囲内かチェック
        // グリッド範囲内に収まるようにクランプ
        transform.position = RSGridHelper.ClampToGrid(snappedPosition, gridParentPosition, gridOffset, gridWidth, gridHeight, selectorW, selectorH);

        // 選択矩形をデバッグ更新（コピー状態に依存せず常に）
        UpdateSelectionBoundsFromTransform();

        // コピー中であれば、現在位置でプレビューとバリデーションを更新
        if (hasCopy)
        {
            UpdatePreviewAndValidity();
        }
    }

    /// <summary>
    /// 入力処理（左クリックでコピー/ペースト、右クリックでキャンセル/削除、ホイール回転）
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
                // RS削除（アイテムを表示に戻す）
                Debug.Log("右クリック：RSを削除します");
                CancelSelection();
            }
        }

        // ホイールで回転（90度単位）
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Debug.Log($"マウスホイール：Rockパターンを回転します（スクロール値={scroll}）");

            int rotationStep = 0;
            if (scroll > 0f)
            {
                rotationStep = 1;
                rotationIndex = (rotationIndex + 1) % 4;
            }
            else if (scroll < 0f)
            {
                rotationStep = -1;
                rotationIndex = (rotationIndex + 3) % 4;
            }

            if (rotationStep != 0 && hasCopy)
            {
                // 回転による位置ずれを計算してドラッグオフセットに加算
                Vector3 pivotShift = CalculatePivotBasedShift(rotationStep);
                dragOffset += pivotShift;
            }

            Debug.Log($"マウスホイール：現在の回転インデックス={rotationIndex}");
            // RS本体の見た目の向きも変更
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
        
        // プレビュー消去
        ClearPreviewChildren();
        // 点線を削除
        ClearDashLine();
        SetValidColor(true); // 通常色に戻す
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

        // RSのサイズ（セル数）を取得
        Vector3 scale = transform.localScale;
        int selWidth = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(scale.x)));
        int selHeight = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(scale.y)));
        

        int canonicalW = selWidth;
        int canonicalH = selHeight;
        
        // initialScaleはFloat精度を維持する必要があるため、RoundToIntした値ではなく元のscaleから計算する
        // rotationIndexが奇数(90, 270)の場合は、現在(x,y)が入れ替わった状態なので戻す
        if (rotationIndex % 2 != 0)
        {
            canonicalW = selHeight;
            canonicalH = selWidth;
            initialScale = new Vector3(Mathf.Abs(scale.y), Mathf.Abs(scale.x), scale.z);
        }
        else
        {
            initialScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), scale.z);
        }
        
        copiedSize = new Vector2Int(canonicalW, canonicalH);
        dragOffset = Vector3.zero; // コピー時にオフセットはリセット

        // RSの中心がどのセルかを計算
        Vector2Int centerIndex = RSGridHelper.WorldToGridIndex(transform.position, gridParentPosition, gridOffset);
        int centerX = centerIndex.x;
        int centerY = centerIndex.y;

        Vector2 centerFloat = RSGridHelper.WorldToGridCenter(transform.position, gridParentPosition, gridOffset);
        Debug.Log($"コピー開始: RS位置({transform.position.x}, {transform.position.y}), 中心セル({centerX}, {centerY}), サイズ({selWidth}, {selHeight})");

        // 選択範囲の矩形（グリッドインデックス）
        var (minX, minY, maxX, maxY) = RSGridHelper.CalculateSelectionBounds(centerFloat.x, centerFloat.y, selWidth, selHeight);

        Debug.Log($"コピー範囲: ({minX}, {minY}) ～ ({maxX}, {maxY})");

        // グリッド範囲外ならコピーしない
        if (minX < 0 || minY < 0 || maxX >= gridWidth || maxY >= gridHeight)
        {
            Debug.LogWarning("コピー範囲がグリッド外です");
            return;
        }

        // ヘルパー関数でRockパターンをコピー
        RSHelper.CopyRockPattern(
            stageData,
            minX, minY, maxX, maxY,
            centerX, centerY,
            copiedOffsets);

        hasCopy = copiedOffsets.Count > 0;

        if (hasCopy && rotationIndex != 0)
        {
            // 回転状態でコピーした場合、データを「回転なし(0度)」の状態に戻して保存する
            // これにより、ペースト時に現在の回転角を適用した際に正しくなる
            
            // 逆回転のインデックス (例: 1(90) -> 3(270))
            int inverseRot = (4 - rotationIndex) % 4;
            
            List<RSHelper.CopiedRockData> unrotatedOffsets = new List<RSHelper.CopiedRockData>();
            
            // 現在の(見た目上の)サイズを渡して逆回転させる
            RSHelper.RotateOffsets(
                copiedOffsets,
                inverseRot,
                selWidth,
                selHeight,
                unrotatedOffsets);

            // 保存データを差し替え
            copiedOffsets.Clear();
            copiedOffsets.AddRange(unrotatedOffsets);

        }

        if (!hasCopy)
        {
            Debug.Log("コピー対象のRockがありませんでした");
        }
        else
        {
            Debug.Log($"Rockパターンをコピーしました。セル数: {copiedOffsets.Count}");
            
            // コピーした矩形の周囲を点線で囲む
            CreateDashLine(minX, minY, maxX, maxY);
        }

        // コピー時も現在の回転インデックスを維持したまま見た目を更新
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
            SetValidColor(true);
            return;
        }

        // 現在の中心セル
        Vector2Int centerIndex = RSGridHelper.WorldToGridIndex(transform.position, gridParentPosition, gridOffset);
        int centerX = centerIndex.x;
        int centerY = centerIndex.y;

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
        RSHelper.RotateOffsets(copiedOffsets, rotationIndex, copiedSize.x, copiedSize.y, rotatedOffsets);

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
                RSHelper.ParseCell(cellValue, out baseChar, keys);

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
                RSHelper.ParseCell(rv, out baseChar, dummyKeys);

                if (baseChar == '#')
                {
                    canPaste = false;
                }
            }

            // プレビュー用オブジェクトを生成（自身の兄弟として作成）
            if (rockPreviewPrefab != null)
            {
                Vector3 worldPreviewPos = RSGridHelper.GridIndexToWorld(gx, gy, transform.position.z, gridParentPosition, gridOffset);
                GameObject preview = Instantiate(rockPreviewPrefab, worldPreviewPos, Quaternion.identity, previewParent);
                previewObjects.Add(preview);

                var sr = preview.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = previewAlpha;
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
    }

    /// <summary>
    /// 左クリック時に貼り付けを試みる
    /// </summary>
    private void TryPaste()
    {
        if (!hasCopy || currentGameStatus == null)
        {
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
        Vector2Int centerIndex = RSGridHelper.WorldToGridIndex(transform.position, gridParentPosition, gridOffset);
        int centerX = centerIndex.x;
        int centerY = centerIndex.y;

        // 回転済みオフセットが無ければ更新
        if (rotatedOffsets.Count == 0)
        {
            RSHelper.RotateOffsets(copiedOffsets, rotationIndex, copiedSize.x, copiedSize.y, rotatedOffsets);
        }

        // まずはヘルパー関数で有効性チェック（オーバーラップやマス無しを確認）
        bool canPaste = RSHelper.CanPaste(
            rotatedOffsets,
            centerX, centerY,
            gridWidth, gridHeight,
            massStatus, rockStatus);

        if (!canPaste)
        {
            Debug.Log("この位置には貼り付けできません");
            SetValidColor(false);
            return;
        }

        // --- ここから確定処理 ---

        // 1. 直前の状態をSnapshotに記録（Undo用）
        if (UndoRedoManager.Instance != null)
        {
            UndoRedoManager.Instance.RecordSnapshot();
        }

        // 2. 貼り付け処理：ヘルパー関数でRockStatusを書き換え
        RSHelper.ApplyPaste(rotatedOffsets, centerX, centerY, rockStatus);

        // 3. 使用したアイテムをデータから削除
        if (sourceItem != null)
        {
            int itemIndex = sourceItem.GetItemIndex();
            // インデックスが有効か確認
            if (itemIndex >= 0 && itemIndex < stageData.RSItems.Count)
            {
                // リストから削除（これでアイテムが消費されたことになる）
                stageData.RSItems.RemoveAt(itemIndex);
                Debug.Log($"Used item index {itemIndex} removed from data.");
            }
            else
            {
                // インデックスがない場合は、リストの先頭を削除する、などのフォールバックが必要かもしれないが
                // ここでは安全のため何もしない（論理削除失敗）
                Debug.LogWarning($"Item index {itemIndex} is invalid or out of range. Item not removed.");
            }
        }

        Debug.Log("Rockパターンを貼り付けました");
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

        // アイテムリストを再生成（消費されたアイテムを消すため）
        var itemGen = Object.FindFirstObjectByType<StickyNotesGenerator>();
        if (itemGen != null)
        {
            itemGen.GenerateItems();
        }

        // プレビュー更新 (貼り付け後はSelector消えるので不要だが一応)
        UpdatePreviewAndValidity();

        // 点線を削除
        ClearDashLine();

        // 貼り付け成功したら削除（アイテムごと）
        DestroySelectorAndItem();
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
    /// RS本体の回転（見た目）をrotationIndexに合わせて更新します
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

        // Selectionの位置とサイズを更新
        SetupSelectionCorners();
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

        // 中心座標（GridGeneratorの座標系に合わせる）
        Vector2 centerFloat = RSGridHelper.WorldToGridCenter(transform.position, gridParentPosition, gridOffset);

        var (minX, minY, maxX, maxY) = RSGridHelper.CalculateSelectionBounds(centerFloat.x, centerFloat.y, selWidth, selHeight);
    }

    /// <summary>
    /// 回転時のピボットに基づいた位置シフト量を計算します
    /// </summary>
    private Vector3 CalculatePivotBasedShift(int rotationStep)
    {
        // マウス位置をワールド座標に変換
        Vector3 mouseWorldPos = Vector3.zero;
        if (mainCamera != null && Mouse.current != null)
        {
            Vector3 mouseScreen = Mouse.current.position.ReadValue();
            mouseScreen.z = Mathf.Abs(mainCamera.transform.position.z);
            mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreen);
            mouseWorldPos.z = transform.position.z;
        }

        return RSRotationHelper.CalculatePivotBasedShift(
            rotationStep,
            rotationIndex,
            copiedSize,
            transform.position,
            mouseWorldPos);
    }

    /// <summary>
    /// 指定フレーム待機後に不透明度を元に戻します
    /// </summary>
    private System.Collections.IEnumerator SetOpacityAfterFrames(int waitFrames, Color targetColor)
    {
        for (int i = 0; i < waitFrames; i++)
        {
            yield return new WaitForEndOfFrame();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = targetColor;
        }
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

        Debug.LogWarning("ステージデータを取得できませんでした（RSBehavior）");
        return null;
    }
    

    // アイテム参照（RSItemBehaviorから設定される）
    private RSItemBehavior sourceItem;

    public void SetSourceItem(RSItemBehavior item)
    {
        sourceItem = item;
        if (sourceItem != null)
        {
            sourceItem.SetAlpha(0.3f); // 選択中は薄くする
        }
    }

    /// <summary>
    /// RSの選択をキャンセルし、自身を削除します
    /// （外部からも呼べるようにラップメソッドを公開）
    /// </summary>
    public void CancelSelection()
    {
        DestroySelector();
    }

    private void DestroySelector()
    {
        if (sourceItem != null)
        {
            sourceItem.SetAlpha(1.0f); // 削除時は元に戻す
            RSItemBehavior.ClearCurrentSelection(sourceItem); // キャンセル時に再選択可能にする
        }
        Destroy(gameObject);
    }

    private void DestroySelectorAndItem()
    {
        // 点線を削除
        ClearDashLine();
        
        if (sourceItem != null)
        {
            // アイテムもろとも削除
            Destroy(sourceItem.gameObject);
            RSItemBehavior.ClearCurrentSelection(sourceItem); // 削除後は選択を解放
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// コピーした矩形の周囲を点線で囲むDashLineを生成します
    /// </summary>
    /// <param name="minX">矩形の最小X座標（グリッドインデックス）</param>
    /// <param name="minY">矩形の最小Y座標（グリッドインデックス）</param>
    /// <param name="maxX">矩形の最大X座標（グリッドインデックス）</param>
    /// <param name="maxY">矩形の最大Y座標（グリッドインデックス）</param>
    private void CreateDashLine(int minX, int minY, int maxX, int maxY)
    {
        if (dashLinePrefab == null)
        {
            Debug.LogWarning("DashLinePrefabがアサインされていません");
            return;
        }

        // 既存の点線を削除
        ClearDashLine();

        // DashLineを生成
        Transform parent = transform.parent != null ? transform.parent : transform;
        dashLineInstance = Instantiate(dashLinePrefab, parent);
        if (dashLineInstance == null)
        {
            Debug.LogError("DashLineの生成に失敗しました");
            return;
        }

        dashLineInstance.name = "DashLine";

        // LineRendererを取得
        LineRenderer lineRenderer = dashLineInstance.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("DashLinePrefabにLineRendererがアタッチされていません");
            return;
        }

        // 矩形の4つの角のワールド座標を計算
        // セルの中心座標から、セルの境界（矩形の外側）を計算
        Vector3 bottomLeft = RSGridHelper.GridIndexToWorld(minX, minY, transform.position.z, gridParentPosition, gridOffset) - new Vector3(0.5f, 0.5f, 0f);
        Vector3 topLeft = RSGridHelper.GridIndexToWorld(minX, maxY, transform.position.z, gridParentPosition, gridOffset) - new Vector3(0.5f, -0.5f, 0f);
        Vector3 topRight = RSGridHelper.GridIndexToWorld(maxX, maxY, transform.position.z, gridParentPosition, gridOffset) + new Vector3(0.5f, 0.5f, 0f);
        Vector3 bottomRight = RSGridHelper.GridIndexToWorld(maxX, minY, transform.position.z, gridParentPosition, gridOffset) + new Vector3(0.5f, -0.5f, 0f);

        // LineRendererで矩形を描画（5つの頂点：左下→左上→右上→右下→左下）
        lineRenderer.positionCount = 5;
        lineRenderer.SetPosition(0, bottomLeft);
        lineRenderer.SetPosition(1, topLeft);
        lineRenderer.SetPosition(2, topRight);
        lineRenderer.SetPosition(3, bottomRight);
        lineRenderer.SetPosition(4, bottomLeft); // 閉じる

        Debug.Log($"DashLineを生成しました: ({minX}, {minY}) ～ ({maxX}, {maxY})");
    }

    /// <summary>
    /// DashLineを削除します
    /// </summary>
    private void ClearDashLine()
    {
        if (dashLineInstance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(dashLineInstance);
            }
            else
            {
                DestroyImmediate(dashLineInstance);
            }
            dashLineInstance = null;
        }
    }
}

