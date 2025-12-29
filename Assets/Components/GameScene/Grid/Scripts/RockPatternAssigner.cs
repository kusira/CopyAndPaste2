
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Rockに対してギミック（模様）を適用するコンポーネント。
/// Base文字'#'の後ろについたキー（例: "#S" → "S"）を解釈して
/// Pattern子オブジェクトのSpriteを切り替えます。
/// </summary>
public class RockPatternAssigner : MonoBehaviour
{
    [System.Serializable]
    public class PatternEntry
    {
        public string key;
        public Sprite sprite;
    }

    [Tooltip("キーとSpriteの対応を登録します（例: key='S', sprite=Star画像, key='P', sprite=Pentagon画像, key='C', sprite=Circle画像）")]
    [SerializeField] private List<PatternEntry> patternTable = new List<PatternEntry>();

    [Header("Emission Settings")]
    [Tooltip("EmissionColorのHDR値アニメーション時間（秒）")]
    [SerializeField] private float emissionAnimationDuration = 0.5f;

    [Tooltip("EmissionColorのHDR最大値")]
    [SerializeField] private float emissionMaxHDR = 3f;

    private readonly Dictionary<string, Sprite> patternDict = new Dictionary<string, Sprite>();
    private Transform patternTransform;
    private SpriteRenderer patternRenderer;
    private string currentPatternKey = null; // 現在適用されているパターンキー
    private Tween emissionTween;
    private bool isEmissionEnabled = false; // 現在光っているかどうか

    private void Awake()
    {
        BuildDict();
        CachePatternRenderer();
    }

    private void OnValidate()
    {
        BuildDict();
    }

    public void BuildDict()
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
        RSHelper.ParseCell(cellValue, out baseChar, keys);
        
        // Tagを設定
        if (baseChar == '#')
        {
            gameObject.tag = "Rock";
        }

        // 何もキーがない場合はSpriteを外す
        if (keys.Count == 0)
        {
            patternRenderer.sprite = null;
            currentPatternKey = null;
            // 色をリセット
            ResetEmission();
            return;
        }

        // 最初にマッチしたキーのSpriteを適用（複数ある場合は先頭優先）
        foreach (var k in keys)
        {
            if (patternDict.TryGetValue(k, out var sp))
            {
                patternRenderer.sprite = sp;
                currentPatternKey = k; // 現在のパターンキーを保存
                // 色をリセット
                ResetEmission();
                
                return;
            }
        }

        // 見つからなければ外す
        patternRenderer.sprite = null;
        currentPatternKey = null;
        
        // 色をリセット
        ResetEmission();
    }

    /// <summary>
    /// 現在適用されているパターンキーを取得します
    /// </summary>
    public string GetCurrentPatternKey()
    {
        return currentPatternKey;
    }

    /// <summary>
    /// EmissionColorを有効化してアニメーションします
    /// </summary>
    public void SetEmissionEnabled(bool enabled, bool animate = true)
    {
        if (patternRenderer == null)
        {
            CachePatternRenderer();
        }
        if (patternRenderer == null)
        {
            Debug.LogWarning($"{name}: SetEmissionEnabledが失敗しました。PatternRendererが見つかりません");
            return;
        }

        // 既存のTweenがあれば停止
        if (emissionTween != null && emissionTween.IsActive())
        {
            emissionTween.Kill();
        }

        if (enabled)
        {
            // 現在の色を取得
            Color currentColor = patternRenderer.color;
            
            // 現在のHDR値を計算（maxColorComponentを使用）
            float currentHDR = currentColor.maxColorComponent;
            
            // 既に光っている場合は再アニメーションしない
            if (currentHDR >= emissionMaxHDR * 0.95f)
            {
                // 既に最大値に近い場合は、状態を更新して終了
                isEmissionEnabled = true;
                return;
            }
            
            // 状態を更新
            isEmissionEnabled = true;

            // ベースカラーを取得（HDR値を正規化）
            Color baseColor = Color.white;
            if (currentHDR > 0.01f)
            {
                baseColor = new Color(
                    currentColor.r / currentHDR,
                    currentColor.g / currentHDR,
                    currentColor.b / currentHDR,
                    currentColor.a
                );
            }

            if (animate)
            {
                // HDR値を現在値→最大値とアニメーション
                emissionTween = DOTween.To(
                    () => currentHDR,
                    hdr => {
                        Color emissionColor = baseColor * hdr;
                        patternRenderer.color = emissionColor;
                    },
                    emissionMaxHDR,
                    emissionAnimationDuration
                ).SetEase(Ease.OutQuad);
            }
            else
            {
                // 即座に最大値に設定
                Color emissionColor = baseColor * emissionMaxHDR;
                patternRenderer.color = emissionColor;
            }
        }
        else
        {
            // 状態を更新
            isEmissionEnabled = false;
            
            // Color.whiteにリセット（真っ黒を防ぐ）
            patternRenderer.color = Color.white;
        }
    }

    /// <summary>
    /// 現在EmissionColorが最大値に光っているかどうかを判定します
    /// </summary>
    public bool IsEmissionAtMax()
    {
        return isEmissionEnabled;
    }

    /// <summary>
    /// EmissionColorをリセットします
    /// </summary>
    public void ResetEmission()
    {
        if (patternRenderer == null)
        {
            CachePatternRenderer();
        }
        if (patternRenderer == null)
        {
            return;
        }

        // 既存のTweenがあれば停止
        if (emissionTween != null && emissionTween.IsActive())
        {
            emissionTween.Kill();
        }

        // 状態を更新
        isEmissionEnabled = false;
        
        // Color.whiteにリセット（真っ黒を防ぐ）
        patternRenderer.color = Color.white;
    }


    private void OnDestroy()
    {
        // Tweenをクリーンアップ
        if (emissionTween != null && emissionTween.IsActive())
        {
            emissionTween.Kill();
        }
    }
}
