using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// チュートリアルパネルを表示・非表示するスクリプト
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("背景のBackdropをアサインします")]
    [SerializeField] private GameObject backdrop;
    
    [Tooltip("チュートリアルパネルをアサインします")]
    [SerializeField] private GameObject tutorialPanel;
    
    [Tooltip("閉じるボタンをアサインします")]
    [SerializeField] private Button closeButton;
    
    [Tooltip("Tutorial_1パネルをアサインします")]
    [SerializeField] private GameObject tutorial_1;
    
    [Tooltip("Tutorial_2パネルをアサインします")]
    [SerializeField] private GameObject tutorial_2;
    
    [Tooltip("Tutorial_3パネルをアサインします")]
    [SerializeField] private GameObject tutorial_3;

    [Header("Animation Settings")]
    [Tooltip("アニメーションの時間（秒）")]
    [SerializeField] private float animationDuration = 0.5f;
    [Tooltip("Backdropのフェードアニメーション時間（秒）")]
    [SerializeField] private float backdropFadeDuration = 0.3f;

    private CurrentGameStatus currentGameStatus;
    private bool isAnimating = false;
    private bool initialized = false;

    private RectTransform tutorialPanelRect;
    private Vector2 openedPos;
    private Vector2 closedPos;
    private Tween panelTween;
    private Tween backdropTween;
    private CanvasGroup backdropCanvasGroup;

    private void Awake()
    {
        // TutorialPanelのRectTransformを取得
        if (tutorialPanel != null)
        {
            tutorialPanelRect = tutorialPanel.GetComponent<RectTransform>();
            if (tutorialPanelRect == null)
            {
                Debug.LogWarning("TutorialManager: TutorialPanelにRectTransformがアタッチされていません");
                return;
            }

            // 開始時の位置をオープン位置として記録
            openedPos = tutorialPanelRect.anchoredPosition;
            float height = tutorialPanelRect.rect.height;
            closedPos = openedPos + new Vector2(0f, height);

            // 非表示状態に初期化
            tutorialPanelRect.anchoredPosition = closedPos;
            tutorialPanel.SetActive(false);
        }

        // 初期状態ではすべて非表示
        if (backdrop != null)
        {
            // BackdropのCanvasGroupを取得
            backdropCanvasGroup = backdrop.GetComponent<CanvasGroup>();
            if (backdropCanvasGroup == null)
            {
                Debug.LogWarning("TutorialManager: BackdropにCanvasGroupコンポーネントがアタッチされていません");
            }
            else
            {
                // 初期状態を非表示（alpha = 0）
                backdropCanvasGroup.alpha = 0f;
            }
            
            backdrop.SetActive(false);
        }
        if (tutorial_1 != null)
        {
            tutorial_1.SetActive(false);
        }
        if (tutorial_2 != null)
        {
            tutorial_2.SetActive(false);
        }
        if (tutorial_3 != null)
        {
            tutorial_3.SetActive(false);
        }

        initialized = true;
    }

    private void Start()
    {
        // CurrentGameStatusを取得
        currentGameStatus = Object.FindFirstObjectByType<CurrentGameStatus>();

        // 閉じるボタンのイベントを設定
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        // Backdropにクリックイベントを設定
        if (backdrop != null)
        {
            // EventTriggerを追加
            EventTrigger trigger = backdrop.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = backdrop.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { OnBackdropClicked(); });
            trigger.triggers.Add(entry);
        }

        // ステージデータを確認してチュートリアルを表示
        ShowTutorialIfNeeded();
    }

    /// <summary>
    /// ステージデータに基づいてチュートリアルを表示します
    /// </summary>
    private void ShowTutorialIfNeeded()
    {
        if (currentGameStatus == null)
        {
            return;
        }

        StageDatabase.StageData stageData = currentGameStatus.GetCurrentStageData();
        if (stageData == null)
        {
            return;
        }

        // チュートリアル表示タイプに応じて表示
        switch (stageData.tutorialDisplayType)
        {
            case StageDatabase.TutorialDisplayType.None:
                // 何も表示しない
                break;
            case StageDatabase.TutorialDisplayType.Tutorial_1:
                ShowTutorial(1);
                break;
            case StageDatabase.TutorialDisplayType.Tutorial_2:
                ShowTutorial(2);
                break;
            case StageDatabase.TutorialDisplayType.Tutorial_3:
                ShowTutorial(3);
                break;
        }
    }

    /// <summary>
    /// 指定されたチュートリアルを表示します
    /// </summary>
    private void ShowTutorial(int tutorialNumber)
    {
        if (!initialized || isAnimating)
        {
            return;
        }

        if (backdrop == null || tutorialPanelRect == null)
        {
            return;
        }

        // 指定されたチュートリアルのみを表示
        if (tutorial_1 != null)
        {
            tutorial_1.SetActive(tutorialNumber == 1);
        }
        if (tutorial_2 != null)
        {
            tutorial_2.SetActive(tutorialNumber == 2);
        }
        if (tutorial_3 != null)
        {
            tutorial_3.SetActive(tutorialNumber == 3);
        }

        // Backdropを表示してフェードイン
        backdrop.SetActive(true);
        backdropTween?.Kill();
        
        if (backdropCanvasGroup != null)
        {
            backdropCanvasGroup.alpha = 0f;
            backdropTween = backdropCanvasGroup.DOFade(1f, backdropFadeDuration)
                .SetEase(Ease.OutQuad);
        }
        
        tutorialPanel.SetActive(true);
        tutorialPanelRect.anchoredPosition = openedPos;

        // パネル表示中は振動を停止
        if (CharacterVibrator.Instance != null)
        {
            CharacterVibrator.Instance.SetVibrationEnabled(false);
        }
    }

    /// <summary>
    /// チュートリアルを非表示にします（アニメーション付き）
    /// </summary>
    private void HideTutorial()
    {
        if (!initialized || isAnimating)
        {
            return;
        }

        if (tutorialPanelRect == null || backdrop == null)
        {
            return;
        }

        isAnimating = true;

        // Backdropをフェードアウト
        backdropTween?.Kill();
        if (backdropCanvasGroup != null)
        {
            backdropTween = backdropCanvasGroup.DOFade(0f, backdropFadeDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    if (backdrop != null)
                    {
                        backdrop.SetActive(false);
                    }
                });
        }
        else
        {
            // CanvasGroupがない場合は即座に非表示
            if (backdrop != null)
            {
                backdrop.SetActive(false);
            }
        }

        // アニメーションを実行
        panelTween?.Kill();
        panelTween = tutorialPanelRect.DOAnchorPos(closedPos, animationDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (tutorialPanel != null)
                {
                    tutorialPanel.SetActive(false);
                }
                if (tutorial_1 != null)
                {
                    tutorial_1.SetActive(false);
                }
                if (tutorial_2 != null)
                {
                    tutorial_2.SetActive(false);
                }
                if (tutorial_3 != null)
                {
                    tutorial_3.SetActive(false);
                }
                isAnimating = false;
                
                // パネルが閉じられたら振動を再開
                if (CharacterVibrator.Instance != null)
                {
                    CharacterVibrator.Instance.SetVibrationEnabled(true);
                }
            });
    }

    /// <summary>
    /// 閉じるボタンがクリックされたときの処理
    /// </summary>
    private void OnCloseButtonClicked()
    {
        Debug.Log("閉じるボタンがクリックされました");
        HideTutorial();
    }

    /// <summary>
    /// Backdropがクリックされたときの処理
    /// </summary>
    private void OnBackdropClicked()
    {
        Debug.Log("Backdropがクリックされました");
        HideTutorial();
    }

    private void OnDestroy()
    {
        // DOTweenのTweenをクリーンアップ
        panelTween?.Kill();
        backdropTween?.Kill();
    }
}

