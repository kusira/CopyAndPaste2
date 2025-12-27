using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ボタンを押したらフェードアウトしてシーン遷移するスクリプト
/// </summary>
[RequireComponent(typeof(Button))]
public class MoveSceneButton : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("FadeManagerをアサインします")]
    [SerializeField] private FadeManager fadeManager;

    [Header("設定")]
    [Tooltip("遷移先のシーン名を入力します")]
    [SerializeField] private string targetSceneName;

    [Tooltip("遷移先のシーンのビルドインデックスを指定します（-1の場合はシーン名を使用）")]
    [SerializeField] private int targetSceneBuildIndex = -1;

    private Button button;
    private bool isTransitioning;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (fadeManager == null)
        {
            Debug.LogWarning("MoveSceneButton: FadeManagerがアサインされていません");
        }
    }

    private void OnEnable()
    {
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (isTransitioning) return;

        if (fadeManager == null)
        {
            Debug.LogError("MoveSceneButton: FadeManagerがアサインされていません");
            return;
        }

        if (string.IsNullOrEmpty(targetSceneName) && targetSceneBuildIndex < 0)
        {
            Debug.LogError("MoveSceneButton: 遷移先のシーン名またはビルドインデックスが設定されていません");
            return;
        }

        isTransitioning = true;
        button.interactable = false;

        // FadeManagerのAPIを呼び出してフェードアウトとシーン遷移を実行
        if (targetSceneBuildIndex >= 0)
        {
            fadeManager.FadeOutAndLoadScene(targetSceneBuildIndex);
        }
        else
        {
            fadeManager.FadeOutAndLoadScene(targetSceneName);
        }
    }
}

