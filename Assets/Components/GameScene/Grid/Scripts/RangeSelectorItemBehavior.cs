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

            // Selectorに自身を登録
            var behavior = instance.GetComponent<RangeSelectorBehavior>();
            if (behavior != null)
            {
                behavior.SetSourceItem(this);
            }
            
            Debug.Log($"RangeSelectorを生成しました: サイズ({selectorWidth}, {selectorHeight}), ワールド位置({worldPosition.x}, {worldPosition.y}), ローカル位置({localCenterX}, {localCenterY}), グリッドサイズ({gridWidth}, {gridHeight})");
        }
    }

    /// <summary>
    /// アイテムの透明度を設定します
    /// </summary>
    public void SetAlpha(float alpha)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    /// <summary>
    /// 現在のステージデータを取得します（ランタイムではDeepCopy済みを優先）
    /// </summary>
    private StageDatabase.StageData GetStageData()
    {
        // 1. CurrentGameStatus からランタイムデータを取得（DeepCopy済み）
        CurrentGameStatus currentGameStatus = Object.FindFirstObjectByType<CurrentGameStatus>();
        if (currentGameStatus != null)
        {
            var runtimeData = currentGameStatus.GetCurrentStageData();
            if (runtimeData != null)
            {
                return runtimeData;
            }

            // ランタイムデータがまだない場合は、StageDatabase から直接取得（読み取り専用）
            StageDatabase db = currentGameStatus.GetStageDatabase();
            if (db != null)
            {
                int idx = currentGameStatus.GetCurrentStageIndex();
                return db.GetStageData(idx);
            }
        }

        // 2. フォールバック：GridGenerator から StageDatabase を取得
        GridGenerator gridGenerator = Object.FindFirstObjectByType<GridGenerator>();
        if (gridGenerator != null)
        {
            var dbField = typeof(GridGenerator).GetField("stageDatabase", BindingFlags.NonPublic | BindingFlags.Instance);
            if (dbField != null)
            {
                StageDatabase db = dbField.GetValue(gridGenerator) as StageDatabase;
                if (db != null)
                {
                    int idx = currentGameStatus != null ? currentGameStatus.GetCurrentStageIndex() : 0;
                    return db.GetStageData(idx);
                }
            }
        }

        Debug.LogWarning("ステージデータを取得できませんでした（RangeSelectorItemBehavior）");
        return null;
    }
}

