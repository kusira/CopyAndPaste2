using UnityEngine;

public class CurrentGameStatus : MonoBehaviour
{
    [Tooltip("現在のステージ番号（0から開始）")]
    [SerializeField] private int currentStageIndex = 0;

    [Tooltip("ステージデータベース")]
    [SerializeField] private StageDatabase stageDatabase;

    /// <summary>
    /// 現在のステージ番号を取得します
    /// </summary>
    public int GetCurrentStageIndex()
    {
        return currentStageIndex;
    }

    /// <summary>
    /// 現在のステージ番号を設定します
    /// </summary>
    public void SetCurrentStageIndex(int index)
    {
        currentStageIndex = index;
    }

    /// <summary>
    /// 現在のステージデータを取得します
    /// </summary>
    public StageDatabase.StageData GetCurrentStageData()
    {
        if (stageDatabase == null)
        {
            Debug.LogWarning("StageDatabaseがアサインされていません");
            return null;
        }

        return stageDatabase.GetStageData(currentStageIndex);
    }

    /// <summary>
    /// ステージデータベースを設定します
    /// </summary>
    public void SetStageDatabase(StageDatabase database)
    {
        stageDatabase = database;
    }
}

