using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TutorialOperator : MonoBehaviour
{
    [Header("Page Settings")]
    [Tooltip("チュートリアルページのゲームオブジェクトリスト")]
    [SerializeField] private List<GameObject> tutorialPages = new List<GameObject>();

    [Header("UI References")]
    [Tooltip("前のページへ戻るボタン")]
    [SerializeField] private Button prevButton;

    [Tooltip("次のページへ進むボタン")]
    [SerializeField] private Button nextButton;

    [Tooltip("現在のページ数を表示するテキスト（分子）")]
    [SerializeField] private TMP_Text currentPageText;

    [Tooltip("全体のページ数を表示するテキスト（分母）")]
    [SerializeField] private TMP_Text totalPageText;

    private int currentPageIndex = 0;

    private void Start()
    {
        // ボタンのイベントを設定
        if (prevButton != null)
        {
            prevButton.onClick.AddListener(OnPrevButtonClicked);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        // 初期ページを表示（ページがまだ無い場合もあるため安全に）
        ShowFirstPage();
    }

    private void OnDestroy()
    {
        // ボタンのイベントを解除
        if (prevButton != null)
        {
            prevButton.onClick.RemoveListener(OnPrevButtonClicked);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextButtonClicked);
        }
    }

    /// <summary>
    /// 前のページボタンがクリックされたときの処理
    /// </summary>
    private void OnPrevButtonClicked()
    {
        if (currentPageIndex > 0)
        {
            ShowPage(currentPageIndex - 1);
        }
    }

    /// <summary>
    /// 次のページボタンがクリックされたときの処理
    /// </summary>
    private void OnNextButtonClicked()
    {
        if (currentPageIndex < tutorialPages.Count - 1)
        {
            ShowPage(currentPageIndex + 1);
        }
    }

    /// <summary>
    /// 指定されたページを表示します
    /// </summary>
    private void ShowPage(int pageIndex)
    {
        // インデックスの範囲チェック
        if (pageIndex < 0 || pageIndex >= tutorialPages.Count)
        {
            Debug.LogWarning($"TutorialOperator: 無効なページインデックス {pageIndex} が指定されました");
            return;
        }

        // 切替前に、非表示になるページの動画を「先頭停止＋マスク表示」に戻す
        for (int i = 0; i < tutorialPages.Count; i++)
        {
            if (tutorialPages[i] != null)
            {
                if (i != pageIndex)
                {
                    var moviePlayers = tutorialPages[i].GetComponentsInChildren<MoviePlayer>(true);
                    for (int m = 0; m < moviePlayers.Length; m++)
                    {
                        if (moviePlayers[m] != null)
                        {
                            moviePlayers[m].ResetToInitialState();
                        }
                    }
                }
            }
        }

        // すべてのページを非表示にする（指定ページのみ表示）
        for (int i = 0; i < tutorialPages.Count; i++)
        {
            if (tutorialPages[i] != null)
            {
                tutorialPages[i].SetActive(i == pageIndex);
            }
        }

        // 表示されたページも初期状態（マスク表示）に戻す
        if (tutorialPages[pageIndex] != null)
        {
            var moviePlayers = tutorialPages[pageIndex].GetComponentsInChildren<MoviePlayer>(true);
            for (int m = 0; m < moviePlayers.Length; m++)
            {
                if (moviePlayers[m] != null)
                {
                    moviePlayers[m].ResetToInitialState();
                }
            }
        }

        // 現在のページインデックスを更新
        currentPageIndex = pageIndex;

        // ボタンの有効/無効状態を更新
        UpdateButtonStates();

        // ページテキストを更新
        UpdatePageText();
    }

    /// <summary>
    /// ボタンの有効/無効状態を更新します
    /// </summary>
    private void UpdateButtonStates()
    {
        if (prevButton != null)
        {
            prevButton.interactable = currentPageIndex > 0;
        }

        if (nextButton != null)
        {
            nextButton.interactable = currentPageIndex < tutorialPages.Count - 1;
        }
    }

    /// <summary>
    /// ページテキストを更新します
    /// </summary>
    private void UpdatePageText()
    {
        if (currentPageText != null)
        {
            currentPageText.text = (currentPageIndex + 1).ToString();
        }

        if (totalPageText != null)
        {
            totalPageText.text = tutorialPages.Count.ToString();
        }
    }

    /// <summary>
    /// チュートリアルページを追加します（HintPageGeneratorから呼び出されます）
    /// </summary>
    public void AddTutorialPage(GameObject page)
    {
        if (page != null && !tutorialPages.Contains(page))
        {
            tutorialPages.Add(page);
            Debug.Log($"TutorialOperator: ページを追加しました（総数: {tutorialPages.Count}）");
        }
    }

    /// <summary>
    /// すべてのチュートリアルページをクリアします
    /// </summary>
    public void ClearTutorialPages()
    {
        tutorialPages.Clear();
        Debug.Log("TutorialOperator: すべてのページをクリアしました");
    }

    /// <summary>
    /// 1ページ目を表示します（ページが無ければ何もしません）
    /// </summary>
    public void ShowFirstPage()
    {
        if (tutorialPages.Count > 0)
        {
            ShowPage(0);
        }
        else
        {
            // ページがまだ無い場合でもUIは更新しておく
            UpdateButtonStates();
            UpdatePageText();
        }
    }
}
