using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using CriWare;

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

    [Header("Sound Settings")]
    [Tooltip("ホバー時に再生するCuePlayコンポーネント（未設定の場合は同じGameObjectから取得を試みます）")]
    [SerializeField] private CuePlay cuePlay;

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

        // CuePlayが設定されていない場合は同じGameObjectから取得を試みる
        if (cuePlay == null)
        {
            cuePlay = GetComponent<CuePlay>();
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

        // ホバー時のSEを再生
        if (cuePlay != null)
        {
            cuePlay.PlaySound();
        }
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

