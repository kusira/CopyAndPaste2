using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageDatabase", menuName = "Game/Stage Database")]
public class StageDatabase : ScriptableObject
{
    // ▼ 追加: ネストしたリストを保存するためのラッパークラス
    [System.Serializable]
    public class RowData
    {
        public List<string> columns = new List<string>();
        
        // インデックスアクセサがあると便利（オプション）
        public string this[int index]
        {
            get => columns[index];
            set => columns[index] = value;
        }

        public int Count => columns.Count;
    }

    [System.Serializable]
    public class StageData
    {
        [Tooltip("ステージ名")]
        public string stageName = "Stage 1";
        
        [Tooltip("Massの配置情報を表す二次元リストです。\".\"の位置にMassが生成されます")]
        // ▼ 変更: List<List<string>> から List<RowData> に変更
        public List<RowData> massStatus = new List<RowData>();
        
        [Tooltip("Rockの配置情報を表す二次元リストです。\"#\"の位置にRockが生成されます")]
        // ▼ 変更: List<List<string>> から List<RowData> に変更
        public List<RowData> rockStatus = new List<RowData>();
    }

    [Tooltip("ステージデータのリスト")]
    public List<StageData> stages = new List<StageData>();

    /// <summary>
    /// 指定されたステージ番号のデータを取得します
    /// </summary>
    public StageData GetStageData(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= stages.Count)
        {
            Debug.LogWarning($"ステージ番号{stageIndex}が範囲外です。ステージ数: {stages.Count}");
            return null;
        }

        return stages[stageIndex];
    }

    /// <summary>
    /// ステージ数を取得します
    /// </summary>
    public int GetStageCount()
    {
        return stages.Count;
    }
}