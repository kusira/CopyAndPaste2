using System.Collections.Generic;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("MassのPrefabをアサインします")]
    [SerializeField] private GameObject massPrefab;
    
    [Tooltip("RockのPrefabをアサインします")]
    [SerializeField] private GameObject rockPrefab;

    [Header("Parent Objects")]
    [Tooltip("Massを生成する際の親GameObjectをアサインします。未設定の場合はこのGameObjectが親になります")]
    [SerializeField] private Transform massParent;
    
    [Tooltip("Rockを生成する際の親GameObjectをアサインします。未設定の場合はこのGameObjectが親になります")]
    [SerializeField] private Transform rockParent;

    [Header("References")]
    [Tooltip("現在のゲームステータスを参照します。設定されている場合、ここからステージデータを取得します")]
    [SerializeField] private CurrentGameStatus currentGameStatus;

    private void Start()
    {
        GenerateGrid();
    }

    /// <summary>
    /// グリッドを生成します
    /// </summary>
    public void GenerateGrid()
    {
        // 既存のグリッドをクリア
        ClearGrid();

        // CurrentGameStatusからデータを取得
        List<StageDatabase.RowData> massStatus = null;
        List<StageDatabase.RowData> rockStatus = null;

        if (currentGameStatus != null)
        {
            StageDatabase.StageData stageData = currentGameStatus.GetCurrentStageData();
            if (stageData != null)
            {
                massStatus = stageData.massStatus;
                rockStatus = stageData.rockStatus;
            }
        }

        if (massStatus == null || massStatus.Count == 0)
        {
            Debug.LogWarning("MassStatusが設定されていません");
            return;
        }

        int height = massStatus.Count;
        int width = massStatus[0] != null && massStatus[0].columns != null ? massStatus[0].columns.Count : 0;

        if (width == 0)
        {
            Debug.LogWarning("MassStatusの幅が0です");
            return;
        }

        // グリッドの中心を計算（(0,0)を中心に配置）
        float startX = -(width - 1) * 0.5f;
        float startY = -(height - 1) * 0.5f;

        for (int h = 0; h < height; h++)
        {
            if (massStatus[h] == null || massStatus[h].columns == null || massStatus[h].columns.Count != width)
            {
                Debug.LogWarning($"MassStatusの行{h}の幅が正しくありません");
                continue;
            }

            for (int w = 0; w < width; w++)
            {
                Vector3 position = new Vector3(startX + w, startY + h, 0f);

                // MassStatusをチェック
                string massValue = massStatus[h].columns[w];
                if (!string.IsNullOrEmpty(massValue) && massValue == ".")
                {
                    if (massPrefab != null)
                    {
                        Transform parent = massParent != null ? massParent : transform;
                        Instantiate(massPrefab, position, Quaternion.identity, parent);
                    }
                    else
                    {
                        Debug.LogWarning("MassPrefabがアサインされていません");
                    }
                }

                // RockStatusをチェック
                if (rockStatus != null && h < rockStatus.Count && rockStatus[h] != null && 
                    rockStatus[h].columns != null && w < rockStatus[h].columns.Count)
                {
                    string rockValue = rockStatus[h].columns[w];
                    if (!string.IsNullOrEmpty(rockValue) && rockValue == "#")
                    {
                        if (rockPrefab != null)
                        {
                            Transform parent = rockParent != null ? rockParent : transform;
                            Instantiate(rockPrefab, position, Quaternion.identity, parent);
                        }
                        else
                        {
                            Debug.LogWarning("RockPrefabがアサインされていません");
                        }
                    }
                }
            }
        }

        Debug.Log($"グリッドを生成しました: {width}x{height}");
    }

    /// <summary>
    /// 既存のグリッドをクリアします
    /// </summary>
    public void ClearGrid()
    {
        // MassParentの子オブジェクトをクリア
        Transform massParentTransform = massParent != null ? massParent : transform;
        while (massParentTransform.childCount > 0)
        {
            Destroy(massParentTransform.GetChild(0).gameObject);
        }

        // RockParentの子オブジェクトをクリア（MassParentと異なる場合のみ）
        Transform rockParentTransform = rockParent != null ? rockParent : transform;
        if (rockParentTransform != massParentTransform)
        {
            while (rockParentTransform.childCount > 0)
            {
                Destroy(rockParentTransform.GetChild(0).gameObject);
            }
        }
    }

}

