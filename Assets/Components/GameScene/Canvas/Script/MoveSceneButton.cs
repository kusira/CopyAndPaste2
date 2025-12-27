using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    [Header("シーン遷移後の動作")]
    [Tooltip("シーンチェンジ後、ステージがチュートリアル表示設定であるとき、再びチュートリアルを表示するか")]
    [SerializeField] private bool showTutorialAfterSceneChange = false;

    [Tooltip("シーンチェンジ後現在のステージ数をインクリメントするかどうか")]
    [SerializeField] private bool incrementStageAfterSceneChange = true;

    private Button button;
    private bool isTransitioning;
    private const string PREFS_KEY_SHOW_TUTORIAL = "MoveSceneButton_ShowTutorial";
    private const string PREFS_KEY_INCREMENT_STAGE = "MoveSceneButton_IncrementStage";

    private void Awake()
    {
        button = GetComponent<Button>();
        if (fadeManager == null)
        {
            Debug.LogWarning("MoveSceneButton: FadeManagerがアサインされていません");
        }

        // シーンロードイベントを登録
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // イベントを解除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // シーンロード後に設定を確認して処理を実行
        // チュートリアル表示の設定はTutorialManagerのStart()で処理されるため、ここでは削除しない
        
        if (PlayerPrefs.GetInt(PREFS_KEY_INCREMENT_STAGE, 0) == 1)
        {
            PlayerPrefs.DeleteKey(PREFS_KEY_INCREMENT_STAGE);
            PlayerPrefs.Save();
            // CurrentGameStatusのステージをインクリメント
            CurrentGameStatus currentGameStatus = Object.FindFirstObjectByType<CurrentGameStatus>();
            if (currentGameStatus != null)
            {
                int currentIndex = currentGameStatus.GetCurrentStageIndex();
                currentGameStatus.SetCurrentStageIndex(currentIndex + 1);
            }
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

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("MoveSceneButton: 遷移先のシーン名が設定されていません");
            return;
        }

        // シーン遷移前に設定を保存
        PlayerPrefs.SetInt(PREFS_KEY_SHOW_TUTORIAL, showTutorialAfterSceneChange ? 1 : 0);
        PlayerPrefs.SetInt(PREFS_KEY_INCREMENT_STAGE, incrementStageAfterSceneChange ? 1 : 0);
        PlayerPrefs.Save();

        isTransitioning = true;
        button.interactable = false;

        // FadeManagerのAPIを呼び出してフェードアウトとシーン遷移を実行
        fadeManager.FadeOutAndLoadScene(targetSceneName);
    }
}

