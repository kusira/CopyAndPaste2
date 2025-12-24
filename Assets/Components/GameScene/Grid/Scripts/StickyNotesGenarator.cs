using UnityEngine;

/// <summary>
/// 現在のステージのRSItem数に応じてStickyNoteを生成するスクリプト
/// </summary>
public class StickyNotesGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("StickyNoteのPrefabをアサインします")]
    [SerializeField] private GameObject stickyNotePrefab;

    [Tooltip("StickyNoteを配置する親Transform。未指定の場合はこのGameObjectのTransformが親になります")]
    [SerializeField] private Transform stickyNoteParent;

    [Header("References")]
    [Tooltip("現在のゲームステータスを参照します")]
    [SerializeField] private CurrentGameStatus currentGameStatus;

    [Header("Layout Settings")]
    [Tooltip("最初のStickyNoteを配置するワールド座標")]
    [SerializeField] private Vector2 startPosition = new Vector2(-1.2f, 1.5f);

    [Tooltip("横方向の間隔")]
    [SerializeField] private float cellWidth = 2.4f; // -1.2 -> 1.2

    [Tooltip("縦方向の間隔（下方向が正）")]
    [SerializeField] private float cellHeight = 3.0f; // 1.5 -> -1.5

    [Tooltip("1行あたりの列数")]
    [SerializeField] private int columns = 2;

    private void Start()
    {
        if (currentGameStatus == null)
        {
            currentGameStatus = FindFirstObjectByType<CurrentGameStatus>();
        }

        GenerateStickyNotes();
    }

    /// <summary>
    /// 現在のステージのRSItem数に応じてStickyNoteを生成します
    /// </summary>
    public void GenerateStickyNotes()
    {
        // 既存の子オブジェクトをクリア
        ClearChildren();

        if (stickyNotePrefab == null)
        {
            Debug.LogWarning("StickyNotesGenerator: stickyNotePrefab がアサインされていません");
            return;
        }

        if (currentGameStatus == null)
        {
            Debug.LogWarning("StickyNotesGenerator: CurrentGameStatus が見つかりません");
            return;
        }

        StageDatabase.StageData stageData = currentGameStatus.GetCurrentStageData();
        if (stageData == null || stageData.RSItems == null || stageData.RSItems.Count == 0)
        {
            Debug.LogWarning("StickyNotesGenerator: RSItems が設定されていません");
            return;
        }

        int count = stageData.RSItems.Count;

        // 親Transformを決定（未指定ならこのGameObject）
        Transform parent = stickyNoteParent != null ? stickyNoteParent : transform;

        for (int i = 0; i < count; i++)
        {
            // グリッド上の行・列を計算（左上から右→下の順）
            int row = i / Mathf.Max(1, columns);
            int col = i % Mathf.Max(1, columns);

            float x = startPosition.x + col * cellWidth;
            float y = startPosition.y - row * cellHeight;

            Vector3 pos = new Vector3(x, y, 0f);

            GameObject note = Instantiate(stickyNotePrefab, pos, Quaternion.identity, parent);
            note.name = $"StickyNote_{i}";

            // RSItemBehavior に論理サイズとインデックスを設定
            var behavior = note.GetComponent<RSItemBehavior>();
            if (behavior != null)
            {
                var itemData = stageData.RSItems[i];
                int h = Mathf.Max(1, itemData.height);
                int w = Mathf.Max(1, itemData.width);

                // RSItemBehavior は (width, height) の順で受け取る
                behavior.SetLogicalSize(w, h);
                behavior.SetItemIndex(i);
            }
        }

        Debug.Log($"StickyNotesGenerator: StickyNoteを {count} 個生成しました");
    }

    /// <summary>
    /// 既存コードとの互換用。GenerateStickyNotes を呼び出します。
    /// </summary>
    public void GenerateItems()
    {
        GenerateStickyNotes();
    }

    /// <summary>
    /// 既存コードとの互換用。子オブジェクトをクリアします。
    /// </summary>
    public void ClearItems()
    {
        ClearChildren();
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}


