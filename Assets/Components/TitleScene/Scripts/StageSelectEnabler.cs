using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 到達したステージに応じてボタンの有効/無効を制御するクラス
/// </summary>
public class StageSelectEnabler : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("CurrentGameStatusをアサインします")]
    [SerializeField] private CurrentGameStatus currentGameStatus;

    [Header("ボタンリスト")]
    [Tooltip("ステージ選択ボタンのリスト（インデックス順に並べてください）")]
    [SerializeField] private List<Button> stageButtons = new List<Button>();

    private void Start()
    {
        UpdateButtonStates();
    }

    /// <summary>
    /// ボタンの有効/無効状態を更新します
    /// </summary>
    public void UpdateButtonStates()
    {
        // CurrentGameStatusが未設定の場合は自動検索
        if (currentGameStatus == null)
        {
            currentGameStatus = FindFirstObjectByType<CurrentGameStatus>();
        }

        if (currentGameStatus == null)
        {
            Debug.LogWarning("StageSelectEnabler: CurrentGameStatus が見つかりませんでした");
            // CurrentGameStatusが見つからない場合はすべてのボタンを無効化
            foreach (var button in stageButtons)
            {
                if (button != null)
                {
                    button.interactable = false;
                }
            }
            return;
        }

        // 到達したステージの最大値を取得
        int maxReachedIndex = currentGameStatus.GetMaxReachedStageIndex();

        // 各ボタンの有効/無効を設定
        for (int i = 0; i < stageButtons.Count; i++)
        {
            if (stageButtons[i] != null)
            {
                // 到達したインデックス以下のボタンは有効、それ超過は無効
                stageButtons[i].interactable = (i <= maxReachedIndex);
            }
        }

        Debug.Log($"StageSelectEnabler: 到達ステージ最大値({maxReachedIndex})に基づいてボタンの状態を更新しました");
    }
}

