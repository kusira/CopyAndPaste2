using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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

    [Header("Stage Settings")]
    [Tooltip("このボタンが対応するステージインデックス（0から開始）")]
    [SerializeField] private int stageIndex = 0;

    [Header("References")]
    [Tooltip("CurrentGameStatus（未設定の場合は自動検索します）")]
    [SerializeField] private CurrentGameStatus currentGameStatus;

    private RectTransform rectTransform;
    private Button button;
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

        // 自身のButtonコンポーネントを取得
        button = GetComponent<Button>();
    }

    private void Start()
    {
        // CurrentGameStatusが設定されていない場合は自動検索
        if (currentGameStatus == null)
        {
            currentGameStatus = FindFirstObjectByType<CurrentGameStatus>();
        }

        // MaxReachedStageIndexを超過している場合はボタンを無効化
        UpdateButtonState();
    }

    /// <summary>
    /// ボタンの有効/無効状態を更新します（MaxReachedStageIndexに基づく）
    /// </summary>
    private void UpdateButtonState()
    {
        if (button == null) return;

        if (currentGameStatus == null)
        {
            // CurrentGameStatusが見つからない場合は無効化
            button.interactable = false;
            Debug.LogWarning("StageSelectButton: CurrentGameStatus が見つかりませんでした。ボタンを無効化します。");
            return;
        }

        // 到達したステージの最大値を取得
        int maxReachedIndex = currentGameStatus.GetMaxReachedStageIndex();

        // ステージインデックスがMaxReachedStageIndex以下の場合のみ有効化
        bool shouldBeInteractable = (stageIndex <= maxReachedIndex);
        button.interactable = shouldBeInteractable;

        Debug.Log($"StageSelectButton: ステージ{stageIndex}のボタンは、MaxReachedStageIndex({maxReachedIndex})に基づいて{(shouldBeInteractable ? "有効" : "無効")}に設定されました。");
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

        // ボタンがdisabledのときは回転しない
        if (button != null && !button.interactable)
        {
            return;
        }

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

        // ボタンがdisabledのときは回転しない
        if (button != null && !button.interactable)
        {
            return;
        }

        // 既存のアニメーションを停止
        rotationTween?.Kill();

        // 元の角度に戻る
        rotationTween = rectTransform.DOLocalRotate(originalRotation, animationDuration)
            .SetEase(easing);
    }
}

