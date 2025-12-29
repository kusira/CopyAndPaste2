using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 現在のワールドに応じてBGMを再生するマネージャー
/// </summary>
public class GameBGMManager : MonoBehaviour
{
    [System.Serializable]
    public class WorldBGMData
    {
        [Tooltip("ワールドラベル（StageDatabaseのworldLabelと一致させる）")]
        public string worldLabel;

        [Tooltip("このワールドで再生するCuePlayオブジェクト（CriAtomSourceがアサインされている必要があります）")]
        public CuePlay cuePlay;
    }

    [Header("ワールド別BGM設定")]
    [Tooltip("ワールドラベルとCuePlayのマッピング")]
    [SerializeField] private List<WorldBGMData> worldBGMList = new List<WorldBGMData>();

    [Header("参照設定")]
    [Tooltip("CurrentGameStatus（未設定の場合は自動検索します）")]
    [SerializeField] private CurrentGameStatus currentGameStatus;

    private void Start()
    {
        // CurrentGameStatusが設定されていない場合は自動検索
        if (currentGameStatus == null)
        {
            currentGameStatus = FindFirstObjectByType<CurrentGameStatus>();
        }

        // BGMを再生
        PlayBGMForCurrentWorld();
    }

    /// <summary>
    /// 現在のワールドに応じたBGMを再生します
    /// </summary>
    public void PlayBGMForCurrentWorld()
    {
        if (currentGameStatus == null)
        {
            Debug.LogWarning("GameBGMManager: CurrentGameStatusが見つかりません。");
            return;
        }

        // 現在のステージデータを取得
        var stageData = currentGameStatus.GetCurrentStageData();
        if (stageData == null)
        {
            Debug.LogWarning("GameBGMManager: ステージデータが取得できませんでした。");
            return;
        }

        // ワールドラベルを取得
        string worldLabel = stageData.worldLabel;
        if (string.IsNullOrEmpty(worldLabel))
        {
            Debug.LogWarning("GameBGMManager: ワールドラベルが設定されていません。");
            return;
        }

        // 対応するCuePlayを検索
        CuePlay targetCuePlay = null;
        foreach (var worldBGM in worldBGMList)
        {
            if (worldBGM.worldLabel == worldLabel)
            {
                targetCuePlay = worldBGM.cuePlay;
                break;
            }
        }

        if (targetCuePlay != null)
        {
            targetCuePlay.PlaySound();
            Debug.Log($"GameBGMManager: ワールド '{worldLabel}' のBGMを再生開始しました");
        }
        else
        {
            Debug.LogWarning($"GameBGMManager: ワールド '{worldLabel}' に対応するCuePlayが見つかりませんでした。");
        }
    }

    /// <summary>
    /// ワールド別BGMリストを取得します（エディタ拡張用）
    /// </summary>
    public List<WorldBGMData> GetWorldBGMList()
    {
        return worldBGMList;
    }
}

