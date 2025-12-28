using UnityEngine;
using TMPro;

/// <summary>
/// 現在のステージ名をTMP_Textに表示するスクリプト
/// </summary>
public class StageTextWritter : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("ステージ名を表示するTMP_Textをアサインします")]
    [SerializeField] private TMP_Text stageText;

    [Header("References")]
    [Tooltip("現在のゲームステータスを参照します")]
    [SerializeField] private CurrentGameStatus currentGameStatus;

    private void Start()
    {
        if (currentGameStatus == null)
        {
            Debug.LogWarning("StageTextWritter: CurrentGameStatusがアサインされていません");
        }

        // ステージ名を更新
        UpdateStageText();
    }

    /// <summary>
    /// ステージ名を更新します
    /// </summary>
    public void UpdateStageText()
    {
        if (stageText == null)
        {
            Debug.LogWarning("StageTextWritter: StageTextがアサインされていません");
            return;
        }

        if (currentGameStatus == null)
        {
            Debug.LogWarning("StageTextWritter: CurrentGameStatusが設定されていません");
            return;
        }

        // 現在のステージデータを取得
        StageDatabase.StageData stageData = currentGameStatus.GetCurrentStageData();
        if (stageData != null)
        {
            stageText.text = stageData.stageName;
            Debug.Log($"ステージ名を更新しました: {stageData.stageName}");
        }
        else
        {
            stageText.text = "";
            Debug.LogWarning("StageTextWritter: ステージデータが取得できませんでした");
        }
    }
}
