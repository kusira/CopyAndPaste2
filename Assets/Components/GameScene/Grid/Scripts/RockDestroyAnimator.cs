using System.Collections;
using UnityEngine;

/// <summary>
/// 岩が破壊されたときのアニメーションを管理するスクリプト
/// </summary>
public class RockDestroyAnimator : MonoBehaviour
{
    [System.Serializable]
    public class DestroySpriteSet
    {
        [Tooltip("破壊アニメーション1枚目のスプライト（Destroy_1）をアサインします")]
        public Sprite destroy_1;
        
        [Tooltip("破壊アニメーション2枚目のスプライト（Destroy_2）をアサインします")]
        public Sprite destroy_2;
    }

    [Header("Destroy Sprites by Pattern Type")]
    [Tooltip("何もなし（パターンなし）の破壊スプライト")]
    [SerializeField] private DestroySpriteSet noneSprites = new DestroySpriteSet();
    
    [Tooltip("Star（Sキー）の破壊スプライト")]
    [SerializeField] private DestroySpriteSet starSprites = new DestroySpriteSet();
    
    [Tooltip("Heart（Hキー）の破壊スプライト")]
    [SerializeField] private DestroySpriteSet heartSprites = new DestroySpriteSet();
    
    [Tooltip("Clover（Cキー）の破壊スプライト")]
    [SerializeField] private DestroySpriteSet cloverSprites = new DestroySpriteSet();

    [Header("References")]
    [Tooltip("Patternゲームオブジェクトをアサインします")]
    [SerializeField] private GameObject pattern;

    [Header("Animation Settings")]
    [Tooltip("各スプライト表示の待機時間（秒）")]
    [SerializeField] private float waitDuration = 0.5f;

    private SpriteRenderer spriteRenderer;
    private RockPatternAssigner patternAssigner;
    private bool isDestroying = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"RockDestroyAnimator: {gameObject.name}にSpriteRendererがアタッチされていません");
        }

        // RockPatternAssignerを取得
        patternAssigner = GetComponent<RockPatternAssigner>();
    }

    /// <summary>
    /// 破壊アニメーションを開始します
    /// </summary>
    public void StartDestroyAnimation()
    {
        if (isDestroying)
        {
            return;
        }

        isDestroying = true;
        StartCoroutine(DestroyAnimationCoroutine());
    }

    /// <summary>
    /// 現在のパターンタイプを取得します
    /// </summary>
    private string GetCurrentPatternKey()
    {
        if (patternAssigner == null)
        {
            return null;
        }

        return patternAssigner.GetCurrentPatternKey();
    }

    /// <summary>
    /// パターンキーに応じた破壊スプライトセットを取得します
    /// </summary>
    private DestroySpriteSet GetDestroySpriteSet(string patternKey)
    {
        if (string.IsNullOrEmpty(patternKey))
        {
            return noneSprites;
        }

        switch (patternKey.ToUpper())
        {
            case "S": // Star
                return starSprites;
            case "H": // Heart
                return heartSprites;
            case "C": // Clover
                return cloverSprites;
            default:
                return noneSprites;
        }
    }

    /// <summary>
    /// 破壊アニメーションのコルーチン
    /// </summary>
    private IEnumerator DestroyAnimationCoroutine()
    {
        // Patternゲームオブジェクトを非活性にする
        if (pattern != null)
        {
            pattern.SetActive(false);
        }

        // 現在のパターンタイプを取得
        string patternKey = GetCurrentPatternKey();
        DestroySpriteSet spriteSet = GetDestroySpriteSet(patternKey);

        // SpriteRendererをDestroy_1にする
        if (spriteRenderer != null && spriteSet.destroy_1 != null)
        {
            spriteRenderer.sprite = spriteSet.destroy_1;
        }

        // 0.5秒待機
        yield return new WaitForSeconds(waitDuration);

        // Destroy_2にする
        if (spriteRenderer != null && spriteSet.destroy_2 != null)
        {
            spriteRenderer.sprite = spriteSet.destroy_2;
        }

        // 0.5秒待機
        yield return new WaitForSeconds(waitDuration);

        // Destroy
        Destroy(gameObject);
    }
}

