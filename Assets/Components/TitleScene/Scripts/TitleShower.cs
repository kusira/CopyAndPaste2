using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// タイトルシーンでゲームオブジェクトを表示するクラス
/// </summary>
public class TitleShower : MonoBehaviour
{
    /// <summary>
    /// アニメーション方向のenum
    /// </summary>
    public enum AnimationDirection
    {
        Up,    // 上から
        Down,  // 下から
        Left,  // 左から
        Right  // 右から
    }

    /// <summary>
    /// 各UI要素のアニメーション設定データ
    /// </summary>
    [System.Serializable]
    public class AnimationItemData
    {
        [Tooltip("アニメーション対象のゲームオブジェクト")]
        public GameObject gameObject;

        [Tooltip("出現方向（上下左右4方向）")]
        public AnimationDirection direction = AnimationDirection.Up;

        [Tooltip("フェードを有効にするか")]
        public bool enableFade = false;

        [Tooltip("Easingタイプ")]
        public Ease easing = Ease.OutQuad;

        [Tooltip("アニメーション時間（秒）")]
        public float animationDuration = 0.5f;

        [Tooltip("アニメーションの移動量（ピクセル）")]
        public float moveOffset = 1000f;

        [Tooltip("前回のオブジェクトがアニメーション後に待機する時間（秒）")]
        public float waitAfterPrevious = 0f;

        [Header("Vibration Settings")]
        [Tooltip("出現後に振動を有効にするか")]
        public bool enableVibration = false;

        [Tooltip("振動の振幅（距離）")]
        public float vibrationAmplitude = 0.1f;

        [Tooltip("振動の周期（秒）")]
        public float vibrationPeriod = 1.0f;

        [Tooltip("振動の方向")]
        public Vector3 vibrationDirection = Vector3.up;
    }

    [Header("Animation Timing")]
    [Tooltip("初期待機時間（秒）")]
    [SerializeField] private float initialWaitSeconds = 0f;

    [Header("UI Animation Items")]
    [Tooltip("各UI要素のアニメーション設定")]
    [SerializeField] private List<AnimationItemData> animationItems = new List<AnimationItemData>();

    private Dictionary<GameObject, Vector2> originalPositions = new Dictionary<GameObject, Vector2>();

    private void Start()
    {
        // タイトル表示を開始
        StartCoroutine(ShowTitleRoutine());
    }

    private IEnumerator ShowTitleRoutine()
    {
        // すべてのアニメーション対象オブジェクトを非活性化（リセット）
        foreach (var item in animationItems)
        {
            if (item != null && item.gameObject != null)
            {
                item.gameObject.SetActive(false);
            }
        }

        // 初期待機時間
        if (initialWaitSeconds > 0f)
        {
            yield return new WaitForSeconds(initialWaitSeconds);
        }

        // 各UI要素を順番にアニメーション
        foreach (var item in animationItems)
        {
            if (item == null || item.gameObject == null) continue;

            // 前回のオブジェクトアニメーション後の待機時間
            if (item.waitAfterPrevious > 0f)
            {
                yield return new WaitForSeconds(item.waitAfterPrevious);
            }

            // UI要素のアニメーションを実行
            yield return StartCoroutine(AnimateUIItem(item));
        }

        Debug.Log("タイトルを表示しました");
    }

    /// <summary>
    /// 個別のUI要素をアニメーションします
    /// </summary>
    private IEnumerator AnimateUIItem(AnimationItemData item)
    {
        GameObject obj = item.gameObject;
        if (obj == null) yield break;

        // アニメーション前にオブジェクトを活性化
        obj.SetActive(true);

        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning($"TitleShower: {obj.name}にRectTransformが見つかりません");
            yield break;
        }

        // 元の位置を保存（初回のみ）
        if (!originalPositions.ContainsKey(obj))
        {
            originalPositions[obj] = rectTransform.anchoredPosition;
        }

        Vector2 targetPosition = originalPositions[obj];
        Vector2 startPosition = GetStartPosition(rectTransform, item.direction, targetPosition, item.moveOffset);

        // CanvasGroupを取得または追加
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        if (item.enableFade && canvasGroup == null)
        {
            canvasGroup = obj.AddComponent<CanvasGroup>();
        }

        // 初期状態を設定
        rectTransform.anchoredPosition = startPosition;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = item.enableFade ? 0f : 1f;
        }

        // アニメーションを実行
        Sequence sequence = DOTween.Sequence();

        // 位置アニメーション
        sequence.Join(rectTransform.DOAnchorPos(targetPosition, item.animationDuration).SetEase(item.easing));

        // フェードアニメーション（有効な場合）
        if (item.enableFade && canvasGroup != null)
        {
            sequence.Join(canvasGroup.DOFade(1f, item.animationDuration).SetEase(item.easing));
        }

        yield return sequence.WaitForCompletion();

        // アニメーション完了後、振動が有効な場合は振動を開始
        if (item.enableVibration)
        {
            ResultItemVibrator vibrator = obj.GetComponent<ResultItemVibrator>();
            if (vibrator == null)
            {
                vibrator = obj.AddComponent<ResultItemVibrator>();
            }
            
            // 振動設定を適用
            vibrator.SetupVibration(
                item.vibrationAmplitude,
                item.vibrationPeriod,
                item.vibrationDirection,
                true
            );
        }
    }

    /// <summary>
    /// 方向に応じた開始位置を取得します
    /// </summary>
    private Vector2 GetStartPosition(RectTransform rectTransform, AnimationDirection direction, Vector2 targetPosition, float offset)
    {
        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        RectTransform canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        if (canvasRect == null)
        {
            return targetPosition;
        }

        switch (direction)
        {
            case AnimationDirection.Up:
                return targetPosition + new Vector2(0f, offset);
            case AnimationDirection.Down:
                return targetPosition + new Vector2(0f, -offset);
            case AnimationDirection.Left:
                return targetPosition + new Vector2(-offset, 0f);
            case AnimationDirection.Right:
                return targetPosition + new Vector2(offset, 0f);
            default:
                return targetPosition;
        }
    }
}

