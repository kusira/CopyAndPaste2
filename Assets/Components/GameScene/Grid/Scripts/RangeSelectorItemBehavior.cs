using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public class RangeSelectorItemBehavior : MonoBehaviour, IPointerClickHandler
{
    [Header("Prefabs")]
    [Tooltip("RangeSelectorのPrefabをアサインします")]
    [SerializeField] private GameObject rangeSelectorPrefab;

    /// <summary>
    /// ポインタークリック時の処理（新しいInputSystem対応）
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        GenerateRangeSelector();
    }

    /// <summary>
    /// RangeSelectorを生成します
    /// </summary>
    private void GenerateRangeSelector()
    {
        if (rangeSelectorPrefab == null)
        {
            Debug.LogWarning("RangeSelectorPrefabがアサインされていません");
            return;
        }

        // このオブジェクトのスケールからサイズを取得
        Vector3 scale = transform.localScale;
        float selectorWidth = scale.x;
        float selectorHeight = scale.y;

        // グリッドのサイズを取得（StageDatabaseから）
        int gridWidth = 0;
        int gridHeight = 0;

        StageDatabase.StageData stageData = GetStageData();
        if (stageData != null && stageData.massStatus != null && stageData.massStatus.Count > 0)
        {
            gridHeight = stageData.massStatus.Count;
            if (stageData.massStatus[0] != null && stageData.massStatus[0].columns != null)
            {
                gridWidth = stageData.massStatus[0].columns.Count;
            }
        }

        if (gridWidth == 0 || gridHeight == 0)
        {
            Debug.LogWarning("グリッドのサイズが取得できませんでした");
            return;
        }

        // RangeSelectorParentをシーン内から検索
        GameObject parentObject = GameObject.Find("RangeSelectorParent");
        Transform parent = parentObject != null ? parentObject.transform : null;
        
        if (parent == null)
        {
            Debug.LogWarning("RangeSelectorParentが見つかりませんでした");
            return;
        }

        // RangeSelectorParentの位置を取得
        Vector3 parentPosition = parent.position;

        // RangeSelectorParentの位置を(0,0)として、グリッドの左上とRangeSelectorの左上を合わせる
        // 計算式: ((-グリッドの幅 + RangeSelectorの幅) / 2, (グリッドの高さ - RangeSelectorの高さ) / 2)
        float localCenterX = (-gridWidth + selectorWidth) * 0.5f;
        float localCenterY = (gridHeight - selectorHeight) * 0.5f;
        
        // 親の位置を基準にワールド座標を計算
        Vector3 worldPosition = parentPosition + new Vector3(localCenterX, localCenterY, 0f);

        // RangeSelectorを生成（親の下に生成）
        GameObject instance = Instantiate(rangeSelectorPrefab, parent);
        
        if (instance != null)
        {
            // ワールド座標で位置を設定
            instance.transform.position = worldPosition;
            
            // 同じサイズにスケールを設定
            instance.transform.localScale = new Vector3(selectorWidth, selectorHeight, 1f);
            instance.name = "RangeSelector";
            
            Debug.Log($"RangeSelectorを生成しました: サイズ({selectorWidth}, {selectorHeight}), ワールド位置({worldPosition.x}, {worldPosition.y}), ローカル位置({localCenterX}, {localCenterY}), グリッドサイズ({gridWidth}, {gridHeight})");
        }
    }

    /// <summary>
    /// StageDatabase から現在のステージデータを取得します
    /// </summary>
    private StageDatabase.StageData GetStageData()
    {
        // CurrentGameStatusからステージ番号を取得
        int stageIndex = 0;
        CurrentGameStatus currentGameStatus = Object.FindFirstObjectByType<CurrentGameStatus>();
        if (currentGameStatus != null)
        {
            stageIndex = currentGameStatus.GetCurrentStageIndex();
        }

        // GridGeneratorからStageDatabaseを取得
        GridGenerator gridGenerator = Object.FindFirstObjectByType<GridGenerator>();
        if (gridGenerator == null)
        {
            Debug.LogWarning("GridGeneratorがシーンに見つかりませんでした");
            return null;
        }

        FieldInfo dbField = typeof(GridGenerator).GetField("stageDatabase", BindingFlags.NonPublic | BindingFlags.Instance);
        if (dbField == null)
        {
            Debug.LogWarning("GridGeneratorからStageDatabaseフィールドを取得できませんでした");
            return null;
        }

        StageDatabase stageDatabase = dbField.GetValue(gridGenerator) as StageDatabase;
        if (stageDatabase == null)
        {
            Debug.LogWarning("StageDatabaseがGridGeneratorにアサインされていません");
            return null;
        }

        return stageDatabase.GetStageData(stageIndex);
    }
}

