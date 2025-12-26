using System.Collections;
using UnityEngine;

/// <summary>
/// すべてのProgressがAcquiredになったときにリザルトを表示するクラス
/// </summary>
public class ResultShower : MonoBehaviour
{
    [Header("Result Objects")]
    [Tooltip("背景用のBackdropオブジェクトをアサインします")]
    [SerializeField] private GameObject backdropObject;

    [Tooltip("リザルト表示用のResultオブジェクトをアサインします")]
    [SerializeField] private GameObject resultObject;

    [Header("Timing")]
    [Tooltip("リザルトを表示するまでの待ち時間（秒）")]
    [SerializeField] private float showDelaySeconds = 1.0f;

    private bool isShown = false;
    private bool isResultShowing = false;

    /// <summary>
    /// リザルト表示を開始します（複数回呼ばれても一度だけ表示）
    /// </summary>
    public void ShowResult()
    {
        if (isShown) return;
        isShown = true;
        StartCoroutine(ShowResultRoutine());
    }

    private IEnumerator ShowResultRoutine()
    {
        if (showDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(showDelaySeconds);
        }

        isResultShowing = true;

        if (backdropObject != null)
        {
            backdropObject.SetActive(true);
        }

        if (resultObject != null)
        {
            resultObject.SetActive(true);
        }

        // リザルト表示中は振動を停止
        if (CharacterVibrator.Instance != null)
        {
            CharacterVibrator.Instance.SetVibrationEnabled(false);
        }

        Debug.Log("リザルトを表示しました");
    }

    /// <summary>
    /// リザルトが表示されているかどうかを取得します
    /// </summary>
    public bool IsResultShowing()
    {
        return isResultShowing;
    }

    /// <summary>
    /// リザルトを非表示にして振動を再開します（必要に応じて外部から呼び出し）
    /// </summary>
    public void HideResult()
    {
        if (!isResultShowing) return;

        isResultShowing = false;
        isShown = false;

        if (backdropObject != null)
        {
            backdropObject.SetActive(false);
        }

        if (resultObject != null)
        {
            resultObject.SetActive(false);
        }

        // リザルトが閉じられたら振動を再開
        if (CharacterVibrator.Instance != null)
        {
            CharacterVibrator.Instance.SetVibrationEnabled(true);
        }
    }
}


