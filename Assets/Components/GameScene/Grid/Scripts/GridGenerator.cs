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
        try
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
                        if (!string.IsNullOrEmpty(massValue) && massValue == ".")
                        {
                            if (massPrefab != null)
                            {
                                Transform parent = massParent != null ? massParent : transform;
                                if (parent != null)
                                {
                                    GameObject instance = Instantiate(massPrefab, position, Quaternion.identity, parent);
                                    if (instance != null)
                                    {
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
                            if (!string.IsNullOrEmpty(rockValue) && rockValue == "#")
                            {
                                if (rockPrefab != null)
                                {
                                    Transform parent = rockParent != null ? rockParent : transform;
                                    if (parent != null)
                                    {
                                        GameObject instance = Instantiate(rockPrefab, position, Quaternion.identity, parent);
                                        if (instance != null)
                                        {
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

}

