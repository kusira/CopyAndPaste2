using System.Collections.Generic;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("MassのPrefabをアサインします")]
    [SerializeField] private GameObject massPrefab;
    
    [Tooltip("RockのPrefabをアサインします")]
    [SerializeField] private GameObject rockPrefab;

    [Tooltip("GridFrameのPrefabをアサインします。GridParentの直下に生成されます")]
    [SerializeField] private GameObject gridFramePrefab;
    
    [Header("Parent Objects")]
    [Tooltip("Massを生成する際の親GameObjectをアサインします。未設定の場合はこのGameObjectが親になります")]
    [SerializeField] private Transform massParent;
    
    [Tooltip("Rockを生成する際の親GameObjectをアサインします。未設定の場合はこのGameObjectが親になります")]
    [SerializeField] private Transform rockParent;

    [Header("References")]
    [Tooltip("現在のゲームステータスを参照します。設定されている場合、ここからステージデータを取得します")]
    [SerializeField] private CurrentGameStatus currentGameStatus;

    [Tooltip("ステージデータベース。ここからステージデータを取得します")]
    [SerializeField] private StageDatabase stageDatabase;
    private void Start()
    {
        GenerateGrid();
    }

    /// <summary>
    /// グリッドを生成します
    /// </summary>
    public void GenerateGrid()
    {
        try
        {
            // 既存のグリッドをクリア
            ClearGrid();

            // CurrentGameStatusからステージデータを取得
            List<StageDatabase.RowData> massStatus = null;
            List<StageDatabase.RowData> rockStatus = null;

            // StageDatabaseからデータを取得（CurrentGameStatus経由を優先）
            StageDatabase.StageData stageData = null;
            if (currentGameStatus != null)
            {
                stageData = currentGameStatus.GetCurrentStageData();
            }

            // 直接Databaseから取得（フォールバック）
            if (stageData == null && stageDatabase != null)
            {
                int stageIndex = currentGameStatus != null ? currentGameStatus.GetCurrentStageIndex() : 0;
                stageData = stageDatabase.GetStageData(stageIndex);
            }

            if (stageData != null)
            {
                massStatus = stageData.massStatus;
                rockStatus = stageData.rockStatus;
            }

            if (massStatus == null || massStatus.Count == 0)
            {
                Debug.LogWarning("MassStatusが設定されていません");
                return;
            }

            int height = massStatus.Count;
            if (height == 0)
            {
                Debug.LogWarning("MassStatusの高さが0です");
                return;
            }

            int width = massStatus[0] != null && massStatus[0].columns != null ? massStatus[0].columns.Count : 0;

            if (width == 0)
            {
                Debug.LogWarning("MassStatusの幅が0です");
                return;
            }

            // 親のTransformの位置を取得（MassParentを基準にする）
            Transform parentTransform = massParent != null ? massParent : transform;
            if (parentTransform == null)
            {
                Debug.LogError("親Transformがnullです");
                return;
            }

            Vector3 parentPosition = parentTransform.position;

            // グリッドの中心を計算（親の位置を中心に配置）
            float offsetX = -(width - 1) * 0.5f;
            float offsetY = -(height - 1) * 0.5f;

            // GridFrameを生成してサイズを調整（Mass/Rock生成と同タイミング）
            CreateAndUpdateGridFrame(width, height);

            int generatedCount = 0;
            int errorCount = 0;

            for (int h = 0; h < height; h++)
            {
                if (massStatus[h] == null || massStatus[h].columns == null || massStatus[h].columns.Count != width)
                {
                    Debug.LogWarning($"MassStatusの行{h}の幅が正しくありません");
                    errorCount++;
                    continue;
                }

                for (int w = 0; w < width; w++)
                {
                    try
                    {
                        // 親の位置を基準にグリッドの中心が一致するように配置
                        Vector3 position = parentPosition + new Vector3(offsetX + w, offsetY + h, 0f);

                        // MassStatusをチェック
                        string massValue = massStatus[h].columns[w];
                        bool hasMass = !string.IsNullOrEmpty(massValue);
                        if (hasMass)
                        {
                            if (massPrefab != null)
                            {
                                Transform parent = massParent != null ? massParent : transform;
                                if (parent != null)
                                {
                                    GameObject instance = Instantiate(massPrefab, position, Quaternion.identity, parent);
                                    if (instance != null)
                                    {
                                        // ギミック適用
                                        var assigner = instance.GetComponent<MassPatternAssigner>();
                                        if (assigner != null)
                                        {
                                            assigner.BuildDict(); // 辞書構築（エディタ実行時用）
                                            assigner.ApplyPatterns(massValue);
                                        }
                                        generatedCount++;
                                    }
                                }
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
                            bool hasRock = !string.IsNullOrEmpty(rockValue) && rockValue.Length > 0 && rockValue[0] == '#';
                            if (hasRock)
                            {
                                if (rockPrefab != null)
                                {
                                    Transform parent = rockParent != null ? rockParent : transform;
                                    if (parent != null)
                                    {
                                        GameObject instance = Instantiate(rockPrefab, position, Quaternion.identity, parent);
                                        if (instance != null)
                                        {
                                            // ギミック適用
                                            var assigner = instance.GetComponent<RockPatternAssigner>();
                                            if (assigner != null)
                                            {
                                                assigner.BuildDict(); // 辞書構築（エディタ実行時用）
                                                assigner.ApplyPatterns(rockValue);
                                            }
                                            generatedCount++;
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning("RockPrefabがアサインされていません");
                                }
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"グリッド生成中にエラーが発生しました (h:{h}, w:{w}): {e.Message}");
                        errorCount++;
                    }
                }
            }

            if (errorCount > 0)
            {
                Debug.LogWarning($"グリッドを生成しました: {width}x{height} (生成数: {generatedCount}, エラー数: {errorCount})");
            }
            else
            {
                Debug.Log($"グリッドを生成しました: {width}x{height} (生成数: {generatedCount})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"グリッド生成中に重大なエラーが発生しました: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 既存のグリッドをクリアします
    /// </summary>
    public void ClearGrid()
    {
        try
        {
            // GridFrameをクリア
            Transform gridParentTransform = null;
            if (massParent != null)
            {
                gridParentTransform = massParent.parent;
            }
            else if (rockParent != null)
            {
                gridParentTransform = rockParent.parent;
            }
            else
            {
                gridParentTransform = transform.parent;
            }

            if (gridParentTransform != null)
            {
                ClearGridFrame(gridParentTransform);
            }

            // MassParentの子オブジェクトをクリア
            Transform massParentTransform = massParent != null ? massParent : transform;
            if (massParentTransform != null)
            {
                // 無限ループを防ぐため、子オブジェクトの数を事前に取得
                int massChildCount = massParentTransform.childCount;
                for (int i = massChildCount - 1; i >= 0; i--)
                {
                    if (massParentTransform.childCount <= i)
                    {
                        break; // 安全策：インデックスが範囲外になったら終了
                    }
                    
                    Transform child = massParentTransform.GetChild(i);
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

            // RockParentの子オブジェクトをクリア（MassParentと異なる場合のみ）
            Transform rockParentTransform = rockParent != null ? rockParent : transform;
            if (rockParentTransform != null && rockParentTransform != massParentTransform)
            {
                // 無限ループを防ぐため、子オブジェクトの数を事前に取得
                int rockChildCount = rockParentTransform.childCount;
                for (int i = rockChildCount - 1; i >= 0; i--)
                {
                    if (rockParentTransform.childCount <= i)
                    {
                        break; // 安全策：インデックスが範囲外になったら終了
                    }
                    
                    Transform child = rockParentTransform.GetChild(i);
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
            Debug.LogError($"グリッドのクリア中にエラーが発生しました: {e.Message}");
        }
    }

    /// <summary>
    /// GridFrameを生成してサイズを調整します
    /// </summary>
    /// <param name="gridWidth">グリッドの幅</param>
    /// <param name="gridHeight">グリッドの高さ</param>
    private void CreateAndUpdateGridFrame(int gridWidth, int gridHeight)
    {
        if (gridFramePrefab == null)
        {
            Debug.LogWarning("GridFramePrefabがアサインされていません");
            return;
        }

        // GridParentを取得（massParentの親、またはrockParentの親）
        Transform gridParentTransform = null;
        if (massParent != null)
        {
            gridParentTransform = massParent.parent;
        }
        else if (rockParent != null)
        {
            gridParentTransform = rockParent.parent;
        }
        else
        {
            // massParentとrockParentがnullの場合は、このGameObjectの親を探す
            gridParentTransform = transform.parent;
        }

        if (gridParentTransform == null)
        {
            Debug.LogWarning("GridParentが見つかりません");
            return;
        }

        // 既存のGridFrameを削除
        ClearGridFrame(gridParentTransform);

        // GridFrameを生成
        GameObject gridFrame = Instantiate(gridFramePrefab, gridParentTransform);
        if (gridFrame == null)
        {
            Debug.LogError("GridFrameの生成に失敗しました");
            return;
        }

        gridFrame.name = "GridFrame";

        // サイズを調整
        UpdateGridFrameSize(gridFrame, gridWidth, gridHeight);
    }

    /// <summary>
    /// 既存のGridFrameを削除します
    /// </summary>
    /// <param name="parent">親Transform</param>
    private void ClearGridFrame(Transform parent)
    {
        if (parent == null) return;

        // GridFrameという名前の子オブジェクトを探して削除
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name == "GridFrame")
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

    /// <summary>
    /// GridFrameのサイズをグリッドサイズに合わせて調整します
    /// </summary>
    /// <param name="gridFrame">GridFrameゲームオブジェクト</param>
    /// <param name="gridWidth">グリッドの幅</param>
    /// <param name="gridHeight">グリッドの高さ</param>
    private void UpdateGridFrameSize(GameObject gridFrame, int gridWidth, int gridHeight)
    {
        if (gridFrame == null) return;

        // アウトラインの比率: 60/860
        const float outlineRatio = 60f / 860f;
        // グリッド部分の比率: (860 - 120) / 860 = 740 / 860
        const float gridRatio = 740f / 860f;
        
        // 全体のサイズ = グリッドサイズ / グリッド比率
        // 上下左右にアウトラインがあるので、全体サイズはグリッドサイズより大きくなる
        float totalWidth = gridWidth / gridRatio;
        float totalHeight = gridHeight / gridRatio;

        // RectTransformがある場合はSizeDeltaを変更
        RectTransform rectTransform = gridFrame.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(totalWidth, totalHeight);
            Debug.Log($"GridFrameのサイズを更新しました: {totalWidth}x{totalHeight} (グリッドサイズ: {gridWidth}x{gridHeight})");
            return;
        }

        // SpriteRendererがある場合はTransformのScaleを変更
        SpriteRenderer spriteRenderer = gridFrame.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // スプライトの元のサイズを取得
            float spriteWidth = spriteRenderer.sprite.bounds.size.x;
            float spriteHeight = spriteRenderer.sprite.bounds.size.y;
            
            if (spriteWidth > 0 && spriteHeight > 0)
            {
                float scaleX = totalWidth / spriteWidth;
                float scaleY = totalHeight / spriteHeight;
                gridFrame.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                Debug.Log($"GridFrameのスケールを更新しました: {scaleX}x{scaleY} (グリッドサイズ: {gridWidth}x{gridHeight})");
                return;
            }
        }

        // TransformのScaleを直接変更（デフォルト）
        gridFrame.transform.localScale = new Vector3(totalWidth, totalHeight, 1f);
        Debug.Log($"GridFrameのスケールを更新しました: {totalWidth}x{totalHeight} (グリッドサイズ: {gridWidth}x{gridHeight})");
    }
}

