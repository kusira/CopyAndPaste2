using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ヒントページを生成するスクリプト
/// </summary>
public class HintPageGenerator : MonoBehaviour
{
    [System.Serializable]
    public class StageVideoClipList
    {
        [Tooltip("このステージで使用する動画ファイル名リスト（拡張子なし。例: \"1-1-1\"）")]
        public List<string> videoFileNames = new List<string>();
    }

    [Header("Prefab")]
    [Tooltip("HintPageのPrefab")]
    [SerializeField] private GameObject hintPagePrefab;

    [Header("参照")]
    [Tooltip("CurrentGameStatus（未設定の場合は自動検索します）")]
    [SerializeField] private CurrentGameStatus currentGameStatus;

    [Header("動画ファイル")]
    [Tooltip("インデックス=ステージ番号。各要素の中に、そのステージの動画リストを入れてください")]
    [SerializeField] private List<StageVideoClipList> stageVideoClips = new List<StageVideoClipList>();

    [Header("動画ファイル（デフォルト）")]
    [Tooltip("ステージ別リストが未設定/範囲外のときに使う動画ファイル名リスト（拡張子なし）")]
    [SerializeField] private List<string> defaultVideoFileNames = new List<string>();

    [Header("生成設定")]
    [Tooltip("生成するHintPageの座標（デフォルト: 0, -0.25）")]
    [SerializeField] private Vector2 pagePosition = new Vector2(0f, -0.25f);
    
    [Tooltip("ページの親オブジェクト（未設定の場合はこのGameObjectが親になります）")]
    [SerializeField] private Transform pageParent;

    [Header("TutorialOperator")]
    [Tooltip("TutorialOperator（設定すると生成したHintPageが自動的に登録されます）")]
    [SerializeField] private TutorialOperator tutorialOperator;
    
    [Header("ボタン")]
    [Tooltip("動画がない場合に非活性にするボタン")]
    [SerializeField] private Button hintButton;

    private List<GameObject> generatedPages = new List<GameObject>();

    private void Start()
    {
        // CurrentGameStatusが未設定なら自動検索（Unity6ルール: FindFirstObjectByType）
        if (currentGameStatus == null)
        {
            currentGameStatus = Object.FindFirstObjectByType<CurrentGameStatus>();
            if (currentGameStatus == null)
            {
                Debug.LogWarning("HintPageGenerator: CurrentGameStatusが見つかりませんでした（デフォルト動画リストを使用します）");
            }
        }

        // Start時に自動的にヒントページを生成
        GenerateHintPages();
        
        // ボタンの活性/非活性を設定
        UpdateButtonState();
    }
    
    /// <summary>
    /// ボタンの活性/非活性を更新します
    /// </summary>
    private void UpdateButtonState()
    {
        if (hintButton == null)
        {
            return;
        }
        
        List<string> videoFileNames = ResolveVideoFileNamesForCurrentStage();
        bool hasVideos = videoFileNames != null && videoFileNames.Count > 0;
        
        hintButton.gameObject.SetActive(hasVideos);
        Debug.Log($"HintPageGenerator: ボタンの状態を更新しました（動画数: {videoFileNames?.Count ?? 0}, 活性: {hasVideos}）");
    }

    /// <summary>
    /// 動画ファイル名リストを取得します
    /// </summary>
    private List<string> ResolveVideoFileNamesForCurrentStage()
    {
        int stageIndex = -1;
        if (currentGameStatus != null)
        {
            stageIndex = currentGameStatus.GetCurrentStageIndex();
        }

        if (stageIndex >= 0 && stageIndex < stageVideoClips.Count)
        {
            var list = stageVideoClips[stageIndex];
            if (list != null && list.videoFileNames != null && list.videoFileNames.Count > 0)
            {
                return list.videoFileNames;
            }
        }

        return defaultVideoFileNames;
    }

    /// <summary>
    /// ヒントページを生成します
    /// </summary>
    [ContextMenu("ヒントページを生成")]
    public void GenerateHintPages()
    {
        // 既存のページを削除
        ClearHintPages();

        if (hintPagePrefab == null)
        {
            Debug.LogError("HintPageGenerator: HintPagePrefabが設定されていません");
            return;
        }

        List<string> videoFileNamesToUse = ResolveVideoFileNamesForCurrentStage();
        
        if (videoFileNamesToUse == null || videoFileNamesToUse.Count == 0)
        {
            Debug.LogWarning("HintPageGenerator: 動画ファイルが設定されていません");
            return;
        }

        // TutorialOperatorが設定されている場合はクリア
        if (tutorialOperator != null)
        {
            tutorialOperator.ClearTutorialPages();
        }

        // 各動画ファイルに対してHintPageを生成
        for (int i = 0; i < videoFileNamesToUse.Count; i++)
        {
            string videoFileName = videoFileNamesToUse[i];
            
            if (string.IsNullOrEmpty(videoFileName))
            {
                Debug.LogWarning($"HintPageGenerator: インデックス {i} の動画ファイル名が設定されていません");
                continue;
            }

            CreateHintPage(i, videoFileName);
        }

        // 最初のページだけ表示（それ以外は非表示のまま）
        if (tutorialOperator != null)
        {
            tutorialOperator.ShowFirstPage();
        }
        else if (generatedPages.Count > 0)
        {
            for (int i = 0; i < generatedPages.Count; i++)
            {
                if (generatedPages[i] != null)
                {
                    generatedPages[i].SetActive(i == 0);
                }
            }
        }

        Debug.Log($"HintPageGenerator: {generatedPages.Count}個のヒントページを生成しました");
    }

    /// <summary>
    /// ヒントページを1つ作成します
    /// </summary>
    private void CreateHintPage(int index, string videoFileName)
    {
        // 親オブジェクトを決定
        Transform parentTransform = pageParent != null ? pageParent : transform;
        
        // Prefabをインスタンス化
        GameObject hintPage = Instantiate(hintPagePrefab, parentTransform);
        hintPage.name = $"HintPage_{index + 1}";

        // 座標を設定
        RectTransform rectTransform = hintPage.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = pagePosition;
        }
        else
        {
            hintPage.transform.localPosition = new Vector3(pagePosition.x, pagePosition.y, 0f);
        }

        // Headingを取得してテキストを設定
        Transform headingTransform = hintPage.transform.Find("Heading");
        if (headingTransform != null)
        {
            TextMeshProUGUI headingText = headingTransform.GetComponent<TextMeshProUGUI>();
            if (headingText != null)
            {
                headingText.text = $"■ 解答動画: {index + 1}手目";
            }
            else
            {
                // TextMeshProUGUIがない場合はTextコンポーネントを試す
                Text textComponent = headingTransform.GetComponent<Text>();
                if (textComponent != null)
                {
                    textComponent.text = $"{index + 1}手目";
                }
                else
                {
                    Debug.LogWarning($"HintPageGenerator: HeadingにTextMeshProUGUIまたはTextコンポーネントが見つかりません（インデックス: {index}）");
                }
            }
        }
        else
        {
            Debug.LogWarning($"HintPageGenerator: Headingが見つかりません（インデックス: {index}）");
        }

        // MovieWrapperを取得してMoviePlayerを設定
        Transform movieWrapperTransform = hintPage.transform.Find("MovieWrapper");
        if (movieWrapperTransform != null)
        {
            MoviePlayer moviePlayer = movieWrapperTransform.GetComponent<MoviePlayer>();
            if (moviePlayer != null)
            {
                SetMoviePlayerVideo(moviePlayer, movieWrapperTransform, videoFileName);
            }
            else
            {
                Debug.LogWarning($"HintPageGenerator: MovieWrapperにMoviePlayerコンポーネントが見つかりません（インデックス: {index}）");
            }
        }
        else
        {
            Debug.LogWarning($"HintPageGenerator: MovieWrapperが見つかりません（インデックス: {index}）");
        }

        // 生成直後は非表示（TutorialOperatorが表示制御する）
        hintPage.SetActive(false);

        generatedPages.Add(hintPage);

        // TutorialOperatorが設定されている場合は登録
        if (tutorialOperator != null)
        {
            tutorialOperator.AddTutorialPage(hintPage);
        }
    }

    /// <summary>
    /// MoviePlayerに動画を設定します
    /// </summary>
    private void SetMoviePlayerVideo(MoviePlayer moviePlayer, Transform movieWrapperTransform, string videoFileName)
    {
        // MovieとMaskを取得
        Transform movieTransform = movieWrapperTransform.Find("Movie");
        Transform maskTransform = movieWrapperTransform.Find("Mask");

        if (movieTransform == null || maskTransform == null)
        {
            Debug.LogWarning("HintPageGenerator: MovieまたはMaskが見つかりません");
            return;
        }

        RawImage movieRawImage = movieTransform.GetComponent<RawImage>();
        GameObject maskGameObject = maskTransform.gameObject;

        if (movieRawImage == null)
        {
            Debug.LogWarning("HintPageGenerator: MovieにRawImageコンポーネントが見つかりません");
            return;
        }

        // MoviePlayerのSetupVideoメソッドを呼び出し
        moviePlayer.SetupVideo(null, movieRawImage, maskGameObject, videoFileName);
    }

    /// <summary>
    /// 生成されたヒントページをすべて削除します
    /// </summary>
    [ContextMenu("ヒントページをクリア")]
    public void ClearHintPages()
    {
        foreach (GameObject page in generatedPages)
        {
            if (page != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(page);
                }
                else
                {
                    Destroy(page);
                }
#else
                Destroy(page);
#endif
            }
        }
        generatedPages.Clear();
        Debug.Log("HintPageGenerator: 生成されたヒントページをクリアしました");
    }

    private void OnDestroy()
    {
        ClearHintPages();
    }
}

