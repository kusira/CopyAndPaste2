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
    [Tooltip("Patternに適用するEmissionマテリアルをアサインします")]
    [SerializeField] private Material emissionMaterial;

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
    private Material instanceMaterial; // インスタンス化されたマテリアル

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
            // MaterialPropertyBlockをクリアしてマテリアルのデフォルト値に戻す
            ClearMaterialPropertyBlock();
            return;
        }

        // 最初にマッチしたキーのSpriteを適用（複数ある場合は先頭優先）
        foreach (var k in keys)
        {
            if (patternDict.TryGetValue(k, out var sp))
            {
                patternRenderer.sprite = sp;
                currentPatternKey = k; // 現在のパターンキーを保存
                // MaterialPropertyBlockをクリアしてマテリアルのデフォルト値に戻す
                ClearMaterialPropertyBlock();
                
                // Emissionマテリアルを適用（マテリアルがアサインされている場合）
                ApplyEmissionMaterial();
                
                return;
            }
        }

        // 見つからなければ外す
        patternRenderer.sprite = null;
        currentPatternKey = null;
        
        // Emissionマテリアルを適用（スプライトがない場合でもマテリアルは適用）
        ApplyEmissionMaterial();
    }

    /// <summary>
    /// EmissionマテリアルをPatternに適用します（各Rockで独立したマテリアルインスタンスを作成）
    /// </summary>
    private void ApplyEmissionMaterial()
    {
        if (patternRenderer == null)
        {
            CachePatternRenderer();
        }
        if (patternRenderer == null)
        {
            return;
        }

        if (emissionMaterial == null)
        {
            return;
        }

        // マテリアルをインスタンス化して、各Rockで独立したマテリアルにする
        if (instanceMaterial == null)
        {
            instanceMaterial = new Material(emissionMaterial);
        }

        patternRenderer.material = instanceMaterial;
        
        // Emission初期化を再実行（新しいマテリアルでプロパティ名を取得）
        propertyBlock = null; // プロパティブロックをリセット
        isEmissionInitialized = false;
        InitializeEmission();
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

        // インスタンス化されたマテリアルを使用（なければsharedMaterial）
        Material material = patternRenderer.material;
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
            Material material = patternRenderer.material;
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
    /// MaterialPropertyBlockをクリアしてマテリアルのデフォルト値に戻します
    /// </summary>
    private void ClearMaterialPropertyBlock()
    {
        if (patternRenderer == null)
        {
            CachePatternRenderer();
        }
        if (patternRenderer == null)
        {
            return;
        }

        // MaterialPropertyBlockをクリア（nullを設定することで、マテリアルのデフォルト値が使用される）
        patternRenderer.SetPropertyBlock(null);
        
        // 既存のTweenがあれば停止
        if (emissionTween != null && emissionTween.IsActive())
        {
            emissionTween.Kill();
        }
        
        // 状態をリセット
        isEmissionEnabled = false;
        isEmissionInitialized = false;
        propertyBlock = null;
    }

    /// <summary>
    /// マテリアルからEmissionColorプロパティ名を取得します
    /// </summary>
    private string GetEmissionPropertyName(Material material)
    {
        if (material == null) return null;

        // 優先すべきプロパティ名の候補 - "Color"や"_BaseColor"などの汎用名は意図的な黒化を招くため除外
        string[] priorityPropertyNames = { "_EmissionColor", "_Emission", "EmissionColor", "Emission" };
        
        // シェーダーのプロパティを走査
        Shader shader = material.shader;
        int propertyCount = shader.GetPropertyCount();
        
        // 1. "Emission" を含むプロパティを最優先で探す
        for (int i = 0; i < propertyCount; i++)
        {
            string propName = shader.GetPropertyName(i);
            string lowerName = propName.ToLower();
            UnityEngine.Rendering.ShaderPropertyType propType = shader.GetPropertyType(i);
            
            // ColorまたはVector型のみ対象
            if ((propType == UnityEngine.Rendering.ShaderPropertyType.Color || 
                 propType == UnityEngine.Rendering.ShaderPropertyType.Vector) && 
                lowerName.Contains("emission"))
            {
                return propName; // "Emission"を含むプロパティが見つかればそれを採用
            }
        }
        
        // 2. 候補リストを確認
        foreach (string propName in priorityPropertyNames)
        {
            if (material.HasProperty(propName))
            {
                return propName;
            }
        }

        // 汎用カラープロパティへのフォールバックは廃止 (意図せず真っ黒になるのを防ぐため)
        return null;
    }

    private void OnDestroy()
    {
        // Tweenをクリーンアップ
        if (emissionTween != null && emissionTween.IsActive())
        {
            emissionTween.Kill();
        }

        // インスタンス化されたマテリアルを破棄
        if (instanceMaterial != null)
        {
            Destroy(instanceMaterial);
        }
    }
}
