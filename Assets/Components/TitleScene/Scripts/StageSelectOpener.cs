using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// ステージセレクトボタンにアタッチして、複数のオブジェクトをアニメーションで動かすクラス
/// </summary>
[RequireComponent(typeof(Button))]
public class StageSelectOpener : MonoBehaviour
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
    /// 各オブジェクトのアニメーション設定データ
    /// </summary>
    [System.Serializable]
    public class AnimationObjectData
    {
        [Tooltip("アニメーション対象のゲームオブジェクト")]
        public GameObject gameObject;

        [Tooltip("移動方向")]
        public AnimationDirection direction = AnimationDirection.Left;

        [Tooltip("移動量（ピクセル）")]
        public float moveOffset = 100f;

        [Tooltip("アニメーション時間（秒）")]
        public float animationDuration = 0.3f;

        [Tooltip("Easingタイプ")]
        public Ease easing = Ease.OutQuad;

        [Tooltip("前回のオブジェクトがアニメーション後に待機する時間（秒）")]
        public float waitAfterPrevious = 0f;
    }

    [Header("Backdrop")]
    [Tooltip("背景用のBackdropオブジェクトをアサインします")]
    [SerializeField] private GameObject backdropObject;

    [Tooltip("Backdropのフェードアニメーション時間（秒）")]
    [SerializeField] private float backdropFadeDuration = 0.3f;

    [Header("Animation Objects")]
    [Tooltip("アニメーション対象のオブジェクトリスト")]
    [SerializeField] private List<AnimationObjectData> animationObjects = new List<AnimationObjectData>();

    private Button button;
    private Dictionary<GameObject, Vector2> originalPositions = new Dictionary<GameObject, Vector2>();
    private List<Tween> activeTweens = new List<Tween>();
    private bool isAnimating = false;
    private bool isBackdropVisible = false;
    private CanvasGroup backdropCanvasGroup;
    private Button backdropButton;
    private Tween backdropTween;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning("StageSelectOpener: Buttonコンポーネントが見つかりません");
            return;
        }

        button.onClick.AddListener(OnButtonClicked);

        // Backdropの初期設定
        InitializeBackdrop();

        // 元の位置を保存
        SaveOriginalPositions();
    }

    /// <summary>
    /// Backdropを初期化します
    /// </summary>
    private void InitializeBackdrop()
    {
        if (backdropObject != null)
        {
            backdropCanvasGroup = backdropObject.GetComponent<CanvasGroup>();
            if (backdropCanvasGroup == null)
            {
                backdropCanvasGroup = backdropObject.AddComponent<CanvasGroup>();
            }
            backdropCanvasGroup.alpha = 0f;
            backdropObject.SetActive(false);
            isBackdropVisible = false;

            // BackdropにButtonコンポーネントを追加または取得
            backdropButton = backdropObject.GetComponent<Button>();
            if (backdropButton == null)
            {
                backdropButton = backdropObject.AddComponent<Button>();
            }

            // Backdropクリック時に閉じる処理を登録
            backdropButton.onClick.AddListener(OnBackdropClicked);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }

        if (backdropButton != null)
        {
            backdropButton.onClick.RemoveListener(OnBackdropClicked);
        }

        // すべてのアニメーションを停止
        KillAllTweens();
        backdropTween?.Kill();
    }

    /// <summary>
    /// 元の位置を保存します
    /// </summary>
    private void SaveOriginalPositions()
    {
        foreach (var data in animationObjects)
        {
            if (data != null && data.gameObject != null)
            {
                RectTransform rectTransform = data.gameObject.GetComponent<RectTransform>();
                if (rectTransform != null && !originalPositions.ContainsKey(data.gameObject))
                {
                    originalPositions[data.gameObject] = rectTransform.anchoredPosition;
                }
            }
        }
    }

    /// <summary>
    /// ボタンがクリックされたときに呼ばれます
    /// </summary>
    private void OnButtonClicked()
    {
        if (isAnimating)
        {
            Debug.LogWarning("StageSelectOpener: アニメーション実行中です");
            return;
        }

        if (isBackdropVisible)
        {
            // Backdropが表示されている場合は閉じる（フェードアウト）
            CloseBackdrop();
        }
        else
        {
            // Backdropが表示されていない場合は開く（フェードイン）
            OpenBackdrop();
        }
    }

    /// <summary>
    /// Backdropを開きます（フェードイン）
    /// </summary>
    private void OpenBackdrop()
    {
        if (backdropObject == null) return;

        isBackdropVisible = true;
        isAnimating = true;
        KillAllTweens();

        // Backdropを有効化
        backdropObject.SetActive(true);
        backdropCanvasGroup.alpha = 0f;

        // フェードインアニメーション
        backdropTween?.Kill();
        backdropTween = backdropCanvasGroup.DOFade(1f, backdropFadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // フェードイン完了後、オブジェクトをアニメーション
                StartCoroutine(AnimateObjectsCoroutine());
            });
    }

    /// <summary>
    /// Backdropがクリックされたときに呼ばれます
    /// </summary>
    private void OnBackdropClicked()
    {
        if (isAnimating || !isBackdropVisible)
        {
            return;
        }

        CloseBackdrop();
    }

    /// <summary>
    /// Backdropを閉じます（フェードアウトとオブジェクトの逆アニメーション）
    /// </summary>
    public void CloseBackdrop()
    {
        if (backdropObject == null) return;

        isBackdropVisible = false;
        isAnimating = true;
        KillAllTweens();

        // オブジェクトを元の位置に戻すアニメーション（逆操作）
        StartCoroutine(ReverseAnimateObjectsCoroutine());
    }

    /// <summary>
    /// オブジェクトを逆順にアニメーションして元の位置に戻すコルーチン
    /// </summary>
    private IEnumerator ReverseAnimateObjectsCoroutine()
    {
        // オブジェクトを逆順に処理（最後から最初へ）
        for (int i = animationObjects.Count - 1; i >= 0; i--)
        {
            var data = animationObjects[i];
            if (data == null || data.gameObject == null) continue;

            // 前回のオブジェクトアニメーション後の待機時間（アニメーション時間は含めない）
            if (data.waitAfterPrevious > 0f)
            {
                yield return new WaitForSeconds(data.waitAfterPrevious);
            }

            // オブジェクトを元の位置に戻すアニメーション
            ReverseAnimateObject(data);
        }

        // すべてのオブジェクトが元の位置に戻ったら、Backdropをフェードアウト
        yield return new WaitForSeconds(GetMaxAnimationDuration());

        backdropTween?.Kill();
        backdropTween = backdropCanvasGroup.DOFade(0f, backdropFadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                backdropObject.SetActive(false);
                isAnimating = false;
            });
    }

    /// <summary>
    /// 個別のオブジェクトを元の位置に戻すアニメーション
    /// </summary>
    private void ReverseAnimateObject(AnimationObjectData data)
    {
        GameObject obj = data.gameObject;
        if (obj == null) return;

        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // 元の位置を取得
        if (!originalPositions.ContainsKey(obj))
        {
            Debug.LogWarning($"StageSelectOpener: {obj.name}の元の位置が見つかりません");
            return;
        }

        Vector2 originalPos = originalPositions[obj];

        // 元の位置に戻すアニメーション
        Tween tween = rectTransform.DOAnchorPos(originalPos, data.animationDuration)
            .SetEase(data.easing);

        activeTweens.Add(tween);
    }

    /// <summary>
    /// 最大のアニメーション時間を取得します
    /// </summary>
    private float GetMaxAnimationDuration()
    {
        float maxDuration = 0f;
        foreach (var data in animationObjects)
        {
            if (data != null && data.animationDuration > maxDuration)
            {
                maxDuration = data.animationDuration;
            }
        }
        return maxDuration;
    }

    /// <summary>
    /// すべてのオブジェクトをアニメーションします
    /// </summary>
    public void AnimateObjects()
    {
        if (isAnimating) return;

        isAnimating = true;
        KillAllTweens();

        StartCoroutine(AnimateObjectsCoroutine());
    }

    /// <summary>
    /// オブジェクトを順番にアニメーションするコルーチン
    /// </summary>
    private IEnumerator AnimateObjectsCoroutine()
    {
        foreach (var data in animationObjects)
        {
            if (data == null || data.gameObject == null) continue;

            // 前回のオブジェクトアニメーション後の待機時間（アニメーション時間は含めない）
            if (data.waitAfterPrevious > 0f)
            {
                yield return new WaitForSeconds(data.waitAfterPrevious);
            }

            // オブジェクトをアニメーション（アニメーション時間は待たない）
            AnimateObject(data);
        }

        isAnimating = false;
    }

    /// <summary>
    /// 個別のオブジェクトをアニメーションします
    /// </summary>
    private void AnimateObject(AnimationObjectData data)
    {
        GameObject obj = data.gameObject;
        if (obj == null) return;

        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning($"StageSelectOpener: {obj.name}にRectTransformが見つかりません");
            return;
        }

        // 元の位置を取得（保存されていない場合は現在の位置を使用）
        Vector2 originalPos;
        if (!originalPositions.ContainsKey(obj))
        {
            originalPos = rectTransform.anchoredPosition;
            originalPositions[obj] = originalPos;
        }
        else
        {
            originalPos = originalPositions[obj];
        }

        // 目標位置を計算
        Vector2 targetPosition = GetTargetPosition(originalPos, data.direction, data.moveOffset);

        // アニメーションを実行
        Tween tween = rectTransform.DOAnchorPos(targetPosition, data.animationDuration)
            .SetEase(data.easing);

        activeTweens.Add(tween);
    }

    /// <summary>
    /// 方向と移動量から目標位置を取得します
    /// </summary>
    private Vector2 GetTargetPosition(Vector2 originalPosition, AnimationDirection direction, float offset)
    {
        switch (direction)
        {
            case AnimationDirection.Up:
                return originalPosition + new Vector2(0f, offset);
            case AnimationDirection.Down:
                return originalPosition + new Vector2(0f, -offset);
            case AnimationDirection.Left:
                return originalPosition + new Vector2(-offset, 0f);
            case AnimationDirection.Right:
                return originalPosition + new Vector2(offset, 0f);
            default:
                return originalPosition;
        }
    }

    /// <summary>
    /// すべてのアニメーションを停止します
    /// </summary>
    private void KillAllTweens()
    {
        foreach (var tween in activeTweens)
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }
        }
        activeTweens.Clear();
    }

    /// <summary>
    /// すべてのオブジェクトを元の位置に戻します
    /// </summary>
    public void ResetPositions()
    {
        KillAllTweens();
        isAnimating = false;

        foreach (var data in animationObjects)
        {
            if (data == null || data.gameObject == null) continue;

            RectTransform rectTransform = data.gameObject.GetComponent<RectTransform>();
            if (rectTransform != null && originalPositions.ContainsKey(data.gameObject))
            {
                rectTransform.anchoredPosition = originalPositions[data.gameObject];
            }
        }
    }
}

