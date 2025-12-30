using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// RSP（ピッケル）の挙動を制御するスクリプト
/// 右クリックで範囲内のRockを破壊します
/// </summary>
public class RSPBehavior : MonoBehaviour
{
    private Camera mainCamera;
    private CurrentGameStatus currentGameStatus;
    private GridGenerator gridGenerator;

    private int gridWidth = 0;
    private int gridHeight = 0;
    private Vector3 gridParentPosition;
    private Vector3 gridOffset;
    private float gridScale = 1.0f;

    // コピーされたRockパターン（中心からのオフセット）
    private readonly List<RSHelper.CopiedRockData> copiedOffsets = new List<RSHelper.CopiedRockData>();
    private readonly List<RSHelper.CopiedRockData> rotatedOffsets = new List<RSHelper.CopiedRockData>();

    private Vector2Int copiedSize = Vector2Int.one; // コピーした領域のサイズ (W, H)
    private bool hasCopy = false;
    private int rotationIndex = 0; // 0,1,2,3 = 0,90,180,270
    private Vector3 initialScale;
    private Vector3 dragOffset = Vector3.zero; // 回転時の位置ずれを吸収するためのマウスとのオフセット
    private bool shouldFollowMouse = false; // マウス追跡を開始するかどうか

    // プレビュー用
    private GameObject rockPreviewPrefab;
    private readonly List<GameObject> previewObjects = new List<GameObject>();
    private bool previewDirty = true;
    private int lastPreviewCenterX = int.MinValue;
    private int lastPreviewCenterY = int.MinValue;
    private int lastPreviewRotationIndex = -1;
    private bool lastCanPaste = true;

    // アニメーション中のRock破壊アニメーターを追跡
    private readonly List<RockDestroyAnimator> animatingDestroyAnimators = new List<RockDestroyAnimator>();

    // プレビュー用の透明度
    [Tooltip("コピーしたもの（プレビュー）の透明度を指定します（0.0～1.0）")]
    [SerializeField] [Range(0f, 1f)] private float previewAlpha = 0.5f;

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

    // UIテキスト管理用ヘルパー
    private RSInputUIHelper uiHelper = new RSInputUIHelper();

    [Header("UI Text Settings")]
    [Tooltip("左クリックテキスト")]
    [SerializeField] private string leftClickText = "削除";
    
    [Tooltip("右クリックテキスト")]
    [SerializeField] private string rightClickText = "キャンセル";
    
    [Tooltip("マウスホイールテキスト")]
    [SerializeField] private string mouseWheelText = "回転";

    [Header("Character Animator")]
    [Tooltip("CharacterAnimatorコンポーネントをアサインします（自動検索も可能）")]
    [SerializeField] private CharacterAnimator characterAnimator;


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

        // 各Selectionの初期スケールを保存（PositionToGridCenter()より前に実行する必要がある）
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

        // RSPをグリッドの中央に配置（SetupSelectionCorners()も内部で呼ばれる）
        PositionToGridCenter();

        // グリッドにスナップ（確実にスナップするため追加処理）
        SnapToGrid();

        // UIテキスト要素を取得
        uiHelper.FindUIElements();
        
        // 初期テキストを設定
        UpdateUITexts();

        // CharacterAnimatorを自動検索（アサインされていない場合）
        if (characterAnimator == null)
        {
            characterAnimator = CharacterAnimator.Instance;
        }
    }

    /// <summary>
    /// 四隅のSelectionオブジェクトをRSPの四隅に配置します
    /// </summary>
    private void SetupSelectionCorners()
    {
        // RSPのサイズを取得
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

        // SelectionのサイズをRSPのサイズの逆数に設定
        UpdateSelectionSizes();
    }

    /// <summary>
    /// Selectionオブジェクトのサイズを元の大きさ×RSPのサイズの逆数に設定します
    /// </summary>
    private void UpdateSelectionSizes()
    {
        // RSPの現在のサイズを取得
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

        // scaleを更新
        Transform massParent = null;
        if (gridGenerator != null)
        {
            FieldInfo massParentField = typeof(GridGenerator).GetField("massParent", BindingFlags.NonPublic | BindingFlags.Instance);
            if (massParentField != null)
            {
                Transform t = (Transform)massParentField.GetValue(gridGenerator);
                massParent = t != null ? t : gridGenerator.transform;
            }
        }
        if (massParent != null) 
        { 
            gridScale = massParent.lossyScale.x; 
        }
    }

    /// <summary>
    /// グリッドにスナップします
    /// </summary>
    private void SnapToGrid()
    {
        if (gridWidth == 0 || gridHeight == 0)
        {
            return;
        }

        // RSPのサイズを取得
        Vector3 selectorScale = transform.localScale;
        int selectorW = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(selectorScale.x)));
        int selectorH = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(selectorScale.y)));

        // 現在位置をグリッドにスナップ
        Vector3 snappedPos = RSGridHelper.SnapToGrid(transform.position, gridParentPosition, gridOffset, selectorW, selectorH, gridScale);

        // グリッド範囲内に収まるようにクランプ
        transform.position = RSGridHelper.ClampToGrid(snappedPos, gridParentPosition, gridOffset, gridWidth, gridHeight, selectorW, selectorH, gridScale);

        // Selectionの位置も更新
        SetupSelectionCorners();
    }

    /// <summary>
    /// RSPをグリッドの中央に配置します
    /// </summary>
    private void PositionToGridCenter()
    {
        if (gridWidth == 0 || gridHeight == 0)
        {
            return;
        }

        // RSPのサイズを取得
        Vector3 selectorScale = transform.localScale;
        int selectorW = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(selectorScale.x)));
        int selectorH = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(selectorScale.y)));

        // グリッドの中央セルインデックスを計算
        float centerX = (gridWidth - 1) * 0.5f;
        float centerY = (gridHeight - 1) * 0.5f;

        // 中央位置をワールド座標に変換
        Vector3 centerWorldPos = RSGridHelper.GridIndexToWorld(
            Mathf.RoundToInt(centerX), 
            Mathf.RoundToInt(centerY), 
            transform.position.z, 
            gridParentPosition, 
            gridOffset);

        // RSPのサイズを考慮してスナップ
        Vector3 snappedPos = RSGridHelper.SnapToGrid(centerWorldPos, gridParentPosition, gridOffset, selectorW, selectorH);

        // グリッド範囲内に収まるようにクランプ
        transform.position = RSGridHelper.ClampToGrid(snappedPos, gridParentPosition, gridOffset, gridWidth, gridHeight, selectorW, selectorH);

        // Selectionの位置も更新
        SetupSelectionCorners();
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
        mouseScreenPosition.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        mouseWorldPosition.z = transform.position.z;

        // マウスがグリッド内にあるかチェック
        Vector2Int mouseGridIndex = RSGridHelper.WorldToGridIndex(mouseWorldPosition, gridParentPosition, gridOffset);
        bool isMouseInGrid = mouseGridIndex.x >= 0 && mouseGridIndex.x < gridWidth && 
                            mouseGridIndex.y >= 0 && mouseGridIndex.y < gridHeight;

        // マウスがグリッド内に入ったら追跡を開始
        if (isMouseInGrid && !shouldFollowMouse)
        {
            shouldFollowMouse = true;
        }

        // マウス追跡が開始されていない場合は処理をスキップ
        if (!shouldFollowMouse)
        {
            return;
        }

        // グリッドにスナップ（ドラッグオフセットを加味）
        Vector3 snapperTarget = mouseWorldPosition + dragOffset;
        
        // RSPのサイズを取得
        Vector3 selectorScale = transform.localScale;
        int selectorW = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(selectorScale.x)));
        int selectorH = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(selectorScale.y)));
        
        Vector3 snappedPosition = RSGridHelper.SnapToGrid(snapperTarget, gridParentPosition, gridOffset, selectorW, selectorH, gridScale);

        // グリッド範囲内に収まるようにクランプ
        transform.position = RSGridHelper.ClampToGrid(snappedPosition, gridParentPosition, gridOffset, gridWidth, gridHeight, selectorW, selectorH, gridScale);

        // 選択矩形をデバッグ更新（コピー状態に依存せず常に）
        UpdateSelectionBoundsFromTransform();

        // コピー中であれば、現在位置でプレビューとバリデーションを更新
        if (hasCopy)
        {
            UpdatePreviewAndValidity();
        }
    }

    /// <summary>
    /// 入力処理（左クリックでRock削除、右クリックで選択キャンセル、ホイール回転）
    /// </summary>
    private void HandleInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // 左クリック (範囲内のRockを削除)
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Debug.Log("左クリック：範囲内のRockを削除します");
            DestroyRocksInRange();
        }

        // 右クリック (選択キャンセル)
        if (mouse.rightButton.wasPressedThisFrame)
        {
            Debug.Log("右クリック：選択をキャンセルします");
            // アニメーションをIdleに設定
            if (characterAnimator != null)
            {
                characterAnimator.SetIdle();
            }
            CancelSelection();
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

            // 回転時のSEを再生
            if (rotationStep != 0)
            {
                RSHelper.PlayTurnSound();
            }

            Debug.Log($"マウスホイール：現在の回転インデックス={rotationIndex}");
            // RSP本体の見た目の向きも変更
            UpdateSelectorRotation();

            // 回転後に位置をスナップ
            if (rotationStep != 0 && gridWidth > 0 && gridHeight > 0)
            {
                // 現在の位置をdragOffsetを考慮してスナップ
                Vector3 currentPos = transform.position;
                Vector3 targetPos = currentPos + dragOffset;
                
                // RSPのサイズを取得
                Vector3 selectorScale = transform.localScale;
                int selectorW = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(selectorScale.x)));
                int selectorH = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(selectorScale.y)));
                
                // グリッドにスナップ
                Vector3 snappedPos = RSGridHelper.SnapToGrid(targetPos, gridParentPosition, gridOffset, selectorW, selectorH, gridScale);
                
                // グリッド範囲内に収まるようにクランプ
                transform.position = RSGridHelper.ClampToGrid(snappedPos, gridParentPosition, gridOffset, gridWidth, gridHeight, selectorW, selectorH, gridScale);
                
                // Selectionの位置も更新
                SetupSelectionCorners();
            }

            previewDirty = true;
            UpdatePreviewAndValidity();
            UpdateUITexts();
        }
    }

    /// <summary>
    /// UIテキストを更新します
    /// </summary>
    private void UpdateUITexts()
    {
        uiHelper.UpdateLeftClickText(leftClickText);
        uiHelper.UpdateRightClickText(rightClickText);
        uiHelper.UpdateMouseWheelText(mouseWheelText);
    }

    /// <summary>
    /// 範囲内のRockを破壊します（RSP特有の処理）
    /// </summary>
    private void DestroyRocksInRange()
    {
        UpdateGridInfo();
        if (gridWidth == 0 || gridHeight == 0)
        {
            Debug.LogWarning("グリッド情報が正しく取得できていません");
            return;
        }

        // RSPのサイズ（セル数）を取得
        Vector3 scale = transform.localScale;
        int selWidth = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(scale.x)));
        int selHeight = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(scale.y)));

        // RSPの中心セルを計算
        Vector2 centerFloat = RSGridHelper.WorldToGridCenter(transform.position, gridParentPosition, gridOffset, gridScale);
        var (minX, minY, maxX, maxY) = RSGridHelper.CalculateSelectionBounds(centerFloat.x, centerFloat.y, selWidth, selHeight);

        Debug.Log($"破壊範囲: ({minX}, {minY}) ～ ({maxX}, {maxY})");

        // グリッド範囲外ならスキップ
        if (minX < 0 || minY < 0 || maxX >= gridWidth || maxY >= gridHeight)
        {
            Debug.LogWarning("破壊範囲がグリッド外です");
            return;
        }

        // 1. Undo用にスナップショットを記録
        if (UndoRedoManager.Instance != null)
        {
            UndoRedoManager.Instance.RecordSnapshot();
        }

        // 2. ステージデータを取得
        StageDatabase.StageData stageData = GetStageData();
        if (stageData == null)
        {
            Debug.LogWarning("ステージデータを取得できませんでした");
            return;
        }

        // --- アニメーション開始処理 ---
        // Scene上のRockを取得して、範囲内のものをアニメーションさせる
        animatingDestroyAnimators.Clear();
        GameObject[] rocks = GameObject.FindGameObjectsWithTag("Rock");
        foreach (GameObject rock in rocks)
        {
            if (rock == null) continue;

            // Rockの位置をグリッドインデックスに変換
            Vector2Int rockGridIndex = RSGridHelper.WorldToGridIndex(rock.transform.position, gridParentPosition, gridOffset, gridScale);

            // 範囲内にあるかチェック
            if (rockGridIndex.x >= minX && rockGridIndex.x <= maxX &&
                rockGridIndex.y >= minY && rockGridIndex.y <= maxY)
            {
                // アニメーションコンポーネントを取得
                var animator = rock.GetComponent<RockDestroyAnimator>();
                if (animator != null)
                {
                    // グリッド再生成で消されないように親から切り離す
                    rock.transform.SetParent(null);
                    
                    // アニメーション完了時のコールバックを設定
                    animator.OnDestroyAnimationComplete += () => OnRockDestroyAnimationComplete(animator);
                    animatingDestroyAnimators.Add(animator);
                    
                    // アニメーション開始（アニメーション完了後に自身をDestroyする）
                    animator.StartDestroyAnimation();
                }
            }
        }
        
        // アニメーション中のRockがない場合は即座にCharacterAnimationを呼び出す
        if (animatingDestroyAnimators.Count == 0)
        {
            CheckAndTransitionCharacterAnimation();
        }
        // ---------------------------

        // 3. ヘルパー関数でRockをデータから削除
        int destroyedCount = RSHelper.DestroyRocksInRange(
            stageData,
            minX, minY, maxX, maxY,
            gridParentPosition, gridOffset);

        Debug.Log($"{destroyedCount}個のRockを削除しました");

        // 4. グリッドを再生成して見た目を更新（アニメーション中のRockは切り離されているので残る）
        if (gridGenerator != null)
        {
            gridGenerator.GenerateGrid();
        }

        // 4.5. GridMonitorに通知してProgressを再計算
        GridMonitor gridMonitor = FindFirstObjectByType<GridMonitor>();
        if (gridMonitor != null)
        {
            gridMonitor.RecalculateProgress();
        }

        // 5. 使用したアイテムをデータから削除
        if (sourceItem != null)
        {
            int itemIndex = sourceItem.GetItemIndex();
            if (itemIndex >= 0 && itemIndex < stageData.RSItems.Count)
            {
                stageData.RSItems.RemoveAt(itemIndex);
                Debug.Log($"使用したアイテム (index {itemIndex}) をデータから削除しました");
            }
        }

        // 6. アイテムリストを再生成（消費されたアイテムを消すため）
        var itemGen = Object.FindFirstObjectByType<StickyNotesGenerator>();
        if (itemGen != null)
        {
            itemGen.GenerateItems();
        }

        // 7. RSPとアイテムを削除
        DestroySelectorAndItem();
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

        // RSPのサイズ（セル数）を取得
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

        // RSPの中心がどのセルかを計算
        Vector2Int centerIndex = RSGridHelper.WorldToGridIndex(transform.position, gridParentPosition, gridOffset, gridScale);
        int centerX = centerIndex.x;
        int centerY = centerIndex.y;

        Vector2 centerFloat = RSGridHelper.WorldToGridCenter(transform.position, gridParentPosition, gridOffset, gridScale);
        Debug.Log($"コピー開始: RSP位置({transform.position.x}, {transform.position.y}), 中心セル({centerX}, {centerY}), サイズ({selWidth}, {selHeight})");

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
            return;
        }

        // 現在の中心セル
        Vector2Int centerIndex = RSGridHelper.WorldToGridIndex(transform.position, gridParentPosition, gridOffset, gridScale);
        int centerX = centerIndex.x;
        int centerY = centerIndex.y;

        // 変化がなければプレビュー再生成をスキップ
        if (!previewDirty && centerX == lastPreviewCenterX && centerY == lastPreviewCenterY && rotationIndex == lastPreviewRotationIndex)
        {
            return;
        }

        ClearPreviewChildren();

        // プレビューと同時に有効性チェック
        bool canPaste = true;

        StageDatabase.StageData stageData = GetStageData();
        if (stageData == null)
        {
            return;
        }

        // Mass/Rock配列の存在チェック
        List<StageDatabase.RowData> massStatus = stageData.massStatus;
        List<StageDatabase.RowData> rockStatus = stageData.rockStatus;

        if (massStatus == null || rockStatus == null)
        {
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
                Vector3 worldPreviewPos = RSGridHelper.GridIndexToWorld(gx, gy, transform.position.z, gridParentPosition, gridOffset, gridScale);
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
                Debug.Log($"使用したアイテム (index {itemIndex}) をデータから削除しました");
            }
            else
            {
                Debug.LogWarning($"アイテムインデックス {itemIndex} が無効または範囲外です。アイテムは削除されませんでした");
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

        // GridMonitorに通知してProgressを再計算
        GridMonitor gridMonitor = FindFirstObjectByType<GridMonitor>();
        if (gridMonitor != null)
        {
            gridMonitor.RecalculateProgress();
        }

        // アイテムリストを再生成（消費されたアイテムを消すため）
        var itemGen = Object.FindFirstObjectByType<StickyNotesGenerator>();
        if (itemGen != null)
        {
            itemGen.GenerateItems();
        }

        // プレビュー更新 (貼り付け後はSelector消えるので不要だが一応)
        UpdatePreviewAndValidity();

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
    /// RSP本体の回転（見た目）をrotationIndexに合わせて更新します
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
        Vector2 centerFloat = RSGridHelper.WorldToGridCenter(transform.position, gridParentPosition, gridOffset, gridScale);
        
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
            mouseWorldPos,
            gridScale);
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

        Debug.LogWarning("ステージデータを取得できませんでした（RSPBehavior）");
        return null;
    }
    

    // アイテム参照（StickyNoteBehaviorから設定される）
    private StickyNoteBehavior sourceItem;

    public void SetSourceItem(StickyNoteBehavior item)
    {
        sourceItem = item;
        if (sourceItem != null)
        {
            sourceItem.SetAlpha(0.3f); // 選択中は薄くする
        }
    }

    /// <summary>
    /// RSPの選択をキャンセルし、自身を削除します
    /// （外部からも呼べるようにラップメソッドを公開）
    /// </summary>
    public void CancelSelection()
    {
        // キャンセル時のSEを再生
        RSHelper.PlayCancelSound();
        DestroySelector();
    }

    private void DestroySelector()
    {
        if (sourceItem != null)
        {
            sourceItem.SetAlpha(1.0f); // 削除時は元に戻す
            StickyNoteBehavior.ClearCurrentSelection(sourceItem); // キャンセル時に再選択可能にする
        }
        Destroy(gameObject);
    }

    private void DestroySelectorAndItem()
    {
        if (sourceItem != null)
        {
            // 選択を解除（アイテム削除前に実行）
            StickyNoteBehavior.ClearCurrentSelection(sourceItem);
            
            // アイテムもろとも削除
            Destroy(sourceItem.gameObject);
        }
        
        // 残っているすべてのStickyNoteBehaviorのUIを更新
        StickyNoteBehavior.UpdateAllStickyNotesUI();
        
        Destroy(gameObject);
    }

    /// <summary>
    /// 岩破壊アニメーション完了時のコールバック
    /// </summary>
    private void OnRockDestroyAnimationComplete(RockDestroyAnimator animator)
    {
        if (animatingDestroyAnimators != null && animatingDestroyAnimators.Contains(animator))
        {
            animatingDestroyAnimators.Remove(animator);
        }
        
        // すべてのアニメーションが完了したらCharacterAnimationを呼び出す
        if (animatingDestroyAnimators == null || animatingDestroyAnimators.Count == 0)
        {
            CheckAndTransitionCharacterAnimation();
        }
    }

    /// <summary>
    /// クリア条件を確認してCharacterAnimationを適切な状態に遷移します
    /// </summary>
    private void CheckAndTransitionCharacterAnimation()
    {
        if (characterAnimator != null)
        {
            // GridMonitorでクリア条件を確認
            GridMonitor gridMonitor = Object.FindFirstObjectByType<GridMonitor>();
            if (gridMonitor != null && gridMonitor.IsClearConditionMet())
            {
                characterAnimator.SetClear();
            }
            else
            {
                characterAnimator.SetIdle();
            }
        }
    }
}
