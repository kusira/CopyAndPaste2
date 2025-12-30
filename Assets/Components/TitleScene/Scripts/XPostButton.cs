using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// X（旧Twitter）に投稿するボタンクラス
/// </summary>
[RequireComponent(typeof(Button))]
public class XPostButton : MonoBehaviour
{
    [Header("Post Settings")]
    [Tooltip("Xに投稿する内容（改行は\\nで指定、{max}は最大到達ステージインデックスに置き換えられます）")]
    [TextArea(3, 10)]
    [SerializeField] private string postText = @"『複製妖精コピペちゃん』で{max}個のステージをクリアしました
#複製妖精コピペちゃん
https://unityroom.com/games/copipechan";

    [Header("References")]
    [Tooltip("CurrentGameStatus（未設定の場合は自動検索します）")]
    [SerializeField] private CurrentGameStatus currentGameStatus;

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

    private void Start()
    {
        // CurrentGameStatusが設定されていない場合は自動検索
        if (currentGameStatus == null)
        {
            currentGameStatus = FindFirstObjectByType<CurrentGameStatus>();
        }
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

        // CurrentGameStatusが未設定の場合は自動検索
        if (currentGameStatus == null)
        {
            currentGameStatus = FindFirstObjectByType<CurrentGameStatus>();
        }

        // 改行を実際の改行に変換
        string text = postText.Replace("\\n", "\n");

        // {max}を最大到達ステージインデックスに置き換え
        int maxReachedIndex = 0;
        if (currentGameStatus != null)
        {
            maxReachedIndex = currentGameStatus.GetMaxReachedStageIndex();
        }
        else
        {
            Debug.LogWarning("XPostButton: CurrentGameStatus が見つかりませんでした。{max}は0に置き換えられます。");
        }
        text = text.Replace("{max}", maxReachedIndex.ToString());

        // URLエンコード
        string encodedText = System.Uri.EscapeDataString(text);

        // Xの投稿URLを構築
        string url = $"https://twitter.com/intent/tweet?text={encodedText}";

        // ブラウザで開く
        Application.OpenURL(url);

        Debug.Log($"X投稿URLを開きました: {url}");
    }
}

