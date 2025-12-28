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

    [Tooltip("現在のゲームステータスを参照します")]
    [SerializeField] private CurrentGameStatus currentGameStatus;

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
    // private const string PREFS_KEY_INCREMENT_STAGE = "MoveSceneButton_IncrementStage"; // 旧キー
    private const string PREFS_KEY_NEXT_STAGE_INDEX = "MoveSceneButton_NextStageIndex";

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
        
        if (PlayerPrefs.HasKey(PREFS_KEY_NEXT_STAGE_INDEX))
        {
            int nextStageIndex = PlayerPrefs.GetInt(PREFS_KEY_NEXT_STAGE_INDEX);
            PlayerPrefs.DeleteKey(PREFS_KEY_NEXT_STAGE_INDEX);
            PlayerPrefs.Save();
            
            // CurrentGameStatusのステージを設定
            // シーン遷移後は currentGameStatus が null の可能性があるため、検索する
            CurrentGameStatus statusToUpdate = currentGameStatus;
            if (statusToUpdate == null)
            {
                // ScriptableObject を検索 (またはシーン内のオブジェクト)
                CurrentGameStatus[] allStatuses = Resources.FindObjectsOfTypeAll<CurrentGameStatus>();
                if (allStatuses.Length > 0)
                {
                    statusToUpdate = allStatuses[0];
                }
                else 
                {
                    // シーン内のActiveなものを探す
                    statusToUpdate = Object.FindFirstObjectByType<CurrentGameStatus>();
                }
            }
            
            if (statusToUpdate != null)
            {
                statusToUpdate.SetCurrentStageIndex(nextStageIndex);
                Debug.Log($"MoveSceneButton: ステージ番号を {nextStageIndex} に設定しました");
            }
            else
            {
                Debug.LogWarning("MoveSceneButton: CurrentGameStatus が見つかりませんでした");
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

        // 現在のステータスを取得（アサインされていない場合は探す）
        CurrentGameStatus currentStatus = currentGameStatus;
        if (currentStatus == null)
        {
            currentStatus = Object.FindFirstObjectByType<CurrentGameStatus>();
        }

        // 次のステージ番号を計算して保存
        if (currentStatus != null)
        {
            int nextIndex = currentStatus.GetCurrentStageIndex();
            if (incrementStageAfterSceneChange)
            {
                nextIndex++;
            }
            PlayerPrefs.SetInt(PREFS_KEY_NEXT_STAGE_INDEX, nextIndex);
        }

        // シーン遷移前に設定を保存
        PlayerPrefs.SetInt(PREFS_KEY_SHOW_TUTORIAL, showTutorialAfterSceneChange ? 1 : 0);
        // PlayerPrefs.SetInt(PREFS_KEY_INCREMENT_STAGE, incrementStageAfterSceneChange ? 1 : 0); // 旧ロジック廃止
        PlayerPrefs.Save();

        isTransitioning = true;
        button.interactable = false;

        // FadeManagerのAPIを呼び出してフェードアウトとシーン遷移を実行
        fadeManager.FadeOutAndLoadScene(targetSceneName);
    }
}

