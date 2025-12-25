using UnityEngine;
using TMPro;

/// <summary>
/// RS系の入力UIテキストを管理するヘルパークラス
/// </summary>
public class RSInputUIHelper
{
    private TextMeshProUGUI leftClickText;
    private TextMeshProUGUI rightClickText;
    private TextMeshProUGUI mouseWheelText;

    /// <summary>
    /// UIテキスト要素を自動検索で取得します
    /// </summary>
    public void FindUIElements()
    {
        // まずCanvas配下を検索
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null) continue;
            SearchInChildren(canvas.transform);
        }

        // まだ見つからなかった場合は、シーン全体から検索（非アクティブも含む）
        if (leftClickText == null)
        {
            TextMeshProUGUI[] allTMPs = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var tmp in allTMPs)
            {
                if (tmp.name == "LeftClickText")
                {
                    leftClickText = tmp;
                    break;
                }
            }
        }

        if (rightClickText == null)
        {
            TextMeshProUGUI[] allTMPs = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var tmp in allTMPs)
            {
                if (tmp.name == "RightClickText")
                {
                    rightClickText = tmp;
                    break;
                }
            }
        }

        if (mouseWheelText == null)
        {
            TextMeshProUGUI[] allTMPs = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var tmp in allTMPs)
            {
                if (tmp.name == "MauseWheelText" || tmp.name == "MouseWheelText")
                {
                    mouseWheelText = tmp;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 指定されたTransform配下の子要素を再帰的に検索します
    /// </summary>
    private void SearchInChildren(Transform parent)
    {
        if (parent == null) return;

        // このTransformのGameObjectをチェック
        if (parent.name == "LeftClickText" && leftClickText == null)
        {
            leftClickText = parent.GetComponent<TextMeshProUGUI>();
        }
        else if (parent.name == "RightClickText" && rightClickText == null)
        {
            rightClickText = parent.GetComponent<TextMeshProUGUI>();
        }
        else if ((parent.name == "MauseWheelText" || parent.name == "MouseWheelText") && mouseWheelText == null)
        {
            mouseWheelText = parent.GetComponent<TextMeshProUGUI>();
        }

        // 子要素を再帰的に検索
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null)
            {
                SearchInChildren(child);
            }
        }
    }

    /// <summary>
    /// 左クリックテキストを更新します
    /// </summary>
    public void UpdateLeftClickText(string text)
    {
        if (leftClickText != null)
        {
            leftClickText.text = text;
        }
    }

    /// <summary>
    /// 右クリックテキストを更新します
    /// </summary>
    public void UpdateRightClickText(string text)
    {
        if (rightClickText != null)
        {
            rightClickText.text = text;
        }
    }

    /// <summary>
    /// マウスホイールテキストを更新します
    /// </summary>
    public void UpdateMouseWheelText(string text)
    {
        if (mouseWheelText != null)
        {
            mouseWheelText.text = text;
        }
    }

    /// <summary>
    /// すべてのテキストを一度に更新します
    /// </summary>
    public void UpdateAllTexts(string leftText, string rightText, string wheelText)
    {
        UpdateLeftClickText(leftText);
        UpdateRightClickText(rightText);
        UpdateMouseWheelText(wheelText);
    }
}

