using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

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

    [Tooltip("キャラクターアニメーション制御用のCharacterAnimator")]
    [SerializeField] private CharacterAnimator characterAnimator;

    [Header("Monitor Settings")]
    [Tooltip("監視の更新間隔（秒）")]
    [SerializeField] private float checkInterval = 0.1f;

    [Header("Bloom Settings")]
    [Tooltip("Global Volumeをアサインします")]
    [SerializeField] private Volume globalVolume;

    [Tooltip("BloomのIntensityアニメーション時間（秒）")]
    [SerializeField] private float bloomAnimationDuration = 0.3f;

    [Tooltip("BloomのIntensity最大値")]
    [SerializeField] private float bloomMaxIntensity = 2f;

    [Header("Rock Emission Settings")]
    [Tooltip("Rockの親Transformをアサインします（座標からRockオブジェクトを取得するために使用）")]
    [SerializeField] private Transform rockParent;

    private float lastCheckTime = 0f;
    private HashSet<string> acquiredProgressKeys = new HashSet<string>();
    private HashSet<Vector2Int> currentlySatisfiedPositions = new HashSet<Vector2Int>();
    private Bloom bloomEffect;
    private Tween bloomTween;

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

        // CharacterAnimatorが見つからない場合は自動検索
        if (characterAnimator == null)
        {
            characterAnimator = CharacterAnimator.Instance;
        }

        // rockParentが見つからない場合は自動検索（GridGeneratorから取得）
        if (rockParent == null)
        {
            GridGenerator gridGenerator = FindFirstObjectByType<GridGenerator>();
            if (gridGenerator != null)
            {
                // GridGeneratorのrockParentを取得（リフレクションを使用）
                var rockParentField = typeof(GridGenerator).GetField("rockParent", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (rockParentField != null)
                {
                    rockParent = rockParentField.GetValue(gridGenerator) as Transform;
                }
            }
        }

        // Global VolumeからBloomエフェクトを取得
        if (globalVolume != null && globalVolume.profile != null)
        {
            if (globalVolume.profile.TryGet<Bloom>(out var bloom))
            {
                bloomEffect = bloom;
                // IntensityのOverrideStateを有効化（まだ有効でない場合）
                if (!bloomEffect.intensity.overrideState)
                {
                    bloomEffect.intensity.overrideState = true;
                }
                // 初期値を0に設定
                bloomEffect.intensity.value = 0f;
            }
            else
            {
                Debug.LogWarning("GridMonitor: Global VolumeにBloomエフェクトが見つかりません");
            }
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

        // 現在条件を満たしている座標のセットを更新
        HashSet<Vector2Int> newSatisfiedPositions = new HashSet<Vector2Int>();

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
                RSHelper.ParseCell(massValue, out massBaseChar, massKeys);

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
                                    RSHelper.ParseCell(rockValue, out rockBaseChar, rockKeys);

                                    // #S, #H, #Cをチェック
                                    if (rockBaseChar == '#' && rockKeys.Contains(key))
                                    {
                                        // 条件を満たしている
                                        Vector2Int gridPos = new Vector2Int(w, h);
                                        string progressKey = $"{key}_{gridPos.x}_{gridPos.y}";

                                        // 現在条件を満たしている座標として記録
                                        newSatisfiedPositions.Add(gridPos);

                                        if (!acquiredProgressKeys.Contains(progressKey))
                                        {
                                            acquiredProgressKeys.Add(progressKey);
                                            ProgressManager.SetProgressAcquired(gridPos, key);
                                            
                                            // Bloomエフェクトをトリガー
                                            TriggerBloomEffect();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // 条件を満たさなくなった座標を検出してacquiredProgressKeysから削除
        foreach (var pos in currentlySatisfiedPositions)
        {
            if (!newSatisfiedPositions.Contains(pos))
            {
                // 条件を満たさなくなった座標のprogressKeyを削除
                // 各パターンキー（S, H, C）をチェック
                foreach (var key in new string[] { "S", "H", "C" })
                {
                    string progressKey = $"{key}_{pos.x}_{pos.y}";
                    if (acquiredProgressKeys.Contains(progressKey))
                    {
                        acquiredProgressKeys.Remove(progressKey);
                        // 対応するProgressアイテムもリセット
                        if (ProgressManager != null)
                        {
                            ProgressManager.ResetProgressItem(pos, key);
                        }
                    }
                }
            }
        }

        // 条件を満たしている座標のEmissionColorを更新
        UpdateRockEmissionStates(newSatisfiedPositions);

        // チェック後にクリア判定を行い、クリア時はCharacterAnimatorにClearを投げる
        NotifyClearIfNeeded();
    }

    /// <summary>
    /// 監視状態をリセットします（ステージ開始時などに呼び出し）
    /// </summary>
    public void ResetMonitor()
    {
        acquiredProgressKeys.Clear();
        currentlySatisfiedPositions.Clear();
        
        // すべてのRockのEmissionColorをリセット
        if (rockParent != null)
        {
            for (int i = 0; i < rockParent.childCount; i++)
            {
                Transform child = rockParent.GetChild(i);
                if (child != null)
                {
                    RockPatternAssigner patternAssigner = child.GetComponent<RockPatternAssigner>();
                    if (patternAssigner != null)
                    {
                        patternAssigner.ResetEmission();
                    }
                }
            }
        }
    }

    /// <summary>
    /// クリア条件が満たされているか（すべてのProgressアイテムがAcquiredになっているか）を判定します
    /// </summary>
    public bool IsClearConditionMet()
    {
        if (ProgressManager == null)
        {
            ProgressManager = FindFirstObjectByType<ProgressManager>();
        }
        
        if (ProgressManager != null)
        {
            return ProgressManager.IsClearConditionMet();
        }
        
        return false;
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
                item.isGlowing = false; // isGlowingもリセット
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
                RSHelper.ParseCell(massValue, out massBaseChar, massKeys);

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
                                    RSHelper.ParseCell(rockValue, out rockBaseChar, rockKeys);

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

        // 条件を満たしている座標のセットを作成
        HashSet<Vector2Int> allSatisfiedPositions = new HashSet<Vector2Int>();
        foreach (var kvp in satisfiedPositions)
        {
            foreach (var pos in kvp.Value)
            {
                allSatisfiedPositions.Add(pos);
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

        // 条件を満たしている座標のEmissionColorを更新
        UpdateRockEmissionStates(allSatisfiedPositions);

        // 再計算後にクリア判定を行い、クリア時はCharacterAnimatorにClearを投げる
        NotifyClearIfNeeded();
    }

    /// <summary>
    /// ProgressManager経由でクリア判定を行い、クリア時はCharacterAnimatorにClearを送ります
    /// </summary>
    private void NotifyClearIfNeeded()
    {
        if (ProgressManager == null)
        {
            ProgressManager = FindFirstObjectByType<ProgressManager>();
        }

        if (ProgressManager != null && ProgressManager.IsClearConditionMet())
        {
            if (characterAnimator == null)
            {
                characterAnimator = CharacterAnimator.Instance;
            }

            if (characterAnimator != null)
            {
                characterAnimator.SetClear();
            }
        }
    }

    /// <summary>
    /// Bloomエフェクトをトリガーします（Intensityを0→最大値→0とアニメーション）
    /// </summary>
    private void TriggerBloomEffect()
    {
        if (bloomEffect == null)
        {
            return;
        }

        // 既存のTweenがあれば停止
        if (bloomTween != null && bloomTween.IsActive())
        {
            bloomTween.Kill();
        }

        // 現在のIntensityを0にリセット（既存のアニメーションを中断した場合のため）
        bloomEffect.intensity.value = 0f;

        // BloomのIntensityを0→最大値→0とアニメーション
        Sequence sequence = DOTween.Sequence();
        
        // 0→最大値
        sequence.Append(DOTween.To(
            () => bloomEffect.intensity.value,
            x => bloomEffect.intensity.value = x,
            bloomMaxIntensity,
            bloomAnimationDuration / 2f
        ).SetEase(Ease.OutQuad));
        
        // 最大値→0
        sequence.Append(DOTween.To(
            () => bloomEffect.intensity.value,
            x => bloomEffect.intensity.value = x,
            0f,
            bloomAnimationDuration / 2f
        ).SetEase(Ease.InQuad));

        bloomTween = sequence;
    }

    /// <summary>
    /// 指定されたグリッド座標にあるRockPatternAssignerを取得します
    /// </summary>
    /// <param name="gridPos">グリッド座標（Vector2Int）</param>
    /// <returns>見つかったRockPatternAssigner、見つからない場合はnull</returns>
    private RockPatternAssigner GetRockPatternAssignerAtPosition(Vector2Int gridPos)
    {
        if (rockParent == null)
        {
            return null;
        }

        // GridGeneratorと同じ方法でlocalPositionを計算
        StageDatabase.StageData stageData = currentGameStatus?.GetCurrentStageData();
        if (stageData == null || stageData.massStatus == null || stageData.massStatus.Count == 0)
        {
            return null;
        }

        int width = stageData.massStatus[0]?.columns?.Count ?? 0;
        int height = stageData.massStatus.Count;

        if (width == 0 || height == 0)
        {
            return null;
        }

        // GridGeneratorと同じオフセット計算
        float offsetX = -(width - 1) * 0.5f;
        float offsetY = -(height - 1) * 0.5f;
        Vector3 targetLocalPos = new Vector3(offsetX + gridPos.x, offsetY + gridPos.y, 0f);

        // rockParentの子オブジェクトを走査
        for (int i = 0; i < rockParent.childCount; i++)
        {
            Transform child = rockParent.GetChild(i);
            if (child == null) continue;

            // localPositionが一致するかチェック（誤差を許容）
            Vector3 childLocalPos = child.localPosition;
            if (Mathf.Abs(childLocalPos.x - targetLocalPos.x) < 0.01f &&
                Mathf.Abs(childLocalPos.y - targetLocalPos.y) < 0.01f &&
                Mathf.Abs(childLocalPos.z - targetLocalPos.z) < 0.01f)
            {
                // RockPatternAssignerコンポーネントを取得
                RockPatternAssigner assigner = child.GetComponent<RockPatternAssigner>();
                if (assigner != null)
                {
                    return assigner;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 指定座標のRockオブジェクトを取得します
    /// </summary>
    private GameObject GetRockAtPosition(Vector2Int gridPos)
    {
        if (rockParent == null)
        {
            return null;
        }

        // GridGeneratorと同じ方法でlocalPositionを計算
        StageDatabase.StageData stageData = currentGameStatus?.GetCurrentStageData();
        if (stageData == null || stageData.massStatus == null || stageData.massStatus.Count == 0)
        {
            return null;
        }

        int width = stageData.massStatus[0]?.columns?.Count ?? 0;
        int height = stageData.massStatus.Count;

        if (width == 0 || height == 0)
        {
            return null;
        }

        // GridGeneratorと同じオフセット計算
        float offsetX = -(width - 1) * 0.5f;
        float offsetY = -(height - 1) * 0.5f;
        Vector3 targetLocalPos = new Vector3(offsetX + gridPos.x, offsetY + gridPos.y, 0f);

        // rockParentの子オブジェクトを走査
        for (int i = 0; i < rockParent.childCount; i++)
        {
            Transform child = rockParent.GetChild(i);
            if (child == null) continue;

            // localPositionが一致するかチェック（誤差を許容）
            Vector3 childLocalPos = child.localPosition;
            if (Mathf.Abs(childLocalPos.x - targetLocalPos.x) < 0.01f &&
                Mathf.Abs(childLocalPos.y - targetLocalPos.y) < 0.01f)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    /// <summary>
    /// 条件を満たしている座標のEmissionColorの状態を更新します
    /// </summary>
    private void UpdateRockEmissionStates(HashSet<Vector2Int> satisfiedPositions)
    {
        // 新しく条件を満たした座標を検出
        foreach (var pos in satisfiedPositions)
        {
            GameObject rock = GetRockAtPosition(pos);
            if (rock == null) continue;

            RockPatternAssigner patternAssigner = rock.GetComponent<RockPatternAssigner>();
            if (patternAssigner == null) continue;

            if (!currentlySatisfiedPositions.Contains(pos))
            {
                // 新しく条件を満たした座標：アニメーションを開始
                patternAssigner.SetEmissionEnabled(true, true);
            }
            else
            {
                // 条件を満たし続けている座標：最大値を維持（既に最大値になっている場合は何もしない）
                if (!patternAssigner.IsEmissionAtMax())
                {
                    patternAssigner.SetEmissionEnabled(true, false);
                }
            }
        }

        // 条件を満たさなくなった座標を検出
        foreach (var pos in currentlySatisfiedPositions)
        {
            if (!satisfiedPositions.Contains(pos))
            {
                // 条件を満たさなくなった座標：0に戻す
                GameObject rock = GetRockAtPosition(pos);
                if (rock != null)
                {
                    RockPatternAssigner patternAssigner = rock.GetComponent<RockPatternAssigner>();
                    if (patternAssigner != null)
                    {
                        patternAssigner.ResetEmission();
                    }
                }
            }
        }

        // 現在の状態を更新
        currentlySatisfiedPositions = satisfiedPositions;
    }


    private void OnDestroy()
    {
        // Tweenをクリーンアップ
        if (bloomTween != null && bloomTween.IsActive())
        {
            bloomTween.Kill();
        }

    }
}

