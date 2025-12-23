using System.Collections.Generic;
using UnityEngine;

public class ProgressGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("ProgressStarのPrefabをアサインします")]
    [SerializeField] private GameObject progressStarPrefab;
    
    [Tooltip("ProgressPentagonのPrefabをアサインします")]
    [SerializeField] private GameObject progressPentagonPrefab;
    
    [Tooltip("ProgressCircleのPrefabをアサインします")]
    [SerializeField] private GameObject progressCirclePrefab;

    [Header("References")]
    [Tooltip("現在のゲームステータスを参照します")]
    [SerializeField] private CurrentGameStatus currentGameStatus;
    
    [Tooltip("ステージデータベース。ここからステージデータを取得します")]
    [SerializeField] private StageDatabase stageDatabase;

    [Header("Layout Settings")]
    [Tooltip("アイテム間の間隔")]
    [SerializeField] private float itemSpacing = 70;
    
    [Tooltip("行間の間隔")]
    [SerializeField] private float rowSpacing = 80;
    
    [Tooltip("各行の右方向へのオフセット（段差）")]
    [SerializeField] private float rowOffset = 0.5f;

    private List<ProgressItemData> createdProgressItems = new List<ProgressItemData>();

    /// <summary>
    /// Progressアイテムのデータを保持するクラス
    /// </summary>
    [System.Serializable]
    public class ProgressItemData
    {
        public GameObject gameObject;
        public string patternKey; // "S", "P", "C"
        public Vector2Int gridPosition; // グリッド座標 (w, h)
        public bool isAcquired;

        public ProgressItemData(GameObject obj, string key, Vector2Int pos)
        {
            gameObject = obj;
            patternKey = key;
            gridPosition = pos;
            isAcquired = false;
        }
    }

    private void Start()
    {
        CreateProgressItems();
    }

    /// <summary>
    /// グリッドの初期盤面から.S, .P, .Cの数をカウントしてProgressアイテムを生成します
    /// </summary>
    public void CreateProgressItems()
    {
        // 既存のアイテムをクリア
        ClearProgressItems();

        // ステージデータを取得
        StageDatabase.StageData stageData = null;
        if (currentGameStatus != null)
        {
            stageData = currentGameStatus.GetCurrentStageData();
        }

        if (stageData == null && stageDatabase != null)
        {
            int stageIndex = currentGameStatus != null ? currentGameStatus.GetCurrentStageIndex() : 0;
            stageData = stageDatabase.GetStageData(stageIndex);
        }

        if (stageData == null || stageData.massStatus == null)
        {
            Debug.LogWarning("ProgressManager: ステージデータが取得できません");
            return;
        }

        // .S, .P, .Cの座標を記録
        List<ProgressItemData> starItems = new List<ProgressItemData>();
        List<ProgressItemData> pentagonItems = new List<ProgressItemData>();
        List<ProgressItemData> circleItems = new List<ProgressItemData>();

        List<StageDatabase.RowData> massStatus = stageData.massStatus;
        for (int h = 0; h < massStatus.Count; h++)
        {
            if (massStatus[h] == null || massStatus[h].columns == null) continue;

            for (int w = 0; w < massStatus[h].columns.Count; w++)
            {
                string cellValue = massStatus[h].columns[w];
                if (string.IsNullOrEmpty(cellValue)) continue;

                char baseChar;
                List<string> keys = new List<string>();
                RangeSelectorHelper.ParseCell(cellValue, out baseChar, keys);

                if (baseChar == '.')
                {
                    // キーをチェックして座標を記録
                    foreach (var key in keys)
                    {
                        Vector2Int gridPos = new Vector2Int(w, h);
                        if (key == "S")
                        {
                            starItems.Add(new ProgressItemData(null, "S", gridPos));
                        }
                        else if (key == "P")
                        {
                            pentagonItems.Add(new ProgressItemData(null, "P", gridPos));
                        }
                        else if (key == "C")
                        {
                            circleItems.Add(new ProgressItemData(null, "C", gridPos));
                        }
                    }
                }
            }
        }

        Debug.Log($"ProgressGenerator: S={starItems.Count}, P={pentagonItems.Count}, C={circleItems.Count}");

        // 左上を基準として配置
        Vector3 startPosition = transform.position;

        // Sを横に並べる
        for (int i = 0; i < starItems.Count; i++)
        {
            if (progressStarPrefab != null)
            {
                Vector3 position = startPosition + new Vector3(i * itemSpacing, 0, 0);
                GameObject instance = Instantiate(progressStarPrefab, position, Quaternion.identity, transform);
                if (instance != null)
                {
                    starItems[i].gameObject = instance;
                    SetProgressItemState(instance, false);
                    createdProgressItems.Add(starItems[i]);
                }
            }
        }

        // Pをその下に、少し右にずらして並べる
        for (int i = 0; i < pentagonItems.Count; i++)
        {
            if (progressPentagonPrefab != null)
            {
                Vector3 position = startPosition + new Vector3(rowOffset + i * itemSpacing, -rowSpacing, 0);
                GameObject instance = Instantiate(progressPentagonPrefab, position, Quaternion.identity, transform);
                if (instance != null)
                {
                    pentagonItems[i].gameObject = instance;
                    SetProgressItemState(instance, false);
                    createdProgressItems.Add(pentagonItems[i]);
                }
            }
        }

        // Cをその下に、さらに右にずらして並べる
        for (int i = 0; i < circleItems.Count; i++)
        {
            if (progressCirclePrefab != null)
            {
                Vector3 position = startPosition + new Vector3(rowOffset * 2 + i * itemSpacing, -rowSpacing * 2, 0);
                GameObject instance = Instantiate(progressCirclePrefab, position, Quaternion.identity, transform);
                if (instance != null)
                {
                    circleItems[i].gameObject = instance;
                    SetProgressItemState(instance, false);
                    createdProgressItems.Add(circleItems[i]);
                }
            }
        }
    }

    /// <summary>
    /// 生成したProgressアイテムをクリアします
    /// </summary>
    public void ClearProgressItems()
    {
        foreach (var item in createdProgressItems)
        {
            if (item != null && item.gameObject != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(item.gameObject);
                }
                else
                {
                    DestroyImmediate(item.gameObject);
                }
            }
        }
        createdProgressItems.Clear();
    }

    /// <summary>
    /// Progressアイテムの状態を設定します（Acquired/NotAcquired）
    /// </summary>
    private void SetProgressItemState(GameObject progressItem, bool acquired)
    {
        if (progressItem == null) return;

        Transform acquiredTransform = progressItem.transform.Find("Acquired");
        Transform notAcquiredTransform = progressItem.transform.Find("NotAcquired");

        if (acquiredTransform != null)
        {
            acquiredTransform.gameObject.SetActive(acquired);
        }
        if (notAcquiredTransform != null)
        {
            notAcquiredTransform.gameObject.SetActive(!acquired);
        }
    }

    /// <summary>
    /// 指定された座標とパターンキーに対応するProgressアイテムの条件が満たされたことを記録します
    /// 既にAcquiredになっているアイテムの数を数えて、その数+1個目のアイテム（左側から）をAcquiredにします
    /// </summary>
    public void SetProgressAcquired(Vector2Int gridPosition, string patternKey)
    {
        // 既にAcquiredになっているアイテムの数を数える
        int acquiredCount = 0;
        foreach (var item in createdProgressItems)
        {
            if (item != null && item.isAcquired)
            {
                acquiredCount++;
            }
        }

        // その数+1個目のアイテム（左側から）をAcquiredにする
        int targetIndex = acquiredCount; // 0-indexedなので、acquiredCountが次のアイテムのインデックス
        if (targetIndex < createdProgressItems.Count)
        {
            var targetItem = createdProgressItems[targetIndex];
            if (targetItem != null && targetItem.gameObject != null && !targetItem.isAcquired)
            {
                targetItem.isAcquired = true;
                SetProgressItemState(targetItem.gameObject, true);
                Debug.Log($"ProgressGenerator: {targetItem.patternKey} at ({targetItem.gridPosition.x}, {targetItem.gridPosition.y}) をAcquiredにしました（{acquiredCount + 1}個目）");
            }
        }
    }

    /// <summary>
    /// すべてのProgressアイテムの状態を取得します
    /// </summary>
    public List<ProgressItemData> GetProgressItems()
    {
        return createdProgressItems;
    }
}

