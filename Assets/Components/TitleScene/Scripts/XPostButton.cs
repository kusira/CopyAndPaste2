using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// X（旧Twitter）に投稿するボタンクラス
/// </summary>
[RequireComponent(typeof(Button))]
public class XPostButton : MonoBehaviour
{
    [Header("Post Settings")]
    [Tooltip("Xに投稿する内容（改行は\\nで指定）")]
    [TextArea(3, 10)]
    [SerializeField] private string postText = @"『複製妖精コピペちゃん』をプレイしたよ
#複製妖精コピペちゃん
https://unityroom.com/games/copipechan";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning("XPostButton: Buttonコンポーネントが見つかりません");
            return;
        }

        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    /// <summary>
    /// ボタンがクリックされたときに呼ばれます
    /// </summary>
    private void OnButtonClicked()
    {
        PostToX();
    }

    /// <summary>
    /// Xに投稿するURLを開きます
    /// </summary>
    public void PostToX()
    {
        if (string.IsNullOrEmpty(postText))
        {
            Debug.LogWarning("XPostButton: 投稿内容が設定されていません");
            return;
        }

        // 改行を実際の改行に変換
        string text = postText.Replace("\\n", "\n");

        // URLエンコード
        string encodedText = System.Uri.EscapeDataString(text);

        // Xの投稿URLを構築
        string url = $"https://twitter.com/intent/tweet?text={encodedText}";

        // ブラウザで開く
        Application.OpenURL(url);

        Debug.Log($"X投稿URLを開きました: {url}");
    }
}

