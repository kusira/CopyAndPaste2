using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UndoRedoManager : MonoBehaviour
{
    public static UndoRedoManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("UndoボタンのGameObject（Buttonコンポーネントが必要）")]
    [SerializeField] private Button undoButton;
    [Tooltip("RedoボタンのGameObject（Buttonコンポーネントが必要）")]
    [SerializeField] private Button redoButton;

    // 履歴スタック
    private readonly Stack<StageDatabase.StageData> undoStack = new Stack<StageDatabase.StageData>();
    private readonly Stack<StageDatabase.StageData> redoStack = new Stack<StageDatabase.StageData>();

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
        // ボタンのイベント登録
        if (undoButton != null)
        {
            undoButton.onClick.AddListener(Undo);
        }

        if (redoButton != null)
        {
            redoButton.onClick.AddListener(Redo);
        }

        UpdateButtons();
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

        // 2. RangeSelectorParentの中身を破棄
        GameObject parentObject = GameObject.Find("RangeSelectorParent");
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
        var itemGen = Object.FindFirstObjectByType<RangeSelectorItemGenarator>();
        if (itemGen != null)
        {
            itemGen.GenerateItems();
        }

        UpdateButtons();
    }

    /// <summary>
    /// ボタンの有効/無効状態を更新します
    /// </summary>
    private void UpdateButtons()
    {
        if (undoButton != null)
        {
            undoButton.interactable = undoStack.Count > 0;
        }

        if (redoButton != null)
        {
            redoButton.interactable = redoStack.Count > 0;
        }
    }
}
