using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public class RangeSelectorItemBehavior : MonoBehaviour, IPointerClickHandler
{
    [Header("Prefabs")]
    [Tooltip("RangeSelectorのPrefabをアサインします")]
    [SerializeField] private GameObject rangeSelectorPrefab;
    
    // 論理サイズ（グリッド上のサイズ）を保持。未設定(0,0)の場合はtransform.localScaleを使用（互換性のため）
    private Vector2Int logicalSize = Vector2Int.zero;
    
    // このアイテムのインデックス（RangeSelectorItemGenaratorで設定される）
    [SerializeField] private int itemIndex = -1;

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

        // 表示のちらつきを防ぐため、既に存在する場合は削除（通常はないはずだが）
        var existingSelectors = GameObject.FindGameObjectsWithTag("RangeSelector"); // 必要ならTag等で検索
        // RangeSelectorBehaviorがシングルトン的な挙動をするならここで整理してもいいが、
        // 既存実装では複数生成されないように管理されているかは不明。
        // ここでは単純に新規生成する。

        // このオブジェクトのスケールからサイズを取得（論理サイズ優先）
        float width, height;
        if (logicalSize.x > 0 && logicalSize.y > 0)
        {
            width = logicalSize.x;
            height = logicalSize.y;
        }
        else
        {
            width = transform.localScale.x;
            height = transform.localScale.y;
        }
        Vector3 targetScale = new Vector3(width, height, 1f);
        
        // RangeSelectorParentをシーン内から検索
        GameObject parentObject = GameObject.Find("RangeSelectorParent");
        Transform parent = parentObject != null ? parentObject.transform : null;
        
        // Itemの位置に生成する（直後にBehavior側でマウス追従が始まる）
        // Parentがある場合はParent下に入れる
        GameObject instance;
        if (parent != null)
        {
            instance = Instantiate(rangeSelectorPrefab, parent);
        }
        else
        {
            instance = Instantiate(rangeSelectorPrefab);
        }
        
        if (instance != null)
        {
            // 位置を設定（取り急ぎItemと同じ位置）
            instance.transform.position = transform.position;
            
            // 同じサイズにスケールを設定
            instance.transform.localScale = targetScale;
            instance.name = "RangeSelector";

            // Selectorに自身を登録
            var behavior = instance.GetComponent<RangeSelectorBehavior>();
            if (behavior != null)
            {
                behavior.SetSourceItem(this);
            }
            
            Debug.Log($"RangeSelectorを生成しました: サイズ({targetScale.x}, {targetScale.y})");
        }
    }

    /// <summary>
    /// アイテムの論理サイズ（グリッド単位）を設定します。
    /// これを設定すると、見た目のスケールに関わらず、指定されたサイズでRangeSelectorが生成されます。
    /// </summary>
    public void SetLogicalSize(int width, int height)
    {
        logicalSize = new Vector2Int(width, height);
    }

    /// <summary>
    /// アイテムのインデックスを設定します
    /// </summary>
    public void SetItemIndex(int index)
    {
        itemIndex = index;
    }

    /// <summary>
    /// アイテムのインデックスを取得します
    /// </summary>
    public int GetItemIndex()
    {
        return itemIndex;
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

