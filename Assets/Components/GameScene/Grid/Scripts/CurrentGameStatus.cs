using UnityEngine;

public class CurrentGameStatus : MonoBehaviour
{
    [Tooltip("現在のステージ番号（0から開始）")]
    [SerializeField] private int currentStageIndex = 0;

    [Tooltip("ステージデータベース")]
    [SerializeField] private StageDatabase stageDatabase;

    [Header("到達ステージ情報")]
    [Tooltip("到達したステージの最大値（読み取り専用）")]
    [SerializeField, ReadOnly] private int maxReachedStageIndex = 0;

    // ランタイム用のステージデータ（コピー）
    private StageDatabase.StageData runtimeStageData;
    private int cachedStageIndex = -1;

    private const string PREFS_KEY_MAX_REACHED_STAGE_INDEX = "MaxReachedStageIndex";

    private void Start()
    {
        // ゲーム開始時にデータをコピー
        if (Application.isPlaying)
        {
            RefreshRuntimeStageData();
        }

        // 到達したステージの最大値を読み込む
        LoadMaxReachedStageIndex();
    }

    /// <summary>
    /// 現在のステージデータを取得します。
    /// プレイ中はDeepCopyされたランタイムデータを、エディタ中はオリジナルデータを返します。
    /// </summary>
    public StageDatabase.StageData GetCurrentStageData()
    {
        // ステージ番号が変わっていたら更新
        if (cachedStageIndex != currentStageIndex)
        {
            RefreshRuntimeStageData();
        }

        if (Application.isPlaying)
        {
            // ランタイムデータが未生成なら生成
            if (runtimeStageData == null)
            {
                RefreshRuntimeStageData();
            }
            return runtimeStageData;
        }
        else
        {
            // エディタ（非プレイ時）はオリジナルを返す
            if (stageDatabase != null)
            {
                return stageDatabase.GetStageData(currentStageIndex);
            }
        }
        return null;
    }

    /// <summary>
    /// ステージデータベースへの参照を取得します（エディタ拡張用）
    /// </summary>
    public StageDatabase GetStageDatabase()
    {
        return stageDatabase;
    }

    private void RefreshRuntimeStageData()
    {
        cachedStageIndex = currentStageIndex;
        if (stageDatabase != null)
        {
            var original = stageDatabase.GetStageData(currentStageIndex);
            if (original != null)
            {
                if (Application.isPlaying)
                {
                    // プレイ中はディープコピーを作成
                    runtimeStageData = original.DeepCopy();

                    // RockStatus が未設定またはサイズ0の場合、MassStatusと同じサイズの空グリッドを用意する
                    if (runtimeStageData != null)
                    {
                        int height = runtimeStageData.massStatus != null ? runtimeStageData.massStatus.Count : 0;
                        int width = (height > 0 && runtimeStageData.massStatus[0] != null && runtimeStageData.massStatus[0].columns != null)
                            ? runtimeStageData.massStatus[0].columns.Count
                            : 0;

                        if (height > 0 && width > 0)
                        {
                            // RockStatus が null または行数0のときは、Mass と同じサイズの空グリッドを用意
                            if (runtimeStageData.rockStatus == null || runtimeStageData.rockStatus.Count == 0)
                            {
                                runtimeStageData.rockStatus = new System.Collections.Generic.List<StageDatabase.RowData>();
                            }

                            // 行数を合わせる
                            while (runtimeStageData.rockStatus.Count < height)
                            {
                                runtimeStageData.rockStatus.Add(new StageDatabase.RowData());
                            }
                            while (runtimeStageData.rockStatus.Count > height)
                            {
                                runtimeStageData.rockStatus.RemoveAt(runtimeStageData.rockStatus.Count - 1);
                            }

                            // 各行の列数を合わせる（不足分は空文字）
                            for (int h = 0; h < height; h++)
                            {
                                var row = runtimeStageData.rockStatus[h];
                                if (row == null)
                                {
                                    row = new StageDatabase.RowData();
                                    runtimeStageData.rockStatus[h] = row;
                                }
                                if (row.columns == null)
                                {
                                    row.columns = new System.Collections.Generic.List<string>();
                                }

                                while (row.columns.Count < width)
                                {
                                    row.columns.Add(string.Empty);
                                }
                                while (row.columns.Count > width)
                                {
                                    row.columns.RemoveAt(row.columns.Count - 1);
                                }
                            }
                        }
                    }
                }
                else
                {
                    runtimeStageData = null;
                }
            }
        }
    }

    /// <summary>
    /// ランタイムのステージデータを外部から設定します（Undo/Redo用）
    /// </summary>
    public void SetRuntimeStageData(StageDatabase.StageData data)
    {
        if (data != null)
        {
            runtimeStageData = data;
        }
    }

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
            // データ更新
            RefreshRuntimeStageData();
            
            // 到達したステージの最大値を更新
            UpdateMaxReachedStageIndex(index);
        }
    }

    /// <summary>
    /// 到達したステージの最大値を更新します
    /// </summary>
    private void UpdateMaxReachedStageIndex(int stageIndex)
    {
        if (stageIndex > maxReachedStageIndex)
        {
            maxReachedStageIndex = stageIndex;
            PlayerPrefs.SetInt(PREFS_KEY_MAX_REACHED_STAGE_INDEX, maxReachedStageIndex);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 到達したステージの最大値を読み込みます
    /// </summary>
    public void LoadMaxReachedStageIndex()
    {
        maxReachedStageIndex = PlayerPrefs.GetInt(PREFS_KEY_MAX_REACHED_STAGE_INDEX, 0);
    }

    /// <summary>
    /// 到達したステージの最大値を取得します
    /// </summary>
    public int GetMaxReachedStageIndex()
    {
        return maxReachedStageIndex;
    }

    /// <summary>
    /// 到達したステージの最大値をリセットします
    /// </summary>
    public void ResetMaxReachedStageIndex()
    {
        maxReachedStageIndex = 0;
        PlayerPrefs.DeleteKey(PREFS_KEY_MAX_REACHED_STAGE_INDEX);
        PlayerPrefs.Save();
        Debug.Log("到達したステージの最大値をリセットしました");
    }
}

/// <summary>
/// Inspectorで読み取り専用フィールドを表示するための属性
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute { }
