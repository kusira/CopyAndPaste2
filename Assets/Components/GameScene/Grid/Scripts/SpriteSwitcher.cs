using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ワールドごとにスプライトを切り替えるコンポーネント。
/// CurrentGameStatusから現在のワールドを取得し、対応するスプライトを適用します。
/// </summary>
public class SpriteSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class WorldSpriteEntry
    {
        [Tooltip("ワールドラベル（例: \"World1\", \"World2\"）")]
        public string worldLabel;
        
        [Tooltip("このワールドで使用するスプライト")]
        public Sprite sprite;
    }

    [Tooltip("ワールドラベルとスプライトの対応表")]
    [SerializeField] private List<WorldSpriteEntry> worldSpriteTable = new List<WorldSpriteEntry>();

    [Tooltip("現在のゲームステータスを参照します。未設定の場合は自動検索します")]
    [SerializeField] private CurrentGameStatus currentGameStatus;

    [Tooltip("スプライトを適用するSpriteRenderer。未設定の場合は自動検索します")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("スプライトを適用するImage（UI用）。未設定の場合は自動検索します")]
    [SerializeField] private Image image;

    [Tooltip("デフォルトスプライト（ワールドが見つからない場合に使用）")]
    [SerializeField] private Sprite defaultSprite;

    private Dictionary<string, Sprite> worldSpriteDict = new Dictionary<string, Sprite>();
    private string currentWorldLabel = null;

    private void Awake()
    {
        BuildDict();
        CacheComponents();
    }

    private void Start()
    {
        ApplyWorldSprite();
    }

    private void OnValidate()
    {
        BuildDict();
    }

    /// <summary>
    /// ワールドラベルとスプライトの辞書を構築します
    /// </summary>
    public void BuildDict()
    {
        worldSpriteDict.Clear();
        foreach (var entry in worldSpriteTable)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.worldLabel) && entry.sprite != null)
            {
                worldSpriteDict[entry.worldLabel] = entry.sprite;
            }
        }
    }

    /// <summary>
    /// 必要なコンポーネントをキャッシュします
    /// </summary>
    private void CacheComponents()
    {
        // CurrentGameStatusを検索
        if (currentGameStatus == null)
        {
            currentGameStatus = FindFirstObjectByType<CurrentGameStatus>();
        }

        // SpriteRendererを検索
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Imageを検索
        if (image == null)
        {
            image = GetComponent<Image>();
        }
    }

    /// <summary>
    /// 現在のワールドに応じてスプライトを適用します
    /// </summary>
    public void ApplyWorldSprite()
    {
        // 現在のワールドラベルを取得
        string worldLabel = GetCurrentWorldLabel();
        
        if (worldLabel == currentWorldLabel)
        {
            // 既に同じワールドが適用されている場合はスキップ
            return;
        }

        currentWorldLabel = worldLabel;

        // ワールドに対応するスプライトを取得
        Sprite spriteToApply = null;
        if (!string.IsNullOrEmpty(worldLabel) && worldSpriteDict.TryGetValue(worldLabel, out spriteToApply))
        {
            // ワールドに対応するスプライトが見つかった
        }
        else if (defaultSprite != null)
        {
            // デフォルトスプライトを使用
            spriteToApply = defaultSprite;
        }
        else
        {
            // スプライトが見つからない場合はnullのまま
            spriteToApply = null;
        }

        // SpriteRendererに適用
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = spriteToApply;
        }

        // Imageに適用
        if (image != null)
        {
            image.sprite = spriteToApply;
        }
    }

    /// <summary>
    /// 現在のワールドラベルを取得します
    /// </summary>
    private string GetCurrentWorldLabel()
    {
        if (currentGameStatus == null)
        {
            currentGameStatus = FindFirstObjectByType<CurrentGameStatus>();
        }

        if (currentGameStatus != null)
        {
            var stageData = currentGameStatus.GetCurrentStageData();
            if (stageData != null && !string.IsNullOrEmpty(stageData.worldLabel))
            {
                return stageData.worldLabel;
            }
        }

        return null;
    }

    /// <summary>
    /// 手動でスプライトを再適用します（エディタ用）
    /// </summary>
    [ContextMenu("スプライトを再適用")]
    public void RefreshSprite()
    {
        BuildDict();
        CacheComponents();
        // 強制的に再適用するためにcurrentWorldLabelをリセット
        currentWorldLabel = null;
        ApplyWorldSprite();
    }
}

