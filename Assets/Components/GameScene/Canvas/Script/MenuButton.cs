using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// メニュー開閉を制御するスクリプト（MenuButtonにアタッチ）
/// </summary>
[RequireComponent(typeof(Button))]
public class MenuButton : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private RectTransform menuPanel;
    [SerializeField] private GameObject backdrop;
    [SerializeField] private Button closeButton;

    [Header("設定")]
    [SerializeField] private float animationDuration = 0.5f;

    private Button menuButton;
    private Tween panelTween;
    private Vector2 openedPos;
    private Vector2 closedPos;
    private bool initialized;

    private void Awake()
    {
        menuButton = GetComponent<Button>();
        if (menuPanel == null || backdrop == null || closeButton == null)
        {
            Debug.LogWarning("MenuButton: 必要な参照が設定されていません");
            return;
        }

        // 開始時の位置をオープン位置として記録
        openedPos = menuPanel.anchoredPosition;
        float height = menuPanel.rect.height;
        closedPos = openedPos + new Vector2(0f, height);

        // 非表示状態に初期化
        menuPanel.anchoredPosition = closedPos;
        menuPanel.gameObject.SetActive(false);
        backdrop.SetActive(false);

        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized) return;
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OpenMenu);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseMenu);
        }

        if (backdrop != null)
        {
            var backdropButton = backdrop.GetComponent<Button>();
            if (backdropButton != null)
            {
                backdropButton.onClick.AddListener(CloseMenu);
            }
        }
    }

    private void OnDisable()
    {
        if (!initialized) return;
        
        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(OpenMenu);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseMenu);
        }

        if (backdrop != null)
        {
            var backdropButton = backdrop.GetComponent<Button>();
            if (backdropButton != null)
            {
                backdropButton.onClick.RemoveListener(CloseMenu);
            }
        }
    }

    private void OnDestroy()
    {
        panelTween?.Kill();
    }

    private void OpenMenu()
    {
        if (!initialized) return;
        if (backdrop == null || menuPanel == null) return;

        backdrop.SetActive(true);
        menuPanel.gameObject.SetActive(true);

        panelTween?.Kill();
        menuPanel.anchoredPosition = closedPos;
        panelTween = menuPanel.DOAnchorPos(openedPos, animationDuration)
            .SetEase(Ease.OutQuad);
    }

    private void CloseMenu()
    {
        if (!initialized) return;
        if (menuPanel == null || backdrop == null) return;

        panelTween?.Kill();
        panelTween = menuPanel.DOAnchorPos(closedPos, animationDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (menuPanel != null)
                {
                    menuPanel.gameObject.SetActive(false);
                }
                if (backdrop != null)
                {
                    backdrop.SetActive(false);
                }
            });
    }
}

