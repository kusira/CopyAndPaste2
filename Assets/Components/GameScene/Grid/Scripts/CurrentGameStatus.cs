using UnityEngine;

public class CurrentGameStatus : MonoBehaviour
{
    [Tooltip("現在のステージ番号（0から開始）")]
    [SerializeField] private int currentStageIndex = 0;

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
        if (currentStageIndex != index)
        {
            currentStageIndex = index;
        }
    }
}
