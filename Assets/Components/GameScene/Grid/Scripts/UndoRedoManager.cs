using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UndoRedoManager : MonoBehaviour
{
    public static UndoRedoManager Instance { get; private set; }

    [Header("Sprite References")]
    [Tooltip("UndoスプライトのGameObject（SpriteRendererとCollider2Dが必要）")]
    [SerializeField] private GameObject undoSpriteObject;
    [Tooltip("RedoスプライトのGameObject（SpriteRendererとCollider2Dが必要）")]
    [SerializeField] private GameObject redoSpriteObject;

    [Header("Color Settings")]
    [Tooltip("通常時の色")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("無効時の色（灰色マスク）")]
    [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [Tooltip("押下時の色")]
    [SerializeField] private Color pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    private SpriteRenderer undoSpriteRenderer;
    private SpriteRenderer redoSpriteRenderer;
    private UndoRedoSpriteButton undoButton;
    private UndoRedoSpriteButton redoButton;

    // 履歴スタック
    private readonly Stack<StageDatabase.StageData> undoStack = new Stack<StageDatabase.StageData>();
    private readonly Stack<StageDatabase.StageData> redoStack = new Stack<StageDatabase.StageData>();

    // ボタン押下時のSE用のCuePlay（キャッシュ）
    private CuePlay redoCuePlay;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Undoスプライトの設定
        if (undoSpriteObject != null)
        {
            undoSpriteRenderer = undoSpriteObject.GetComponent<SpriteRenderer>();
            if (undoSpriteRenderer == null)
            {
                Debug.LogWarning("UndoRedoManager: UndoSpriteObjectにSpriteRendererがありません");
            }

            // Collider2Dがなければ追加
            if (undoSpriteObject.GetComponent<Collider2D>() == null)
            {
                BoxCollider2D collider = undoSpriteObject.AddComponent<BoxCollider2D>();
                if (undoSpriteRenderer != null && undoSpriteRenderer.sprite != null)
                {
                    collider.size = undoSpriteRenderer.sprite.bounds.size;
                }
            }

            // UndoRedoSpriteButtonコンポーネントを追加
            undoButton = undoSpriteObject.GetComponent<UndoRedoSpriteButton>();
            if (undoButton == null)
            {
                undoButton = undoSpriteObject.AddComponent<UndoRedoSpriteButton>();
            }
            undoButton.Initialize(this, true);
        }

        // Redoスプライトの設定
        if (redoSpriteObject != null)
        {
            redoSpriteRenderer = redoSpriteObject.GetComponent<SpriteRenderer>();
            if (redoSpriteRenderer == null)
            {
                Debug.LogWarning("UndoRedoManager: RedoSpriteObjectにSpriteRendererがありません");
            }

            // Collider2Dがなければ追加
            if (redoSpriteObject.GetComponent<Collider2D>() == null)
            {
                BoxCollider2D collider = redoSpriteObject.AddComponent<BoxCollider2D>();
                if (redoSpriteRenderer != null && redoSpriteRenderer.sprite != null)
                {
                    collider.size = redoSpriteRenderer.sprite.bounds.size;
                }
            }

            // UndoRedoSpriteButtonコンポーネントを追加
            redoButton = redoSpriteObject.GetComponent<UndoRedoSpriteButton>();
            if (redoButton == null)
            {
                redoButton = redoSpriteObject.AddComponent<UndoRedoSpriteButton>();
            }
            redoButton.Initialize(this, false);
        }

        UpdateButtons();

        // Redo(CriAtomSource)オブジェクトを検索してCuePlayを取得
        FindRedoCuePlay();
    }

    /// <summary>
    /// Redo(CriAtomSource)オブジェクトを検索してCuePlayを取得します
    /// </summary>
    private void FindRedoCuePlay()
    {
        // シーン内から"Redo(CriAtomSource)"という名前のオブジェクトを検索
        GameObject redoObj = GameObject.Find("Redo(CriAtomSource)");
        if (redoObj != null)
        {
            redoCuePlay = redoObj.GetComponent<CuePlay>();
            if (redoCuePlay == null)
            {
                Debug.LogWarning("UndoRedoManager: Redo(CriAtomSource)にCuePlayコンポーネントが見つかりませんでした");
            }
        }
        else
        {
            Debug.LogWarning("UndoRedoManager: Redo(CriAtomSource)オブジェクトが見つかりませんでした");
        }
    }

    /// <summary>
    /// 現在の状態をUndoスタックに記録します。
    /// （操作を行う直前に呼び出してください）
    /// </summary>
    public void RecordSnapshot()
    {
        var currentStatus = Object.FindFirstObjectByType<CurrentGameStatus>();
        if (currentStatus == null) return;

        var currentData = currentStatus.GetCurrentStageData();
        if (currentData != null)
        {
            // 現在の状態を複製してUndoスタックへ
            undoStack.Push(currentData.DeepCopy());
            
            // 新しい操作が行われたのでRedoスタックはクリア
            redoStack.Clear();

            UpdateButtons();
            Debug.Log($"Snapshot recorded. Undo Count: {undoStack.Count}");
        }
    }

    /// <summary>
    /// Undoを実行します
    /// </summary>
    public void Undo()
    {
        if (undoStack.Count == 0) return;

        var currentStatus = Object.FindFirstObjectByType<CurrentGameStatus>();
        if (currentStatus == null) return;

        // 現在の状態をRedo用に保存
        var currentData = currentStatus.GetCurrentStageData();
        if (currentData != null)
        {
            redoStack.Push(currentData.DeepCopy());
        }

        // 以前の状態を取り出す
        var prevData = undoStack.Pop();
        ApplyState(prevData);

        Debug.Log("Undo executed.");
    }

    /// <summary>
    /// Redoを実行します
    /// </summary>
    public void Redo()
    {
        if (redoStack.Count == 0) return;

        var currentStatus = Object.FindFirstObjectByType<CurrentGameStatus>();
        if (currentStatus == null) return;

        // 現在の状態をUndo用に保存
        var currentData = currentStatus.GetCurrentStageData();
        if (currentData != null)
        {
            undoStack.Push(currentData.DeepCopy());
        }

        // 以前の状態を取り出す
        var nextData = redoStack.Pop();
        ApplyState(nextData);

        Debug.Log("Redo executed.");
    }

    /// <summary>
    /// ステートを適用し、画面を更新します
    /// </summary>
    private void ApplyState(StageDatabase.StageData data)
    {
        if (data == null) return;

        // 1. データをセット
        var currentStatus = Object.FindFirstObjectByType<CurrentGameStatus>();
        if (currentStatus != null)
        {
            currentStatus.SetRuntimeStageData(data);
        }

        // 2. RSParentの中身を破棄
        GameObject parentObject = GameObject.Find("RSParent");
        if (parentObject != null)
        {
            foreach (Transform child in parentObject.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // 3. ビューの更新
        // Gridの再生成
        var gridGenerator = Object.FindFirstObjectByType<GridGenerator>();
        if (gridGenerator != null)
        {
            gridGenerator.GenerateGrid();
        }

        // アイテムリストの再生成
        var itemGen = Object.FindFirstObjectByType<StickyNotesGenerator>();
        if (itemGen != null)
        {
            itemGen.GenerateItems();
        }

        // 4. Progressの状態を更新（1フレーム待ってから実行）
        StartCoroutine(UpdateProgressDelayed());

        UpdateButtons();
    }

    /// <summary>
    /// 1フレーム待ってからProgressの状態を更新します
    /// </summary>
    private System.Collections.IEnumerator UpdateProgressDelayed()
    {
        // GridGeneratorなどの破壊・生成処理が反映されるのを待つ
        yield return null;

        // Progressアイテムを再生成（初期状態に戻す）
        var ProgressManager = Object.FindFirstObjectByType<ProgressManager>();
        if (ProgressManager != null)
        {
            ProgressManager.CreateProgressItems();
        }

        // GridMonitorの監視状態をリセットして再計算
        var gridMonitor = Object.FindFirstObjectByType<GridMonitor>();
        if (gridMonitor != null)
        {
            gridMonitor.ResetMonitor();
            gridMonitor.RecalculateProgress();
        }
    }

    /// <summary>
    /// ボタンの有効/無効状態を更新します
    /// </summary>
    private void UpdateButtons()
    {
        bool canUndo = undoStack.Count > 0;
        bool canRedo = redoStack.Count > 0;

        if (undoSpriteRenderer != null)
        {
            undoSpriteRenderer.color = canUndo ? normalColor : disabledColor;
        }

        if (redoSpriteRenderer != null)
        {
            redoSpriteRenderer.color = canRedo ? normalColor : disabledColor;
        }

        if (undoButton != null)
        {
            undoButton.SetInteractable(canUndo);
        }

        if (redoButton != null)
        {
            redoButton.SetInteractable(canRedo);
        }
    }

    /// <summary>
    /// Undoボタンがクリックされたときに呼ばれます
    /// </summary>
    public void OnUndoClicked()
    {
        // ボタン押下時のSEを再生
        if (redoCuePlay != null)
        {
            redoCuePlay.PlaySound();
        }

        Undo();
    }

    /// <summary>
    /// Redoボタンがクリックされたときに呼ばれます
    /// </summary>
    public void OnRedoClicked()
    {
        // ボタン押下時のSEを再生
        if (redoCuePlay != null)
        {
            redoCuePlay.PlaySound();
        }

        Redo();
    }

    /// <summary>
    /// ボタンが押下されたときの色を設定します
    /// </summary>
    public void SetButtonPressed(bool isUndo, bool pressed)
    {
        if (isUndo && undoSpriteRenderer != null)
        {
            undoSpriteRenderer.color = pressed ? pressedColor : (undoStack.Count > 0 ? normalColor : disabledColor);
        }
        else if (!isUndo && redoSpriteRenderer != null)
        {
            redoSpriteRenderer.color = pressed ? pressedColor : (redoStack.Count > 0 ? normalColor : disabledColor);
        }
    }
}

/// <summary>
/// スプライトベースのUndo/Redoボタンコンポーネント
/// </summary>
public class UndoRedoSpriteButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    private UndoRedoManager manager;
    private bool isUndo;
    private bool isInteractable = true;
    private bool isPressed = false;

    public void Initialize(UndoRedoManager mgr, bool undo)
    {
        manager = mgr;
        isUndo = undo;
    }

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable) return;

        if (isUndo)
        {
            manager.OnUndoClicked();
        }
        else
        {
            manager.OnRedoClicked();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isInteractable) return;

        isPressed = true;
        manager.SetButtonPressed(isUndo, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isPressed)
        {
            isPressed = false;
            manager.SetButtonPressed(isUndo, false);
        }
    }
}
