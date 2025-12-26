using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// RSG（重力）の挙動を制御するスクリプト
/// 左クリックで範囲内のRockを回転方向に応じて詰めます
/// </summary>
public class RSGBehavior : MonoBehaviour
{
    private Camera mainCamera;
    private CurrentGameStatus currentGameStatus;
    private GridGenerator gridGenerator;

    private int gridWidth = 0;
    private int gridHeight = 0;
    private Vector3 gridParentPosition;
    private Vector3 gridOffset;
    private float gridScale = 1.0f;

    private Vector3 initialScale;
    private bool shouldFollowMouse = false; // マウス追跡を開始するかどうか
    private int rotationIndex = 0; // 0,1,2,3 = 0,90,180,270
    private Vector3 dragOffset = Vector3.zero; // 回転時の位置ずれを吸収するためのマウスとのオフセット

    [Header("Animation Settings")]
    [Tooltip("Rockの移動アニメーションの時間（秒）")]
    [SerializeField] private float rockMoveDuration = 0.5f;

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

    [Tooltip("重力方向を示す矢印オブジェクトをアサインします")]
    [SerializeField] private GameObject gravityArrow;

    // 各Selectionの初期スケール（元の大きさ）
    private Vector3 initialScaleLT = Vector3.one;
    private Vector3 initialScaleRT = Vector3.one;
    private Vector3 initialScaleLB = Vector3.one;
    private Vector3 initialScaleRB = Vector3.one;
    private Vector3 initialScaleGravityArrow = Vector3.one;

    // UIテキスト管理用ヘルパー
    private RSInputUIHelper uiHelper = new RSInputUIHelper();

    [Header("UI Text Settings")]
    [Tooltip("左クリックテキスト（上方向）")]
    [SerializeField] private string leftClickTextUp = "上に詰める";
    
    [Tooltip("左クリックテキスト（右方向）")]
    [SerializeField] private string leftClickTextRight = "右に詰める";
    
    [Tooltip("左クリックテキスト（下方向）")]
    [SerializeField] private string leftClickTextDown = "下に詰める";
    
    [Tooltip("左クリックテキスト（左方向）")]
    [SerializeField] private string leftClickTextLeft = "左に詰める";
    
    [Tooltip("右クリックテキスト")]
    [SerializeField] private string rightClickText = "キャンセル";
    
    [Tooltip("マウスホイールテキスト")]
    [SerializeField] private string mouseWheelText = "回転";

    [Header("Character Animator")]
    [Tooltip("CharacterAnimatorコンポーネントをアサインします（自動検索も可能）")]
    [SerializeField] private CharacterAnimator characterAnimator;

    [Header("Animation Settings")]
    [Tooltip("左クリック後のIdle遷移までの待機時間（秒）")]
    [SerializeField] private float idleTransitionDelay = 0.3f;

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
        if (gravityArrow != null)
        {
            initialScaleGravityArrow = gravityArrow.transform.localScale;
        }

        // RSGをグリッドの中央に配置（SetupSelectionCorners()も内部で呼ばれる）
        PositionToGridCenter();

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
    /// 四隅のSelectionオブジェクトをRSGの四隅に配置します
    /// </summary>
    private void SetupSelectionCorners()
    {
        // RSGのサイズを取得
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

        // SelectionのサイズをRSGのサイズの逆数に設定
        UpdateSelectionSizes();

        // GravityArrowをRSGの中心に配置
        if (gravityArrow != null)
        {
            gravityArrow.transform.position = center;
        }
    }

    /// <summary>
    /// Selectionオブジェクトのサイズを元の大きさ×RSGのサイズの逆数に設定します
    /// </summary>
    private void UpdateSelectionSizes()
    {
        // RSGの現在のサイズを取得
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

        // GravityArrowのサイズも更新（RSGのサイズの逆数）
        // 回転インデックスが1または3の時（90度または270度）、xとyを入れ替える
        if (gravityArrow != null)
        {
            int rot = ((rotationIndex % 4) + 4) % 4;
            float gravityInvX = invX;
            float gravityInvY = invY;
            
            if (rot == 1 || rot == 3)
            {
                // 90度または270度の時、xとyを入れ替え
                gravityInvX = invY;
                gravityInvY = invX;
            }
            
            gravityArrow.transform.localScale = new Vector3(initialScaleGravityArrow.x * gravityInvX, initialScaleGravityArrow.y * gravityInvY, initialScaleGravityArrow.z);
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
    /// RSGをグリッドの中央に配置します
    /// </summary>
    private void PositionToGridCenter()
    {
        if (gridWidth == 0 || gridHeight == 0)
        {
            return;
        }

        // RSGのサイズを取得
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

        // RSGのサイズを考慮してスナップ
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
        
        // RSGのサイズを取得（回転を考慮）
        Vector3 selectorScale = transform.localScale;
        int selectorW = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(selectorScale.x)));
        int selectorH = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(selectorScale.y)));
        
        Vector3 snappedPosition = RSGridHelper.SnapToGrid(snapperTarget, gridParentPosition, gridOffset, selectorW, selectorH, gridScale);

        // グリッド範囲内に収まるようにクランプ
        transform.position = RSGridHelper.ClampToGrid(snappedPosition, gridParentPosition, gridOffset, gridWidth, gridHeight, selectorW, selectorH, gridScale);

        // 選択矩形をデバッグ更新
        UpdateSelectionBoundsFromTransform();
    }

    /// <summary>
    /// 入力処理（左クリックでRockを下に詰める、右クリックで選択キャンセル、ホイール回転）
    /// </summary>
    private void HandleInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // 左クリック (範囲内のRockを回転方向に応じて詰める)
        if (mouse.leftButton.wasPressedThisFrame)
        {
            string[] directionNames = { "上", "右", "下", "左" };
            string directionName = directionNames[((rotationIndex % 4) + 4) % 4];
            Debug.Log($"左クリック：範囲内のRockを{directionName}に詰めます");
            ApplyGravityToRocks();
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
            Debug.Log($"マウスホイール：RSGを回転します（スクロール値={scroll}）");

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

            if (rotationStep != 0)
            {
                // 回転による位置ずれを計算してドラッグオフセットに加算
                Vector3 pivotShift = CalculatePivotBasedShift(rotationStep);
                dragOffset += pivotShift;
            }

            Debug.Log($"マウスホイール：現在の回転インデックス={rotationIndex}");
            // RSG本体の見た目の向きも変更
            UpdateSelectorRotation();
            UpdateUITexts();
        }
    }

    /// <summary>
    /// UIテキストを更新します
    /// </summary>
    private void UpdateUITexts()
    {
        // 回転方向に応じてテキストを選択
        string leftText = "";
        int rot = ((rotationIndex % 4) + 4) % 4;
        switch (rot)
        {
            case 0: // 上
                leftText = leftClickTextUp;
                break;
            case 1: // 右
                leftText = leftClickTextRight;
                break;
            case 2: // 下
                leftText = leftClickTextDown;
                break;
            case 3: // 左
                leftText = leftClickTextLeft;
                break;
        }
        
        uiHelper.UpdateLeftClickText(leftText);
        uiHelper.UpdateRightClickText(rightClickText);
        uiHelper.UpdateMouseWheelText(mouseWheelText);
    }

    /// <summary>
    /// 範囲内のRockを回転方向に応じて詰めます（RSG特有の処理）
    /// </summary>
    private void ApplyGravityToRocks()
    {
        UpdateGridInfo();
        if (gridWidth == 0 || gridHeight == 0)
        {
            Debug.LogWarning("グリッド情報が正しく取得できていません");
            return;
        }

        // RSGのサイズ（セル数）を取得
        Vector3 scale = transform.localScale;
        int selWidth = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(scale.x)));
        int selHeight = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(scale.y)));

        // RSGの中心セルを計算
        Vector2 centerFloat = RSGridHelper.WorldToGridCenter(transform.position, gridParentPosition, gridOffset, gridScale);
        var (minX, minY, maxX, maxY) = RSGridHelper.CalculateSelectionBounds(centerFloat.x, centerFloat.y, selWidth, selHeight);

        Debug.Log($"重力適用範囲: ({minX}, {minY}) ～ ({maxX}, {maxY})");

        // グリッド範囲外ならスキップ
        if (minX < 0 || minY < 0 || maxX >= gridWidth || maxY >= gridHeight)
        {
            Debug.LogWarning("重力適用範囲がグリッド外です");
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

        // 3. 移動情報だけを計算（データは変更しない）
        List<RSHelper.RockMoveInfo> moveInfos = new List<RSHelper.RockMoveInfo>();
        RSHelper.CalculateGravityMoves(
            stageData,
            minX, minY, maxX, maxY,
            gridParentPosition, gridOffset,
            rotationIndex,
            moveInfos);

        string[] directionNames = { "上", "右", "下", "左" };
        string directionName = directionNames[((rotationIndex % 4) + 4) % 4];
        Debug.Log($"範囲内のRockを{directionName}に詰めます（移動数: {moveInfos.Count}）");

        // 4. 岩の移動をアニメーション化（範囲情報も渡す）
        AnimateRockMoves(moveInfos, minX, minY, maxX, maxY);

        // 5. アニメーション完了後にデータを変更してグリッドを再生成
        StartCoroutine(ApplyGravityAndRegenerateGridAfterAnimation(
            stageData,
            minX, minY, maxX, maxY,
            moveInfos,
            moveInfos.Count > 0 ? rockMoveDuration : 0f));

        // 6. 使用したアイテムをデータから削除（アニメーション完了後）
        StartCoroutine(RemoveItemAfterAnimation(moveInfos.Count > 0 ? rockMoveDuration : 0f));
    }

    // アニメーション中のRockオブジェクトを追跡（破壊を防ぐため）
    private readonly List<GameObject> animatingRocks = new List<GameObject>();

    /// <summary>
    /// 岩の移動をアニメーション化します
    /// 範囲内のすべてのRockタグが付いているゲームオブジェクトを走査し、移動情報に基づいてアニメーションします
    /// </summary>
    private void AnimateRockMoves(List<RSHelper.RockMoveInfo> moveInfos, int minX, int minY, int maxX, int maxY)
    {
        if (moveInfos == null || moveInfos.Count == 0)
        {
            return;
        }

        // アニメーション中のRockリストをクリア
        animatingRocks.Clear();

        // 移動情報を辞書に変換（高速検索のため）
        Dictionary<Vector2Int, Vector2Int> moveInfoDict = new Dictionary<Vector2Int, Vector2Int>();
        foreach (var moveInfo in moveInfos)
        {
            moveInfoDict[moveInfo.fromPosition] = moveInfo.toPosition;
        }

        // シーン上のすべてのRockタグが付いているゲームオブジェクトを取得
        GameObject[] rocks = GameObject.FindGameObjectsWithTag("Rock");

        // 範囲内のすべてのRockタグが付いているオブジェクトを走査
        foreach (GameObject rock in rocks)
        {
            if (rock == null) continue;

            // Rockの位置をグリッドインデックスに変換
            Vector2Int rockGridIndex = RSGridHelper.WorldToGridIndex(rock.transform.position, gridParentPosition, gridOffset, gridScale);

            // 範囲内にあるかチェック（範囲内のすべてのRockを対象とする）
            if (rockGridIndex.x < minX || rockGridIndex.x > maxX ||
                rockGridIndex.y < minY || rockGridIndex.y > maxY)
            {
                continue; // 範囲外のRockはスキップ
            }

            // このRockが移動するかどうかを確認（移動情報に含まれている場合のみアニメーション）
            if (!moveInfoDict.ContainsKey(rockGridIndex))
            {
                continue; // 移動しないRockはスキップ
            }

            // 移動先の位置を取得
            Vector2Int toPosition = moveInfoDict[rockGridIndex];

            // 移動先の位置を計算
            Vector3 toWorldPos = RSGridHelper.GridIndexToWorld(
                toPosition.x,
                toPosition.y,
                0f,
                gridParentPosition,
                gridOffset,
                gridScale);

            // アニメーション中のRockとして登録
            animatingRocks.Add(rock);

            // アニメーション中にオブジェクトが破壊されないように、一時的に親から切り離す
            Transform originalParent = rock.transform.parent;
            rock.transform.SetParent(null);

            // DOTweenでアニメーション（完了時にリストから削除）
            // Ease.OutExpo: 最初は激しく、後半は緩やかに減速
            rock.transform.DOMove(toWorldPos, rockMoveDuration)
                .SetEase(Ease.OutExpo)
                .OnComplete(() =>
                {
                    if (rock != null)
                    {
                        if (animatingRocks.Contains(rock))
                        {
                            animatingRocks.Remove(rock);
                        }
                        // アニメーション完了後にオブジェクトを破壊（グリッド再生成時に再生成される）
                        Destroy(rock);
                    }
                })
                .OnKill(() =>
                {
                    // アニメーションが中断された場合もリストから削除
                    if (rock != null)
                    {
                        if (animatingRocks.Contains(rock))
                        {
                            animatingRocks.Remove(rock);
                        }
                        // 親を元に戻す
                        if (originalParent != null)
                        {
                            rock.transform.SetParent(originalParent);
                        }
                    }
                });
        }
    }

    /// <summary>
    /// アニメーション完了後にデータを変更してグリッドを再生成します
    /// </summary>
    private IEnumerator ApplyGravityAndRegenerateGridAfterAnimation(
        StageDatabase.StageData stageData,
        int minX, int minY, int maxX, int maxY,
        List<RSHelper.RockMoveInfo> moveInfos,
        float delay)
    {
        // アニメーション完了まで待機
        yield return new WaitForSeconds(delay);

        // アニメーション中のRockオブジェクトを全てクリア（アニメーション完了済み）
        animatingRocks.Clear();

        // データを変更（移動情報に基づいてRockを移動）
        if (stageData != null && moveInfos != null && moveInfos.Count > 0)
        {
            RSHelper.ApplyGravityMoves(stageData, moveInfos);
        }

        // グリッドを再生成
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

        // 重力アニメーション完了後にCharacterAnimationを呼び出す
        CheckAndTransitionCharacterAnimation();
    }

    /// <summary>
    /// アニメーション完了後にアイテムを削除します
    /// </summary>
    private IEnumerator RemoveItemAfterAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);

        StageDatabase.StageData stageData = GetStageData();
        if (stageData == null)
        {
            DestroySelectorAndItem();
            yield break;
        }

        // 使用したアイテムをデータから削除
        if (sourceItem != null)
        {
            int itemIndex = sourceItem.GetItemIndex();
            if (itemIndex >= 0 && itemIndex < stageData.RSItems.Count)
            {
                stageData.RSItems.RemoveAt(itemIndex);
                Debug.Log($"使用したアイテム (index {itemIndex}) をデータから削除しました");
            }
        }

        // アイテムリストを再生成（消費されたアイテムを消すため）
        var itemGen = Object.FindFirstObjectByType<StickyNotesGenerator>();
        if (itemGen != null)
        {
            itemGen.GenerateItems();
        }

        // RSGとアイテムを削除
        DestroySelectorAndItem();
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
    /// RSG本体の回転（見た目）をrotationIndexに合わせて更新します
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

        // GravityArrowの回転を更新（回転インデックスに応じてZ軸回転）
        if (gravityArrow != null)
        {
            float rotationZ = rot * 90f; // 0°, 90°, 180°, 270°
            gravityArrow.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
        }

        // Selectionの位置とサイズを更新
        SetupSelectionCorners();
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

        // RSGのサイズを取得
        Vector3 scale = transform.localScale;
        int selWidth = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(scale.x)));
        int selHeight = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(scale.y)));

        return RSRotationHelper.CalculatePivotBasedShift(
            rotationStep,
            rotationIndex,
             new Vector2Int(selWidth, selHeight),
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

        Debug.LogWarning("ステージデータを取得できませんでした（RSGBehavior）");
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
    /// RSGの選択をキャンセルし、自身を削除します
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

