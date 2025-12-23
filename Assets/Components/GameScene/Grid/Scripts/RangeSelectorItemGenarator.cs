using System.Collections.Generic;
using UnityEngine;

public class RangeSelectorItemGenarator : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("RangeSelectorのPrefabをアサインします（1x1サイズ）")]
    [SerializeField] private GameObject rangeSelectorPrefab;

    [Header("Parent Objects")]
    [Tooltip("RangeSelectorItemを生成する際の親GameObjectをアサインします。未設定の場合はこのGameObjectが親になります")]
    [SerializeField] private Transform rangeSelectorItemParent;

    [Header("References")]
    [Tooltip("現在のゲームステータス（ステージ番号だけを参照します）")]
    [SerializeField] private CurrentGameStatus currentGameStatus;

    [Tooltip("ステージデータベース。ここからRangeSelectorItemの情報を取得します")]
    [SerializeField] private StageDatabase stageDatabase;

    [Header("Settings")]
    [Tooltip("アイテム間の縦方向の間隔")]
    [SerializeField] private float itemSpacing = 1.0f;
    
    // アイテムの表示スケール（機能的なサイズではなく、見た目の大きさの係数）
    private const float ItemScale = 0.7f;

    private void Start()
    {
        GenerateItems();
    }

    /// <summary>
    /// 範囲選択アイテムを生成します
    /// </summary>
    public void GenerateItems()
    {
        // 既存のアイテムをクリア
        ClearItems();

        if (rangeSelectorPrefab == null)
        {
            Debug.LogError("RangeSelectorPrefabがアサインされていません");
            return;
        }

        // CurrentGameStatusからデータを取得
        List<StageDatabase.RangeSelectorItemData> items = null;

        if (stageDatabase != null)
        {
            int stageIndex = currentGameStatus != null ? currentGameStatus.GetCurrentStageIndex() : 0;
            StageDatabase.StageData stageData = stageDatabase.GetStageData(stageIndex);
            if (stageData != null)
                items = stageData.rangeSelectorItems;
        }

        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("RangeSelectorItemが設定されていません");
            return;
        }

        Transform parent = rangeSelectorItemParent != null ? rangeSelectorItemParent : transform;
        if (parent == null)
        {
            Debug.LogError("親Transformがnullです");
            return;
        }

        // 親の位置を取得
        Vector3 parentPosition = parent.position;
        float currentY = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            try
            {
                StageDatabase.RangeSelectorItemData itemData = items[i];
                if (itemData == null)
                {
                    Debug.LogWarning($"RangeSelectorItem[{i}]がnullです");
                    continue;
                }

                int height = Mathf.Max(1, itemData.height);
                int width = Mathf.Max(1, itemData.width);

                // 最初のアイテムのY位置を(0, -VisualH/2)にする（親の位置を基準）
                float visualHeight = height * ItemScale;
                float visualWidth = width * ItemScale;
                
                if (i == 0)
                {
                    currentY = -visualHeight * 0.5f;
                }

                // アイテムの中心位置を計算（親の位置を基準）
                Vector3 itemCenter = parentPosition + new Vector3(0f, currentY, 0f);

                // 範囲選択アイテムを生成（1つのオブジェクトのスケールを変更）
                GameObject instance = Instantiate(rangeSelectorPrefab, itemCenter, Quaternion.identity, parent);
                if (instance != null)
                {
                    // スケールを変更してH*W * 0.7のサイズにする（見た目）
                    instance.transform.localScale = new Vector3(visualWidth, visualHeight, 1f);
                    instance.name = $"RangeSelectorItem_{i}";
                    
                    // 論理サイズ（実際に生成されるセレクターのサイズ）は元の大きさをセット
                    var itemBehavior = instance.GetComponent<RangeSelectorItemBehavior>();
                    if (itemBehavior != null)
                    {
                        itemBehavior.SetLogicalSize(width, height);
                    }
                }

                // 次のアイテムの位置を計算
                // 現在のアイテムの高さの半分 + 間隔 + 次のアイテムの高さの半分
                if (i < items.Count - 1)
                {
                    int nextHeight = Mathf.Max(1, items[i + 1] != null ? items[i + 1].height : 1);
                    float nextVisualHeight = nextHeight * ItemScale;
                    currentY -= (visualHeight * 0.5f + itemSpacing + nextVisualHeight * 0.5f);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RangeSelectorItem[{i}]の生成中にエラーが発生しました: {e.Message}");
            }
        }

        Debug.Log($"範囲選択アイテムを生成しました: {items.Count}個");
    }

    /// <summary>
    /// 既存のアイテムをクリアします
    /// </summary>
    public void ClearItems()
    {
        try
        {
            Transform parentTransform = rangeSelectorItemParent != null ? rangeSelectorItemParent : transform;
            if (parentTransform != null)
            {
                // 無限ループを防ぐため、子オブジェクトの数を事前に取得
                int childCount = parentTransform.childCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    if (parentTransform.childCount <= i)
                    {
                        break; // 安全策：インデックスが範囲外になったら終了
                    }

                    Transform child = parentTransform.GetChild(i);
                    if (child != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(child.gameObject);
                        }
                        else
                        {
                            DestroyImmediate(child.gameObject);
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"アイテムのクリア中にエラーが発生しました: {e.Message}");
        }
    }
}

