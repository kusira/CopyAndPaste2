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
    [Tooltip("最初のStickyNoteを配置するローカル座標（StickyNoteParentが指定されている場合はその親からの相対座標）")]
    [SerializeField] private Vector2 startPosition = new Vector2(-1.2f, 1.5f);

    [Tooltip("Itemどうしの間隔（X方向）")]
    [SerializeField] private float itemSpaceX = 2.4f;

    [Tooltip("Itemどうしの間隔（Y方向）")]
    [SerializeField] private float itemSpaceY = 3.0f;

    [Tooltip("各StickyNoteのスケール（X, Y同じ）")]
    [SerializeField] private float stickyNoteScale = 0.9f;

    [Header("Layout Settings (5-6 Items)")]
    [Tooltip("アイテム数が5または6の場合の開始位置")]
    [SerializeField] private Vector2 startPositionFor5Or6 = new Vector2(-1.1f, 1.85f);

    [Tooltip("アイテム数が5または6の場合のItemどうしの間隔（X方向）")]
    [SerializeField] private float itemSpaceXFor5Or6 = 2.2f;

    [Tooltip("アイテム数が5または6の場合のItemどうしの間隔（Y方向）")]
    [SerializeField] private float itemSpaceYFor5Or6 = 2.6f;

    [Tooltip("アイテム数が5または6の場合の各StickyNoteのスケール（X, Y同じ）")]
    [SerializeField] private float stickyNoteScaleFor5Or6 = 0.8f;

    // 1行あたりの列数（固定値）
    private const int columns = 2;

    // 最初の生成時に決定したパラメータ（ゲーム途中で変更しない）
    private bool parametersInitialized = false;
    private Vector2 savedStartPosition;
    private float savedItemSpaceX;
    private float savedItemSpaceY;
    private float savedStickyNoteScale;

    private void Start()
    {
        if (currentGameStatus == null)
        {
            Debug.LogWarning("StickyNotesGenerator: CurrentGameStatusがアサインされていません");
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

        // パラメータを設定（最初の生成時のみ決定し、以降は変更しない）
        Vector2 actualStartPosition;
        float actualItemSpaceX;
        float actualItemSpaceY;
        float actualStickyNoteScale;

        if (!parametersInitialized)
        {
            // 最初の生成時：アイテム数に応じてパラメータを決定
            actualStartPosition = startPosition;
            actualItemSpaceX = itemSpaceX;
            actualItemSpaceY = itemSpaceY;
            actualStickyNoteScale = stickyNoteScale;

            if (count == 5 || count == 6)
            {
                actualStartPosition = startPositionFor5Or6;
                actualItemSpaceX = itemSpaceXFor5Or6;
                actualItemSpaceY = itemSpaceYFor5Or6;
                actualStickyNoteScale = stickyNoteScaleFor5Or6;
            }

            // パラメータを保存
            savedStartPosition = actualStartPosition;
            savedItemSpaceX = actualItemSpaceX;
            savedItemSpaceY = actualItemSpaceY;
            savedStickyNoteScale = actualStickyNoteScale;
            parametersInitialized = true;
        }
        else
        {
            // 2回目以降：保存されたパラメータを使用
            actualStartPosition = savedStartPosition;
            actualItemSpaceX = savedItemSpaceX;
            actualItemSpaceY = savedItemSpaceY;
            actualStickyNoteScale = savedStickyNoteScale;
        }

        // 親Transformを決定
        Transform parent = stickyNoteParent != null ? stickyNoteParent : transform;

        for (int i = 0; i < count; i++)
        {
            // グリッド上の行・列を計算（左上から右→下の順）
            int row = i / Mathf.Max(1, columns);
            int col = i % Mathf.Max(1, columns);

            // ローカル座標を計算
            float x = actualStartPosition.x + col * actualItemSpaceX;
            float y = actualStartPosition.y - row * actualItemSpaceY;
            Vector3 localPos = new Vector3(x, y, 0f);

            // StickyNoteParentが指定されている場合は相対座標（ローカル座標）で配置
            // 指定されていない場合はこのGameObjectを親として配置
            GameObject note = Instantiate(stickyNotePrefab, parent);
            note.transform.localPosition = localPos;
            note.transform.localScale = Vector3.one * actualStickyNoteScale;
            note.transform.localRotation = Quaternion.identity;
            note.name = $"StickyNote_{i}";

            // StickyNoteBehavior に論理サイズとインデックスを設定
            var behavior = note.GetComponent<StickyNoteBehavior>();
            if (behavior != null)
            {
                var itemData = stageData.RSItems[i];
                int h = Mathf.Max(1, itemData.height);
                int w = Mathf.Max(1, itemData.width);

                // StickyNoteBehavior は (width, height) の順で受け取る
                behavior.SetLogicalSize(w, h);
                behavior.SetItemIndex(i);
                behavior.SetItemType(itemData.type);

                // RSItemMassを生成
                behavior.GenerateItemMasses();
            }
        }
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
        // 親Transformを決定
        Transform parent = stickyNoteParent != null ? stickyNoteParent : transform;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
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


