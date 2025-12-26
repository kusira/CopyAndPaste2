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
    private MaterialPropertyBlock propertyBlock;
    private Tween emissionTween;
    private string emissionPropertyName;
    private bool isEmissionInitialized = false;
    private bool isEmissionEnabled = false; // 現在光っているかどうか

    private void Awake()
    {
        BuildDict();
        CachePatternRenderer();
        InitializeEmission();
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
            return;
        }

        // 最初にマッチしたキーのSpriteを適用（複数ある場合は先頭優先）
        foreach (var k in keys)
        {
            if (patternDict.TryGetValue(k, out var sp))
            {
                patternRenderer.sprite = sp;
                currentPatternKey = k; // 現在のパターンキーを保存
                return;
            }
        }

        // 見つからなければ外す
        patternRenderer.sprite = null;
        currentPatternKey = null;
    }

    /// <summary>
    /// 現在適用されているパターンキーを取得します
    /// </summary>
    public string GetCurrentPatternKey()
    {
        return currentPatternKey;
    }

    /// <summary>
    /// EmissionColorの初期化を行います
    /// </summary>
    private void InitializeEmission()
    {
        if (patternRenderer == null)
        {
            CachePatternRenderer();
        }
        if (patternRenderer == null)
        {
            return;
        }

        Material material = patternRenderer.sharedMaterial;
        if (material == null)
        {
            return;
        }

        emissionPropertyName = GetEmissionPropertyName(material);
        if (emissionPropertyName == null)
        {
            return;
        }

        propertyBlock = new MaterialPropertyBlock();
        patternRenderer.GetPropertyBlock(propertyBlock);
        isEmissionInitialized = true;
    }

    /// <summary>
    /// EmissionColorを有効化してアニメーションします
    /// </summary>
    public void SetEmissionEnabled(bool enabled, bool animate = true)
    {
        if (!isEmissionInitialized)
        {
            InitializeEmission();
        }
        if (!isEmissionInitialized || patternRenderer == null || emissionPropertyName == null)
        {
            return;
        }

        // 既存のTweenがあれば停止
        if (emissionTween != null && emissionTween.IsActive())
        {
            emissionTween.Kill();
        }

        patternRenderer.GetPropertyBlock(propertyBlock);

        if (enabled)
        {
            // 現在のプロパティ値を取得
            Color currentEmission = propertyBlock.GetColor(emissionPropertyName);
            
            // マテリアルから初期値を取得（MaterialPropertyBlockに値がない場合）
            Material material = patternRenderer.sharedMaterial;
            if (material != null && material.HasProperty(emissionPropertyName))
            {
                Color materialEmission = material.GetColor(emissionPropertyName);
                // MaterialPropertyBlockに値が設定されていない、または初期値と同じ場合はマテリアルから取得
                if (currentEmission == Color.black || currentEmission.maxColorComponent < 0.01f)
                {
                    currentEmission = materialEmission;
                }
            }

            // HDR値を抽出
            float currentHDR = currentEmission.maxColorComponent;
            
            // HDR値が0（または非常に小さい値）の場合のみアニメーションを開始
            // 既に光っている場合は再アニメーションしない
            if (currentHDR >= emissionMaxHDR * 0.95f)
            {
                // 既に最大値に近い場合は、状態を更新して終了
                isEmissionEnabled = true;
                return;
            }
            
            // 状態を更新
            isEmissionEnabled = true;

            // ベースカラーを取得
            Color baseColor = currentEmission;
            if (currentHDR > 0.01f)
            {
                baseColor = new Color(
                    currentEmission.r / currentHDR,
                    currentEmission.g / currentHDR,
                    currentEmission.b / currentHDR,
                    currentEmission.a
                );
            }
            else
            {
                baseColor = Color.white;
            }

            if (animate)
            {
                // HDR値を0→最大値とアニメーション
                emissionTween = DOTween.To(
                    () => currentHDR,
                    hdr => {
                        Color emissionColor = baseColor * hdr;
                        propertyBlock.SetColor(emissionPropertyName, emissionColor);
                        patternRenderer.SetPropertyBlock(propertyBlock);
                    },
                    emissionMaxHDR,
                    emissionAnimationDuration
                ).SetEase(Ease.OutQuad);
            }
            else
            {
                // 即座に最大値に設定
                Color emissionColor = baseColor * emissionMaxHDR;
                propertyBlock.SetColor(emissionPropertyName, emissionColor);
                patternRenderer.SetPropertyBlock(propertyBlock);
            }
        }
        else
        {
            // 状態を更新
            isEmissionEnabled = false;
            
            // 0に設定
            Color emissionColor = Color.black;
            propertyBlock.SetColor(emissionPropertyName, emissionColor);
            patternRenderer.SetPropertyBlock(propertyBlock);
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
        SetEmissionEnabled(false, false);
    }

    /// <summary>
    /// マテリアルからEmissionColorプロパティ名を取得します
    /// </summary>
    private string GetEmissionPropertyName(Material material)
    {
        if (material == null) return null;

        string[] possiblePropertyNames = { "_EmissionColor", "_Emission", "Color", "_BaseColor", "_MainColor" };
        
        // まず、シェーダーのすべてのプロパティを確認してEmission関連を探す
        Shader shader = material.shader;
        int propertyCount = shader.GetPropertyCount();
        string emissionPropertyName = null;
        
        for (int i = 0; i < propertyCount; i++)
        {
            string propName = shader.GetPropertyName(i);
            UnityEngine.Rendering.ShaderPropertyType propType = shader.GetPropertyType(i);
            
            // Color型のプロパティで、Emission関連の名前を持つものを探す
            if (propType == UnityEngine.Rendering.ShaderPropertyType.Color || 
                propType == UnityEngine.Rendering.ShaderPropertyType.Vector)
            {
                string lowerName = propName.ToLower();
                if (lowerName.Contains("emission") || lowerName.Contains("color"))
                {
                    // より具体的な名前を優先
                    if (lowerName.Contains("emission"))
                    {
                        emissionPropertyName = propName;
                        break;
                    }
                    else if (emissionPropertyName == null)
                    {
                        emissionPropertyName = propName;
                    }
                }
            }
        }
        
        // 見つからない場合は、候補リストから試す
        if (emissionPropertyName == null)
        {
            foreach (string propName in possiblePropertyNames)
            {
                if (material.HasProperty(propName))
                {
                    emissionPropertyName = propName;
                    break;
                }
            }
        }

        return emissionPropertyName;
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


