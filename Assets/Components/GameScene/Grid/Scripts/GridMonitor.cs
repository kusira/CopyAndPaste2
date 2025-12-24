using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// グリッドの状態を監視し、.Sと#S、.Hと#H、.Cと#Cが同じ座標になったときにProgressを進めます
/// </summary>
public class GridMonitor : MonoBehaviour
{
    [Header("References")]
    [Tooltip("現在のゲームステータスを参照します")]
    [SerializeField] private CurrentGameStatus currentGameStatus;
    
    [Tooltip("ProgressManagerへの参照")]
    [SerializeField] private ProgressManager ProgressManager;

    [Header("Monitor Settings")]
    [Tooltip("監視の更新間隔（秒）")]
    [SerializeField] private float checkInterval = 0.1f;

    private float lastCheckTime = 0f;
    private HashSet<string> acquiredProgressKeys = new HashSet<string>();

    private void Start()
    {
        // ProgressManagerが見つからない場合は自動検索
        if (ProgressManager == null)
        {
            ProgressManager = FindFirstObjectByType<ProgressManager>();
        }

        if (ProgressManager == null)
        {
            Debug.LogWarning("GridMonitor: ProgressManagerが見つかりません");
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
        if (currentGameStatus == null || ProgressManager == null)
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

        // 各座標で.Sと#S、.Hと#H、.Cと#Cが一致しているかチェック
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

                // .S, .H, .Cをチェック
                if (massBaseChar == '.')
                {
                    foreach (var key in massKeys)
                    {
                        if (key == "S" || key == "H" || key == "C")
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

                                    // #S, #H, #Cをチェック
                                    if (rockBaseChar == '#' && rockKeys.Contains(key))
                                    {
                                        // 条件を満たしている
                                        Vector2Int gridPos = new Vector2Int(w, h);
                                        string progressKey = $"{key}_{gridPos.x}_{gridPos.y}";

                                        if (!acquiredProgressKeys.Contains(progressKey))
                                        {
                                            acquiredProgressKeys.Add(progressKey);
                                            ProgressManager.SetProgressAcquired(gridPos, key);
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

    /// <summary>
    /// 現在のグリッド状態に基づいてProgressの状態を再計算します
    /// </summary>
    public void RecalculateProgress()
    {
        if (currentGameStatus == null || ProgressManager == null)
        {
            return;
        }

        // まず、すべてのProgressアイテムを初期状態にリセット
        var progressItems = ProgressManager.GetProgressItems();
        foreach (var item in progressItems)
        {
            if (item != null && item.gameObject != null)
            {
                item.isAcquired = false;
                Transform acquiredTransform = item.gameObject.transform.Find("Acquired");
                Transform notAcquiredTransform = item.gameObject.transform.Find("NotAcquired");
                if (acquiredTransform != null)
                {
                    acquiredTransform.gameObject.SetActive(false);
                }
                if (notAcquiredTransform != null)
                {
                    notAcquiredTransform.gameObject.SetActive(true);
                }
            }
        }

        // acquiredProgressKeysもクリア
        acquiredProgressKeys.Clear();

        // 現在のグリッド状態をチェックして、条件を満たしているProgressをAcquiredにする
        StageDatabase.StageData stageData = currentGameStatus.GetCurrentStageData();
        if (stageData == null || stageData.massStatus == null || stageData.rockStatus == null)
        {
            return;
        }

        List<StageDatabase.RowData> massStatus = stageData.massStatus;
        List<StageDatabase.RowData> rockStatus = stageData.rockStatus;

        // 各パターンキー（S, H, C）ごとに、条件を満たしているアイテムのリストを作成
        Dictionary<string, List<Vector2Int>> satisfiedPositions = new Dictionary<string, List<Vector2Int>>();
        satisfiedPositions["S"] = new List<Vector2Int>();
        satisfiedPositions["H"] = new List<Vector2Int>();
        satisfiedPositions["C"] = new List<Vector2Int>();

        // 各座標で.Sと#S、.Hと#H、.Cと#Cが一致しているかチェック
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

                // .S, .H, .Cをチェック
                if (massBaseChar == '.')
                {
                    foreach (var key in massKeys)
                    {
                        if (key == "S" || key == "H" || key == "C")
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

                                    // #S, #H, #Cをチェック
                                    if (rockBaseChar == '#' && rockKeys.Contains(key))
                                    {
                                        // 条件を満たしている
                                        Vector2Int gridPos = new Vector2Int(w, h);
                                        satisfiedPositions[key].Add(gridPos);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // 各パターンキーごとに、条件を満たしているアイテムの数だけ順番にAcquiredにする
        foreach (var kvp in satisfiedPositions)
        {
            string key = kvp.Key;
            List<Vector2Int> positions = kvp.Value;
            
            // 条件を満たしているアイテムの数だけ、順番にSetProgressAcquiredを呼び出す
            for (int i = 0; i < positions.Count; i++)
            {
                Vector2Int gridPos = positions[i];
                string progressKey = $"{key}_{gridPos.x}_{gridPos.y}";
                
                if (!acquiredProgressKeys.Contains(progressKey))
                {
                    acquiredProgressKeys.Add(progressKey);
                    ProgressManager.SetProgressAcquired(gridPos, key);
                }
            }
        }
    }
}

