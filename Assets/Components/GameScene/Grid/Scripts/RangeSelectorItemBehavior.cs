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

        // CurrentGameStatusをシーン内から検索
        CurrentGameStatus currentGameStatus = Object.FindFirstObjectByType<CurrentGameStatus>();
        if (currentGameStatus == null)
        {
            Debug.LogWarning("CurrentGameStatusが見つかりませんでした");
            return;
        }

        // グリッドのサイズを取得
        int gridWidth = 0;
        int gridHeight = 0;

        StageDatabase.StageData stageData = currentGameStatus.GetCurrentStageData();
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
}

