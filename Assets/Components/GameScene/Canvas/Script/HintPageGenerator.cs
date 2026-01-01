using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;

/// <summary>
/// ヒントページを生成するスクリプト
/// </summary>
public class HintPageGenerator : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("HintPageのPrefab")]
    [SerializeField] private GameObject hintPagePrefab;

    [Header("動画ファイル")]
    [Tooltip("再生する動画ファイルのリスト")]
    [SerializeField] private List<VideoClip> videoClips = new List<VideoClip>();

    [Header("生成設定")]
    [Tooltip("生成するHintPageの座標（デフォルト: 0, -0.25）")]
    [SerializeField] private Vector2 pagePosition = new Vector2(0f, -0.25f);

    [Header("TutorialOperator")]
    [Tooltip("TutorialOperator（設定すると生成したHintPageが自動的に登録されます）")]
    [SerializeField] private TutorialOperator tutorialOperator;

    private List<GameObject> generatedPages = new List<GameObject>();

    private void Start()
    {
        // Start時に自動的にヒントページを生成
        GenerateHintPages();
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

        if (videoClips == null || videoClips.Count == 0)
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
        for (int i = 0; i < videoClips.Count; i++)
        {
            if (videoClips[i] == null)
            {
                Debug.LogWarning($"HintPageGenerator: インデックス {i} の動画ファイルが設定されていません");
                continue;
            }

            CreateHintPage(i, videoClips[i]);
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
    private void CreateHintPage(int index, VideoClip videoClip)
    {
        // Prefabをインスタンス化
        GameObject hintPage = Instantiate(hintPagePrefab, transform);
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
                headingText.text = $"{index + 1}手目";
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
                SetMoviePlayerVideo(moviePlayer, movieWrapperTransform, videoClip);
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
    private void SetMoviePlayerVideo(MoviePlayer moviePlayer, Transform movieWrapperTransform, VideoClip videoClip)
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
        moviePlayer.SetupVideo(videoClip, movieRawImage, maskGameObject);
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

