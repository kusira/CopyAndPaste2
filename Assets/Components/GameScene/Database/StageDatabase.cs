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

        // ▼ 追加: ディープコピー用メソッド
        public RowData DeepCopy()
        {
            RowData copy = new RowData();
            copy.columns = new List<string>(this.columns);
            return copy;
        }
    }

    /// <summary>
    /// アイテムのタイプ
    /// </summary>
    public enum RSItemType
    {
        Normal,
        Pickaxe,
        Gravity
    }

    /// <summary>
    /// チュートリアル表示タイプ
    /// </summary>
    public enum TutorialDisplayType
    {
        None,        // チュートリアル表示なし
        Tutorial_1,  // Tutorial_1だけ表示
        Tutorial_2,  // Tutorial_2だけ表示
        Tutorial_3   // Tutorial_3だけ表示
    }

    [System.Serializable]
    public class RSItemData
    {
        [Tooltip("範囲選択アイテムの高さ（H）")]
        public int height = 1;
        
        [Tooltip("範囲選択アイテムの幅（W）")]
        public int width = 1;

        [Tooltip("アイテムのタイプ")]
        public RSItemType type = RSItemType.Normal;

        // ▼ 追加: ディープコピー用メソッド
        public RSItemData DeepCopy()
        {
            return new RSItemData
            {
                height = this.height,
                width = this.width,
                type = this.type
            };
        }
    }

    [System.Serializable]
    public class StageData
    {
        [Tooltip("ステージ名")]
        public string stageName = "Stage 1";
        
        [Tooltip("ワールドラベル（ワールドごとのスプライト切り替えに使用されます）")]
        public string worldLabel = "World1";
        
        [Tooltip("Massの配置情報を表す二次元リストです。\".\"の位置にMassが生成されます")]
        // ▼ 変更: List<List<string>> から List<RowData> に変更
        public List<RowData> massStatus = new List<RowData>();
        
        [Tooltip("Rockの配置情報を表す二次元リストです。\"#\"の位置にRockが生成されます")]
        // ▼ 変更: List<List<string>> から List<RowData> に変更
        public List<RowData> rockStatus = new List<RowData>();
        
        [Tooltip("範囲選択アイテムのリストです。i個目のアイテムのサイズはH_i*W_iで指定されます")]
        public List<RSItemData> RSItems = new List<RSItemData>();

        [Tooltip("GridParentのScale（XとYは等しく、Zは1固定）。子オブジェクト（Mass、Rock、GridFrame）もこの影響を受けます")]
        public float gridParentScaleXY = 1f;

        [Tooltip("チュートリアルパネルの表示設定")]
        public TutorialDisplayType tutorialDisplayType = TutorialDisplayType.None;

        // ▼ 追加: ディープコピー用メソッド
        public StageData DeepCopy()
        {
            StageData copy = new StageData();
            copy.stageName = this.stageName;
            copy.worldLabel = this.worldLabel;
            copy.gridParentScaleXY = this.gridParentScaleXY;
            copy.tutorialDisplayType = this.tutorialDisplayType;

            foreach (var mass in this.massStatus)
            {
                copy.massStatus.Add(mass.DeepCopy());
            }

            foreach (var rock in this.rockStatus)
            {
                copy.rockStatus.Add(rock.DeepCopy());
            }

            foreach (var item in this.RSItems)
            {
                copy.RSItems.Add(item.DeepCopy());
            }

            return copy;
        }
    }

    [Tooltip("ワールドラベルのリスト（各ステージで選択可能なワールドラベル）")]
    public List<string> worldLabels = new List<string>();

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