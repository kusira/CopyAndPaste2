using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーンのフェードを管理するスクリプト
/// </summary>
public class FadeManager : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("フェードに使用するUnmaskゲームオブジェクトをアサインします")]
    [SerializeField] private GameObject unmask;

    [Header("設定")]
    [Tooltip("アニメーションの時間（秒）")]
    [SerializeField] private float animationDuration = 0.5f;

    [Tooltip("フェードの最終サイズ")]
    [SerializeField] private float fadeSize = 35f;

    [Tooltip("フェードアウト後のシーン遷移までの待機時間（秒）")]
    [SerializeField] private float sceneTransitionDelay = 0.3f;

    [Header("回転設定")]
    [Tooltip("フェードイン・フェードアウト時に回転させるかどうか")]
    [SerializeField] private bool enableRotation = true;

    [Tooltip("フェードイン・フェードアウト時の回転角度（度）")]
    [SerializeField] private float rotationAngle = 360f;

    private RectTransform unmaskRectTransform;
    private Tween fadeTween;
    private bool isFadingIn;

    private void Start()
    {
        if (unmask == null)
        {
            Debug.LogError("FadeManager: Unmaskがアサインされていません");
            return;
        }

        unmaskRectTransform = unmask.GetComponent<RectTransform>();
        if (unmaskRectTransform == null)
        {
            Debug.LogError("FadeManager: UnmaskにRectTransformがアタッチされていません");
            return;
        }

        // Start時にフェードインから始まる
        FadeIn();
    }

    private void OnDestroy()
    {
        fadeTween?.Kill();
    }

    /// <summary>
    /// フェードイン（サイズ0→35、その後SetActive(false)）
    /// </summary>
    public void FadeIn()
    {
        if (unmask == null || unmaskRectTransform == null) return;

        // 既にフェードイン中なら実行しない
        if (isFadingIn)
        {
            Debug.Log("FadeManager: フェードインは既に実行中です");
            return;
        }

        isFadingIn = true;

        // オブジェクトを有効化
        unmask.SetActive(true);

        // サイズを0に設定
        unmaskRectTransform.localScale = Vector3.zero;
        // 回転を0にリセット
        unmaskRectTransform.localRotation = Quaternion.identity;

        // 既存のアニメーションを停止
        fadeTween?.Kill();

        // サイズと回転を同時にアニメーション
        Sequence sequence = DOTween.Sequence();
        sequence.Append(unmaskRectTransform.DOScale(Vector3.one * fadeSize, animationDuration).SetEase(Ease.OutQuad));
        
        // 回転が有効な場合のみ回転アニメーションを追加（フェードイン時は前半が緩やか）
        if (enableRotation)
        {
            sequence.Join(unmaskRectTransform.DORotate(new Vector3(0, 0, rotationAngle), animationDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad));
        }
        
        sequence.OnComplete(() =>
        {
            Debug.Log("フェードイン完了");
            isFadingIn = false;
        });

        fadeTween = sequence;
    }

    /// <summary>
    /// フェードアウト（サイズ35→0）
    /// </summary>
    public void FadeOut()
    {
        FadeOut(null);
    }

    /// <summary>
    /// フェードアウト（サイズ35→0、360度回転、コールバック付き）
    /// </summary>
    /// <param name="onComplete">アニメーション完了時のコールバック</param>
    public void FadeOut(System.Action onComplete)
    {
        if (unmask == null || unmaskRectTransform == null) return;

        // オブジェクトを有効化
        unmask.SetActive(true);

        // サイズを35に設定（フェードインの逆）
        unmaskRectTransform.localScale = Vector3.one * fadeSize;
        // 回転を0にリセット
        unmaskRectTransform.localRotation = Quaternion.identity;

        // 既存のアニメーションを停止
        fadeTween?.Kill();

        // サイズと回転を同時にアニメーション
        Sequence sequence = DOTween.Sequence();
        sequence.Append(unmaskRectTransform.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InQuad));
        
        // 回転が有効な場合のみ回転アニメーションを追加（フェードアウト時は後半が緩やか）
        if (enableRotation)
        {
            sequence.Join(unmaskRectTransform.DORotate(new Vector3(0, 0, rotationAngle), animationDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
        }
        
        sequence.OnComplete(() =>
        {
            Debug.Log("フェードアウト完了");
            onComplete?.Invoke();
        });

        fadeTween = sequence;
    }

    /// <summary>
    /// フェードアウトしてシーン遷移（シーン名指定）
    /// </summary>
    /// <param name="sceneName">遷移先のシーン名</param>
    public void FadeOutAndLoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("FadeManager: シーン名が指定されていません");
            return;
        }

        FadeOut(() =>
        {
            // 待機時間後にシーン遷移
            DOVirtual.DelayedCall(sceneTransitionDelay, () =>
            {
                SceneManager.LoadScene(sceneName);
                Debug.Log($"シーン遷移: {sceneName}");
            });
        });
    }

    /// <summary>
    /// フェードアウトしてシーン遷移（ビルドインデックス指定）
    /// </summary>
    /// <param name="buildIndex">遷移先のシーンのビルドインデックス</param>
    public void FadeOutAndLoadScene(int buildIndex)
    {
        if (buildIndex < 0)
        {
            Debug.LogError("FadeManager: ビルドインデックスが無効です");
            return;
        }

        FadeOut(() =>
        {
            // 待機時間後にシーン遷移
            DOVirtual.DelayedCall(sceneTransitionDelay, () =>
            {
                SceneManager.LoadScene(buildIndex);
                Debug.Log($"シーン遷移: ビルドインデックス {buildIndex}");
            });
        });
    }
}

