using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// グリッドの状態を監視し、.Sと#S、.Pと#P、.Cと#Cが同じ座標になったときにProgressを進めます
/// </summary>
public class GridMonitor : MonoBehaviour
{
    [Header("References")]
    [Tooltip("現在のゲームステータスを参照します")]
    [SerializeField] private CurrentGameStatus currentGameStatus;
    
    [Tooltip("ProgressGeneratorへの参照")]
    [SerializeField] private ProgressGenerator progressGenerator;

    [Header("Monitor Settings")]
    [Tooltip("監視の更新間隔（秒）")]
    [SerializeField] private float checkInterval = 0.1f;

    private float lastCheckTime = 0f;
    private HashSet<string> acquiredProgressKeys = new HashSet<string>();

    private void Start()
    {
        // ProgressGeneratorが見つからない場合は自動検索
        if (progressGenerator == null)
        {
            progressGenerator = FindFirstObjectByType<ProgressGenerator>();
        }

        if (progressGenerator == null)
        {
            Debug.LogWarning("GridMonitor: ProgressGeneratorが見つかりません");
        }

        if (currentGameStatus == null)
        {
            currentGameStatus = FindFirstObjectByType<CurrentGameStatus>();
        }

        if (currentGameStatus == null)
        {
            Debug.LogWarning("GridMonitor: CurrentGameStatusが見つかりません");
        }
    }

    private void Update()
    {
        // 指定間隔でチェック
        if (Time.time - lastCheckTime >= checkInterval)
        {
            lastCheckTime = Time.time;
            CheckProgressConditions();
        }
    }

    /// <summary>
    /// グリッドの状態をチェックして、Progress条件を満たしているか確認します
    /// </summary>
    private void CheckProgressConditions()
    {
        if (currentGameStatus == null || progressGenerator == null)
        {
            return;
        }

        StageDatabase.StageData stageData = currentGameStatus.GetCurrentStageData();
        if (stageData == null || stageData.massStatus == null || stageData.rockStatus == null)
        {
            return;
        }

        List<StageDatabase.RowData> massStatus = stageData.massStatus;
        List<StageDatabase.RowData> rockStatus = stageData.rockStatus;

        // 各座標で.Sと#S、.Pと#P、.Cと#Cが一致しているかチェック
        for (int h = 0; h < massStatus.Count; h++)
        {
            if (massStatus[h] == null || massStatus[h].columns == null) continue;

            for (int w = 0; w < massStatus[h].columns.Count; w++)
            {
                // MassStatusをチェック
                string massValue = massStatus[h].columns[w];
                if (string.IsNullOrEmpty(massValue)) continue;

                char massBaseChar;
                List<string> massKeys = new List<string>();
                RangeSelectorHelper.ParseCell(massValue, out massBaseChar, massKeys);

                // .S, .P, .Cをチェック
                if (massBaseChar == '.')
                {
                    foreach (var key in massKeys)
                    {
                        if (key == "S" || key == "P" || key == "C")
                        {
                            // 同じ座標のRockStatusをチェック
                            if (h < rockStatus.Count && 
                                rockStatus[h] != null && 
                                rockStatus[h].columns != null && 
                                w < rockStatus[h].columns.Count)
                            {
                                string rockValue = rockStatus[h].columns[w];
                                if (!string.IsNullOrEmpty(rockValue))
                                {
                                    char rockBaseChar;
                                    List<string> rockKeys = new List<string>();
                                    RangeSelectorHelper.ParseCell(rockValue, out rockBaseChar, rockKeys);

                                    // #S, #P, #Cをチェック
                                    if (rockBaseChar == '#' && rockKeys.Contains(key))
                                    {
                                        // 条件を満たしている
                                        Vector2Int gridPos = new Vector2Int(w, h);
                                        string progressKey = $"{key}_{gridPos.x}_{gridPos.y}";

                                        if (!acquiredProgressKeys.Contains(progressKey))
                                        {
                                            acquiredProgressKeys.Add(progressKey);
                                            progressGenerator.SetProgressAcquired(gridPos, key);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 監視状態をリセットします（ステージ開始時などに呼び出し）
    /// </summary>
    public void ResetMonitor()
    {
        acquiredProgressKeys.Clear();
    }
}

