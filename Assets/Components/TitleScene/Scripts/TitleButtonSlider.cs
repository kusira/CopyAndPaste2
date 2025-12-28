using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// UI Imageにアサインしてホバー時に左に移動するアニメーションを実装するクラス
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TitleButtonSlider : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Animation Settings")]
    [Tooltip("ホバー時の移動量（ピクセル）")]
    [SerializeField] private float moveOffset = 100f;

    [Tooltip("アニメーション時間（秒）")]
    [SerializeField] private float animationDuration = 0.3f;

    [Tooltip("Easingタイプ")]
    [SerializeField] private Ease easing = Ease.OutQuad;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Tween positionTween;
    private bool isInitialized = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
            isInitialized = true;
        }
        else
        {
            Debug.LogWarning("MenuButton: RectTransformが見つかりません");
        }
    }

    private void OnDestroy()
    {
        // アニメーションを停止
        positionTween?.Kill();
    }

    /// <summary>
    /// ポインターが入ったときに呼ばれます
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInitialized) return;

        // 既存のアニメーションを停止
        positionTween?.Kill();

        // 左に移動
        Vector2 targetPosition = originalPosition + new Vector2(-moveOffset, 0f);
        positionTween = rectTransform.DOAnchorPos(targetPosition, animationDuration)
            .SetEase(easing);
    }

    /// <summary>
    /// ポインターが出たときに呼ばれます
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInitialized) return;

        // 既存のアニメーションを停止
        positionTween?.Kill();

        // 元の位置に戻る
        positionTween = rectTransform.DOAnchorPos(originalPosition, animationDuration)
            .SetEase(easing);
    }
}

