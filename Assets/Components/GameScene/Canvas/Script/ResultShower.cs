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

        if (backdropObject != null)
        {
            backdropObject.SetActive(true);
        }

        if (resultObject != null)
        {
            resultObject.SetActive(true);
        }

        Debug.Log("リザルトを表示しました");
    }
}


