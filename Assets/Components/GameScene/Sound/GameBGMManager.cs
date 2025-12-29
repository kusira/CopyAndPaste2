using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CriWare;

/// <summary>
/// 現在のワールドに応じてBGMを再生するマネージャー
/// </summary>
public class GameBGMManager : MonoBehaviour
{
    [System.Serializable]
    public class WorldBGMData
    {
        [Tooltip("ワールドラベル（StageDatabaseのworldLabelと一致させる）")]
        public string worldLabel;

        [Tooltip("このワールドで再生するCuePlayオブジェクト（CriAtomSourceがアサインされている必要があります）")]
        public CuePlay cuePlay;

        [Tooltip("このBGMを流す最大ステージインデックス（CurrentIndexがこの値以下の場合にこのBGMが流れます）")]
        public int maxStageIndex = 0;
    }

    [Header("ワールド別BGM設定")]
    [Tooltip("ワールドラベルとCuePlayのマッピング")]
    [SerializeField] private List<WorldBGMData> worldBGMList = new List<WorldBGMData>();

    [Header("参照設定")]
    [Tooltip("CurrentGameStatus（未設定の場合は自動検索します）")]
    [SerializeField] private CurrentGameStatus currentGameStatus;

    private void Start()
    {
        // CurrentGameStatusが設定されていない場合は自動検索
        if (currentGameStatus == null)
        {
            currentGameStatus = FindFirstObjectByType<CurrentGameStatus>();
        }

        // CRIWAREの初期化を待ってからBGMを再生
        StartCoroutine(PlayBGMAfterInitialization());
    }

    /// <summary>
    /// CRIWAREの初期化が完了してからBGMを再生するコルーチン
    /// </summary>
    private IEnumerator PlayBGMAfterInitialization()
    {
        // まず、すべてのキューシートのロードが完了するまで待つ
        float timeout = 10f; // タイムアウト時間（10秒）
        float elapsed = 0f;

        while (CriAtom.CueSheetsAreLoading && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (elapsed >= timeout)
        {
            Debug.LogWarning("GameBGMManager: キューシートのロードがタイムアウトしました。");
        }

        // 次に、対象のBGMのキューシートがロードされるまで待つ
        CuePlay targetCuePlay = GetTargetCuePlay();
        if (targetCuePlay != null)
        {
            yield return StartCoroutine(WaitForCueSheetLoaded(targetCuePlay));
        }

        // BGMを再生
        PlayBGMForCurrentWorld();
    }

    /// <summary>
    /// 現在のワールドに対応するCuePlayを取得します
    /// </summary>
    private CuePlay GetTargetCuePlay()
    {
        if (currentGameStatus == null)
        {
            return null;
        }

        int currentStageIndex = currentGameStatus.GetCurrentStageIndex();
        List<WorldBGMData> sortedList = new List<WorldBGMData>(worldBGMList);
        sortedList.Sort((a, b) => a.maxStageIndex.CompareTo(b.maxStageIndex));

        int minStageIndex = 0;
        foreach (var worldBGM in sortedList)
        {
            if (worldBGM.cuePlay == null) continue;

            if (currentStageIndex >= minStageIndex && currentStageIndex <= worldBGM.maxStageIndex)
            {
                return worldBGM.cuePlay;
            }

            minStageIndex = worldBGM.maxStageIndex + 1;
        }

        return null;
    }

    /// <summary>
    /// 指定されたCuePlayのキューシートがロードされるまで待つコルーチン
    /// </summary>
    private IEnumerator WaitForCueSheetLoaded(CuePlay cuePlay)
    {
        if (cuePlay == null) yield break;

        // CriAtomSourceを取得
        CriAtomSource atomSource = cuePlay.GetComponent<CriAtomSource>();
        if (atomSource == null)
        {
            Debug.LogWarning("GameBGMManager: CuePlayにCriAtomSourceが見つかりません。");
            yield break;
        }

        string cueSheetName = atomSource.cueSheet;
        if (string.IsNullOrEmpty(cueSheetName))
        {
            Debug.LogWarning("GameBGMManager: CriAtomSourceにキューシート名が設定されていません。");
            yield break;
        }

        // キューシートがロードされるまで待つ
        float timeout = 10f; // タイムアウト時間（10秒）
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            CriAtomExAcb acb = CriAtom.GetAcb(cueSheetName);
            if (acb != null)
            {
                Debug.Log($"GameBGMManager: キューシート '{cueSheetName}' のロードが完了しました。");
                yield break;
            }

            yield return null;
            elapsed += Time.deltaTime;
        }

        if (elapsed >= timeout)
        {
            Debug.LogWarning($"GameBGMManager: キューシート '{cueSheetName}' のロードがタイムアウトしました。");
        }
    }

    /// <summary>
    /// 現在のワールドに応じたBGMを再生します
    /// </summary>
    public void PlayBGMForCurrentWorld()
    {
        if (currentGameStatus == null)
        {
            Debug.LogWarning("GameBGMManager: CurrentGameStatusが見つかりません。");
            return;
        }

        // 現在のステージインデックスを取得
        int currentStageIndex = currentGameStatus.GetCurrentStageIndex();

        // maxStageIndexでソートされたリストを作成（小さい順）
        List<WorldBGMData> sortedList = new List<WorldBGMData>(worldBGMList);
        sortedList.Sort((a, b) => a.maxStageIndex.CompareTo(b.maxStageIndex));

        // 現在のステージインデックスが範囲内にあるBGMを検索
        CuePlay targetCuePlay = null;
        int selectedMaxIndex = -1;
        int minStageIndex = 0; // 最初のBGMの最小値は0

        foreach (var worldBGM in sortedList)
        {
            if (worldBGM.cuePlay == null) continue;

            // 現在のステージインデックスがこのBGMの範囲内にあるかチェック
            // 範囲: minStageIndex から worldBGM.maxStageIndex まで
            if (currentStageIndex >= minStageIndex && currentStageIndex <= worldBGM.maxStageIndex)
            {
                targetCuePlay = worldBGM.cuePlay;
                selectedMaxIndex = worldBGM.maxStageIndex;
                break; // 最初に見つかったBGMを使用
            }

            // 次のBGMの最小値は、現在のBGMのmaxStageIndex + 1
            minStageIndex = worldBGM.maxStageIndex + 1;
        }

        if (targetCuePlay != null)
        {
            targetCuePlay.PlaySound();
            Debug.Log($"GameBGMManager: ステージインデックス {currentStageIndex} に対応するBGMを再生開始しました（maxStageIndex: {selectedMaxIndex}）");
        }
        else
        {
            Debug.LogWarning($"GameBGMManager: ステージインデックス {currentStageIndex} に対応するCuePlayが見つかりませんでした。");
        }
    }

    /// <summary>
    /// ワールド別BGMリストを取得します（エディタ拡張用）
    /// </summary>
    public List<WorldBGMData> GetWorldBGMList()
    {
        return worldBGMList;
    }
}

