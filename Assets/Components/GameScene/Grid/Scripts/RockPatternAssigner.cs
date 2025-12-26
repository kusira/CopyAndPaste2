
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
                
                // Emission初期化を再実行（スプライト変更後）
                InitializeEmission();
                
                return;
            }
        }

        // 見つからなければ外す
        patternRenderer.sprite = null;
        currentPatternKey = null;
        
        // MaterialPropertyBlockをクリアしてマテリアルのデフォルト値に戻す
        ClearMaterialPropertyBlock();
        
        // Emission初期化を再実行
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
            Debug.LogWarning($"{name}: PatternRendererが見つかりません");
            return;
        }

        // 元のマテリアルを使用（MaterialPropertyBlockで個別に制御するため、マテリアルを変更する必要はない）
        Material material = patternRenderer.sharedMaterial;
        if (material == null)
        {
            Debug.LogWarning($"{name}: PatternRendererにマテリアルが設定されていません");
            return;
        }

        emissionPropertyName = GetEmissionPropertyName(material);
        if (emissionPropertyName == null)
        {
            Debug.LogWarning($"{name}: Emissionプロパティが見つかりません。マテリアル: {material.name}, シェーダー: {material.shader.name}");
            return;
        }

        propertyBlock = new MaterialPropertyBlock();
        patternRenderer.GetPropertyBlock(propertyBlock);
        isEmissionInitialized = true;
        Debug.Log($"{name}: Emission初期化完了。プロパティ名: {emissionPropertyName}");
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
            Debug.LogWarning($"{name}: SetEmissionEnabledが失敗しました。初期化: {isEmissionInitialized}, Renderer: {patternRenderer != null}, プロパティ名: {emissionPropertyName}");
            return;
        }

        Debug.Log($"{name}: SetEmissionEnabled呼び出し。enabled={enabled}, animate={animate}");

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
                Debug.Log($"{name}: Emissionアニメーション開始。現在HDR: {currentHDR}, 目標HDR: {emissionMaxHDR}, ベースカラー: {baseColor}");
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
                Debug.Log($"{name}: Emission即座に設定。HDR: {emissionMaxHDR}, カラー: {emissionColor}");
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
        
        // 発光状態と初期化状態をリセット（パターンが変わるため、以前の状態は引き継がない）
        isEmissionInitialized = false;
        isEmissionEnabled = false;
        propertyBlock = null;
    }

    /// <summary>
    /// マテリアルからEmissionColorプロパティ名を取得します
    /// </summary>
    private string GetEmissionPropertyName(Material material)
    {
        if (material == null) return null;

        // シェーダーのプロパティを走査して、マテリアルに実際に存在するプロパティを確認
        Shader shader = material.shader;
        int propertyCount = shader.GetPropertyCount();
        
        // デバッグ用：すべてのプロパティを列挙
        List<string> allProperties = new List<string>();
        List<string> colorProperties = new List<string>();
        
        // 1. シェーダーのすべてのプロパティを走査
        for (int i = 0; i < propertyCount; i++)
        {
            string propName = shader.GetPropertyName(i);
            UnityEngine.Rendering.ShaderPropertyType propType = shader.GetPropertyType(i);
            
            // マテリアルに実際にこのプロパティが存在するか確認
            if (material.HasProperty(propName))
            {
                allProperties.Add($"{propName} ({propType})");
                
                // ColorまたはVector型のみ対象
                if (propType == UnityEngine.Rendering.ShaderPropertyType.Color || 
                    propType == UnityEngine.Rendering.ShaderPropertyType.Vector)
                {
                    colorProperties.Add(propName);
                    
                    // "Emission"を含むプロパティを最優先で探す
                    string lowerName = propName.ToLower();
                    if (lowerName.Contains("emission"))
                    {
                        Debug.Log($"{name}: Emissionプロパティを発見: {propName}");
                        return propName;
                    }
                }
            }
        }
        
        // 2. 見つからなかった場合、すべてのプロパティをログ出力
        Debug.LogWarning($"{name}: Emissionプロパティが見つかりませんでした。");
        Debug.LogWarning($"{name}: マテリアル '{material.name}' のシェーダー '{shader.name}' のすべてのプロパティ:");
        foreach (var prop in allProperties)
        {
            Debug.LogWarning($"{name}:   - {prop}");
        }
        
        if (colorProperties.Count > 0)
        {
            Debug.LogWarning($"{name}: 利用可能なColor型プロパティ: {string.Join(", ", colorProperties)}");
            // 最初のColor型プロパティを試す（フォールバック）
            Debug.LogWarning($"{name}: フォールバックとして最初のColor型プロパティ '{colorProperties[0]}' を使用します");
            return colorProperties[0];
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
    }
}
