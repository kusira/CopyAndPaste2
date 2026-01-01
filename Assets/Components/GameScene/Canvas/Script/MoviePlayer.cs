using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 動画再生をコントロールするスクリプト
/// </summary>
public class MoviePlayer : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("動画を表示するRawImage")]
    [SerializeField] private RawImage hintMovie;

    [Tooltip("クリック可能なマスクオブジェクト（Buttonコンポーネントが必要）")]
    [SerializeField] private GameObject hintMask;

    [Tooltip("VideoPlayerコンポーネント（未設定の場合は自動検索します）")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Tooltip("TutorialOperator（任意）")]
    [SerializeField] private TutorialOperator tutorialOperator;

    private Button hintMaskButton;
    private bool isPlaying = false;
    private RenderTexture runtimeRenderTexture;

    private void Awake()
    {
        // NOTE:
        // ここで FindFirstObjectByType<VideoPlayer>() を使うと、別ページのVideoPlayerを掴んでしまい
        // 「最後に設定した動画だけ表示/他が透明」などの原因になります。
        // このMoviePlayer自身(配下)のVideoPlayerのみを参照します。
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = GetComponentInChildren<VideoPlayer>(true);
            }
        }

        // VideoPlayerのイベントを設定
        if (videoPlayer != null)
        {
            // Direct出力の警告を避ける（音が必要ならAudioSourceモードに変更してください）
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.loopPointReached -= OnVideoEnd;
            videoPlayer.loopPointReached += OnVideoEnd;
        }

        // HintMaskのButtonコンポーネントを取得
        if (hintMask != null)
        {
            hintMaskButton = hintMask.GetComponent<Button>();
            if (hintMaskButton == null)
            {
                Debug.LogWarning("MoviePlayer: HintMaskにButtonコンポーネントが見つかりませんでした");
            }
        }
    }

    private void Start()
    {
        // 初期状態で動画を停止
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            isPlaying = false;
            Debug.Log("動画を初期停止状態に設定しました");
        }
    }

    private void OnEnable()
    {
        // ButtonのonClickイベントに登録
        if (hintMaskButton != null)
        {
            hintMaskButton.onClick.AddListener(PlayVideo);
        }

        // 表示されたタイミングでは常に「マスク表示＋先頭停止」に戻す
        ResetToInitialState();
    }

    private void OnDisable()
    {
        // 非活性化で再生がバグりやすいので、先に状態を巻き戻す
        ResetToInitialState();

        // ButtonのonClickイベントから解除
        if (hintMaskButton != null)
        {
            hintMaskButton.onClick.RemoveListener(PlayVideo);
        }
    }

    private void OnDestroy()
    {
        // イベントの解除
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEnd;
        }

        // ButtonのonClickイベントから解除
        if (hintMaskButton != null)
        {
            hintMaskButton.onClick.RemoveListener(PlayVideo);
        }

        // RenderTextureの解放
        if (runtimeRenderTexture != null)
        {
            runtimeRenderTexture.Release();
            Destroy(runtimeRenderTexture);
            runtimeRenderTexture = null;
        }
    }

    /// <summary>
    /// 動画を再生します
    /// </summary>
    public void PlayVideo()
    {
        if (isPlaying) return;

        if (videoPlayer == null)
        {
            Debug.LogWarning("MoviePlayer: VideoPlayerが設定されていません");
            return;
        }

        if (hintMovie == null)
        {
            Debug.LogWarning("MoviePlayer: HintMovieが設定されていません");
            return;
        }

        // 動画を再生
        videoPlayer.Play();
        isPlaying = true;

        // HintMaskを非活性化
        if (hintMask != null)
        {
            hintMask.SetActive(false);
            Debug.Log("動画再生開始：HintMaskを非活性化しました");
        }
    }

    /// <summary>
    /// 動画終了時のコールバック
    /// </summary>
    private void OnVideoEnd(VideoPlayer source)
    {
        isPlaying = false;

        // HintMaskを再び活性化
        if (hintMask != null)
        {
            hintMask.SetActive(true);
            Debug.Log("動画再生終了：HintMaskを再び活性化しました");
        }
    }

    /// <summary>
    /// 動画を停止します
    /// </summary>
    public void StopVideo()
    {
        if (videoPlayer != null && isPlaying)
        {
            videoPlayer.Stop();
            isPlaying = false;

            // HintMaskを再び活性化
            if (hintMask != null)
            {
                hintMask.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 「マスク表示＋先頭停止」の初期状態に戻します（ページ切替対策）
    /// </summary>
    public void ResetToInitialState()
    {
        // マスクを必ず表示に戻す
        if (hintMask != null)
        {
            hintMask.SetActive(true);
        }

        // 動画を停止し先頭へ戻す
        if (videoPlayer != null)
        {
            // Stopで多くの場合0に戻るが、念のため明示
            videoPlayer.Stop();
            try
            {
                videoPlayer.time = 0;
            }
            catch
            {
                // 一部バックエンドで例外になっても無視
            }
            videoPlayer.frame = 0;
        }

        isPlaying = false;
    }

    /// <summary>
    /// 動画を設定します（HintPageGeneratorから呼び出されます）
    /// </summary>
    public void SetupVideo(VideoClip clip, RawImage movieRawImage, GameObject maskGameObject)
    {
        if (clip == null || movieRawImage == null || maskGameObject == null)
        {
            Debug.LogWarning("MoviePlayer: SetupVideoの引数が無効です");
            return;
        }

        hintMovie = movieRawImage;
        hintMask = maskGameObject;

        // もし別オブジェクトのVideoPlayerを掴んでいたら、必ず自分用を作る
        if (videoPlayer != null && !videoPlayer.transform.IsChildOf(transform))
        {
            Debug.LogWarning("MoviePlayer: 別オブジェクトのVideoPlayerを参照していたため、自分用のVideoPlayerを作成します");
            videoPlayer = null;
        }

        // VideoPlayerを取得または作成（自分の子にぶら下げる）
        if (videoPlayer == null)
        {
            GameObject videoPlayerObj = new GameObject("VideoPlayer_" + clip.name);
            videoPlayerObj.transform.SetParent(transform);
            videoPlayer = videoPlayerObj.AddComponent<VideoPlayer>();
        }

        // 既存のRenderTextureがあれば解放
        if (runtimeRenderTexture != null)
        {
            runtimeRenderTexture.Release();
            Destroy(runtimeRenderTexture);
            runtimeRenderTexture = null;
        }

        // RenderTextureを作成（ARGB32フォーマットで不透明に）
        int w = (int)clip.width;
        int h = (int)clip.height;
        if (w <= 0) w = 1920;
        if (h <= 0) h = 1080;
        runtimeRenderTexture = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        runtimeRenderTexture.name = "RenderTexture_" + clip.name;
        runtimeRenderTexture.Create();

        // VideoPlayerの設定
        videoPlayer.clip = clip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = runtimeRenderTexture;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        
        // オーディオ出力モードをNoneに設定（Directモードのエラーを回避）
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

        // RawImageにRenderTextureを設定
        hintMovie.texture = runtimeRenderTexture;
        
        // RawImageのColorを不透明に設定（透明にならないように）
        hintMovie.color = Color.white;

        // Buttonコンポーネントを取得
        hintMaskButton = hintMask.GetComponent<Button>();
        if (hintMaskButton == null)
        {
            Debug.LogWarning("MoviePlayer: HintMaskにButtonコンポーネントが見つかりませんでした");
        }

        // VideoPlayerのイベントを設定（重複登録防止）
        videoPlayer.loopPointReached -= OnVideoEnd;
        videoPlayer.loopPointReached += OnVideoEnd;

        // 初期状態は停止
        videoPlayer.Stop();
        isPlaying = false;
    }
}

