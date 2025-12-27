using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

/// <summary>
/// すべてのProgressがAcquiredになったときにリザルトを表示するクラス
/// </summary>
public class ResultShower : MonoBehaviour
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
    }
    [Header("Result Objects")]
    [Tooltip("背景用のBackdropオブジェクトをアサインします")]
    [SerializeField] private GameObject backdropObject;

    [Header("Global Volume")]
    [Tooltip("GlobalVolumeをアサインします")]
    [SerializeField] private Volume globalVolume;

    [Header("Animation Timing")]
    [Tooltip("step1: 初期待機時間（秒）")]
    [SerializeField] private float initialWaitSeconds = 0f;

    [Tooltip("step2: DOFアニメーション時間（秒）")]
    [SerializeField] private float dofAnimationDuration = 0.3f;

    [Tooltip("step2: DOFの最大Focal Length")]
    [SerializeField] private float dofMaxFocalLength = 30f;

    [Header("Pre-Animation Objects")]
    [Tooltip("アニメーションを行う前に有効にするゲームオブジェクトのリスト")]
    [SerializeField] private List<GameObject> preAnimationObjects = new List<GameObject>();

    [Header("UI Animation Items")]
    [Tooltip("各UI要素のアニメーション設定")]
    [SerializeField] private List<AnimationItemData> animationItems = new List<AnimationItemData>();

    private bool isShown = false;
    private bool isResultShowing = false;
    private DepthOfField dofEffect;
    private Dictionary<GameObject, Vector2> originalPositions = new Dictionary<GameObject, Vector2>();
    private bool isBackdropFadedIn = false;

    private void Start()
    {
        // GlobalVolumeからDOFエフェクトを取得
        if (globalVolume != null && globalVolume.profile != null)
        {
            if (globalVolume.profile.TryGet<DepthOfField>(out var dof))
            {
                dofEffect = dof;
                // Focal LengthのOverrideStateを有効化
                if (!dofEffect.focalLength.overrideState)
                {
                    dofEffect.focalLength.overrideState = true;
                }
            }
            else
            {
                Debug.LogWarning("ResultShower: Global VolumeにDepthOfFieldエフェクトが見つかりません");
            }
        }
    }

    /// <summary>
    /// リザルト表示を開始します（複数回呼ばれても一度だけ表示）
    /// </summary>
    public void ShowResult()
    {
        if (isShown) return;
        isShown = true;
        StartCoroutine(ShowResultRoutine());
    }

    private IEnumerator ShowResultRoutine()
    {
        // step0: すべてのアニメーション対象オブジェクトを非活性化（リセット）
        foreach (var item in animationItems)
        {
            if (item != null && item.gameObject != null)
            {
                item.gameObject.SetActive(false);
            }
        }

        // Backdropを非活性化してリセット
        if (backdropObject != null)
        {
            backdropObject.SetActive(false);
            CanvasGroup backdropCanvasGroup = backdropObject.GetComponent<CanvasGroup>();
            if (backdropCanvasGroup == null)
            {
                backdropCanvasGroup = backdropObject.AddComponent<CanvasGroup>();
            }
            backdropCanvasGroup.alpha = 0f;
        }
        isBackdropFadedIn = false;

        // step1: 任意秒待機
        if (initialWaitSeconds > 0f)
        {
            yield return new WaitForSeconds(initialWaitSeconds);
        }

        // step2: DOFのFocal LengthとBackdropを同時にアニメーション
        Sequence dofAndBackdropSequence = DOTween.Sequence();

        // DOFのFocal Lengthを0から最大値にアニメーション
        if (dofEffect != null)
        {
            dofEffect.focalLength.value = 0f;
            dofAndBackdropSequence.Join(DOTween.To(
                () => dofEffect.focalLength.value,
                x => dofEffect.focalLength.value = x,
                dofMaxFocalLength,
                dofAnimationDuration
            ).SetEase(Ease.OutQuad));
        }

        // Backdropをフェードイン（DOFと同時に実行）
        if (backdropObject != null)
        {
            backdropObject.SetActive(true);
            CanvasGroup backdropCanvasGroup = backdropObject.GetComponent<CanvasGroup>();
            if (backdropCanvasGroup == null)
            {
                backdropCanvasGroup = backdropObject.AddComponent<CanvasGroup>();
            }
            backdropCanvasGroup.alpha = 0f;
            dofAndBackdropSequence.Join(backdropCanvasGroup.DOFade(1f, dofAnimationDuration).SetEase(Ease.OutQuad));
            isBackdropFadedIn = true;
        }

        yield return dofAndBackdropSequence.WaitForCompletion();

        // step2.5: アニメーション前に有効化するオブジェクトを有効化
        foreach (var obj in preAnimationObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        // step3: 各UI要素を順番にアニメーション
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

        isResultShowing = true;

        // リザルト表示中は振動を停止
        if (CharacterVibrator.Instance != null)
        {
            CharacterVibrator.Instance.SetVibrationEnabled(false);
        }

        Debug.Log("リザルトを表示しました");
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
            Debug.LogWarning($"ResultShower: {obj.name}にRectTransformが見つかりません");
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

    /// <summary>
    /// リザルトが表示されているかどうかを取得します
    /// </summary>
    public bool IsResultShowing()
    {
        return isResultShowing;
    }

    /// <summary>
    /// リザルトを非表示にして振動を再開します（必要に応じて外部から呼び出し）
    /// </summary>
    public void HideResult()
    {
        if (!isResultShowing) return;

        isResultShowing = false;
        isShown = false;

        if (backdropObject != null)
        {
            backdropObject.SetActive(false);
        }

        // リザルトが閉じられたら振動を再開
        if (CharacterVibrator.Instance != null)
        {
            CharacterVibrator.Instance.SetVibrationEnabled(true);
        }
    }
}


