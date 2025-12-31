using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 初期シーンで画面をクリックしたらシーン遷移するスクリプト
/// </summary>
public class InitialSceneManager : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("遷移先のシーン名（デフォルト: TitleScene）")]
    [SerializeField] private string targetSceneName = "TitleScene";

    private bool isTransitioning = false;

    private void Update()
    {
        // 既に遷移中の場合は処理しない
        if (isTransitioning) return;

        // マウスクリックまたはタッチ入力を検出
        bool isClicked = false;

        // マウスの左クリック
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            isClicked = true;
        }

        // タッチスクリーンのタップ
        var touchscreen = Touchscreen.current;
        if (!isClicked && touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            isClicked = true;
        }

        // クリックが検出されたらシーン遷移
        if (isClicked)
        {
            TransitionToScene();
        }
    }

    /// <summary>
    /// シーン遷移を実行します
    /// </summary>
    private void TransitionToScene()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        Debug.Log($"画面クリックを検出しました。{targetSceneName}に遷移します");
        
        // 直接シーン遷移
        SceneManager.LoadScene(targetSceneName);
    }
}

