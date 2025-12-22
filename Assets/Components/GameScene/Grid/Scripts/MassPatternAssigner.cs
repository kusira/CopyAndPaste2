using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Massに対してギミック（模様）を適用するコンポーネント。
/// Base文字'.'の後ろについたキー（例: ".S" → "S"）を解釈して
/// Pattern子オブジェクトのSpriteを切り替えます。
/// </summary>
public class MassPatternAssigner : MonoBehaviour
{
    [System.Serializable]
    public class PatternEntry
    {
        public string key;
        public Sprite sprite;
    }

    [Tooltip("キーとSpriteの対応を登録します（例: key='S', sprite=Star画像）")]
    [SerializeField] private List<PatternEntry> patternTable = new List<PatternEntry>();

    private readonly Dictionary<string, Sprite> patternDict = new Dictionary<string, Sprite>();
    private Transform patternTransform;
    private SpriteRenderer patternRenderer;
    
    // Mass本体のSprite制御用
    [Header("Mass Sprite Settings")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite cantPutSprite;
    private SpriteRenderer massRenderer;

    private void Awake()
    {
        BuildDict();
        CachePatternRenderer();
        massRenderer = GetComponent<SpriteRenderer>();
    }

    private void BuildDict()
    {
        patternDict.Clear();
        foreach (var entry in patternTable)
        {
            if (entry == null || string.IsNullOrEmpty(entry.key) || entry.sprite == null) continue;
            patternDict[entry.key] = entry.sprite;
        }
    }

    private void CachePatternRenderer()
    {
        patternTransform = transform.Find("Pattern");
        if (patternTransform != null)
        {
            patternRenderer = patternTransform.GetComponent<SpriteRenderer>();
        }
    }

    public void ApplyPatterns(string cellValue)
    {
        if (patternRenderer == null)
        {
            CachePatternRenderer();
        }
        if (patternRenderer == null)
        {
            Debug.LogWarning($"{name}: Patternオブジェクトが見つかりません");
            return;
        }

        // パース
        char baseChar;
        var keys = new List<string>();
        RangeSelectorHelper.ParseCell(cellValue, out baseChar, keys);

        // Mass本体のSpriteとTagを設定
        if (baseChar == '.')
        {
            if (massRenderer != null && normalSprite != null)
            {
                massRenderer.sprite = normalSprite;
            }
            gameObject.tag = "Mass";

            // 何もキーがない場合はSpriteを外す
            if (keys.Count == 0)
            {
                if (patternRenderer != null) patternRenderer.sprite = null;
                return;
            }

            // 見つからなければ外す（デフォルト）
            if (patternRenderer != null) patternRenderer.sprite = null;

            // 最初にマッチしたキーのSpriteを適用
            foreach (var k in keys)
            {
                if (patternDict.TryGetValue(k, out var sp))
                {
                    if (patternRenderer != null) patternRenderer.sprite = sp;
                    return;
                }
            }
        }
        else
        {
            // '.' 以外は配置不可マスとする
            if (massRenderer != null && cantPutSprite != null)
            {
                massRenderer.sprite = cantPutSprite;
            }
            gameObject.tag = "Untagged";
            
            // パターンは表示しない
            if (patternRenderer != null)
            {
                patternRenderer.sprite = null;
            }
        }
    }
}


