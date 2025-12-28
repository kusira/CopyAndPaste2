using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// UI Imageにアサインしてホバー時にZ軸回転するアニメーションを実装するクラス
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class StageSelectButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Animation Settings")]
    [Tooltip("ホバー時のZ軸回転角度（度）")]
    [SerializeField] private float rotationAngle = 20f;

    [Tooltip("アニメーション時間（秒）")]
    [SerializeField] private float animationDuration = 0.1f;

    [Tooltip("Easingタイプ")]
    [SerializeField] private Ease easing = Ease.OutQuad;

    private RectTransform rectTransform;
    private Vector3 originalRotation;
    private Tween rotationTween;
    private bool isInitialized = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalRotation = rectTransform.localEulerAngles;
            isInitialized = true;
        }
        else
        {
            Debug.LogWarning("StageSelectButton: RectTransformが見つかりません");
        }
    }

    private void OnDestroy()
    {
        // アニメーションを停止
        rotationTween?.Kill();
    }

    /// <summary>
    /// ポインターが入ったときに呼ばれます
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInitialized) return;

        // 既存のアニメーションを停止
        rotationTween?.Kill();

        // Z軸を回転
        Vector3 targetRotation = originalRotation + new Vector3(0f, 0f, rotationAngle);
        rotationTween = rectTransform.DOLocalRotate(targetRotation, animationDuration)
            .SetEase(easing);
    }

    /// <summary>
    /// ポインターが出たときに呼ばれます
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInitialized) return;

        // 既存のアニメーションを停止
        rotationTween?.Kill();

        // 元の角度に戻る
        rotationTween = rectTransform.DOLocalRotate(originalRotation, animationDuration)
            .SetEase(easing);
    }
}

