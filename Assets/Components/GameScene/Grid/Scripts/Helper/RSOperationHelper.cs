using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RS用のヘルパー関数群
/// コピー／回転／貼り付けのロジックをこのクラスにまとめます。
/// </summary>
public static class RSHelper
{
    /// <summary>
    /// コピーしたRockのデータを保持する構造体
    /// </summary>
    [System.Serializable]
    public struct CopiedRockData
    {
        public Vector2Int offset;
        public string value; // "#", "#S" など

        public CopiedRockData(Vector2Int offset, string value)
        {
            this.offset = offset;
            this.value = value;
        }
    }

    /// <summary>
    /// 左上の座標が0.5刻みでずれている場合、中心座標を補正して左上を整数に揃えます。
    /// centerX/centerY はワールド座標の中心、width/height はセル数。
    /// </summary>
    public static void AdjustCenterForTopLeftAlignment(ref float centerX, ref float centerY, int width, int height)
    {
        // 左上座標（ワールド）を算出
        float halfW = (width - 1) * 0.5f;
        float halfH = (height - 1) * 0.5f;
        float leftX = centerX - halfW;
        float topY = centerY + halfH;

        // 小数部がちょうど0.5なら補正（左に0.5 / 上に0.5）
        if (IsHalfFraction(leftX))
        {
            centerX -= 0.5f;
        }
        if (IsHalfFraction(topY))
        {
            centerY += 0.5f;
        }
    }

    private static bool IsHalfFraction(float v)
    {
        float frac = v - Mathf.Floor(v);
        return Mathf.Abs(frac - 0.5f) < 0.0001f;
    }

    /// <summary>
    /// 文字列から基底タイプ（'.' または '#') とギミックキーの一覧を抽出します。
    /// 例: ".S" -> baseChar='.', keys={'S'}
    /// </summary>
    public static void ParseCell(string cell, out char baseChar, List<string> gimmickKeys)
    {
        baseChar = '\0';
        gimmickKeys.Clear();
        if (string.IsNullOrEmpty(cell)) return;

        baseChar = cell[0];
        for (int i = 1; i < cell.Length; i++)
        {
            var c = cell[i];
            // 1文字キーとして登録
            gimmickKeys.Add(c.ToString());
        }
    }

    /// <summary>
    /// 指定範囲内のRockを中心セルからのオフセットとしてコピーします。
    /// </summary>
    public static void CopyRockPattern(
        StageDatabase.StageData stageData,
        int minX, int minY, int maxX, int maxY,
        int centerX, int centerY,
        List<CopiedRockData> copiedOffsets)
    {
        copiedOffsets.Clear();

        if (stageData == null || stageData.rockStatus == null)
        {
            return;
        }

        for (int y = minY; y <= maxY; y++)
        {
            if (y < 0 || y >= stageData.rockStatus.Count) continue;
            var row = stageData.rockStatus[y];
            if (row == null || row.columns == null) continue;

            for (int x = minX; x <= maxX; x++)
            {
                if (x < 0 || x >= row.columns.Count) continue;

                string v = row.columns[x];
                char baseChar;
                List<string> dummyKeys = new List<string>();
                ParseCell(v, out baseChar, dummyKeys);
                
                if (baseChar == '#')
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    copiedOffsets.Add(new CopiedRockData(new Vector2Int(dx, dy), v));
                }
            }
        }

        // コピー時のSEを再生
        if (copiedOffsets.Count > 0)
        {
            PlayCopySound();
        }
    }

    /// <summary>
    /// Copy SEを再生します
    /// </summary>
    private static void PlayCopySound()
    {
        GameObject copyObj = GameObject.Find("Copy(CriAtomSource)");
        if (copyObj != null)
        {
            CuePlay cuePlay = copyObj.GetComponent<CuePlay>();
            if (cuePlay != null)
            {
                cuePlay.PlaySound();
            }
        }
    }

    /// <summary>
    /// コピー済みオフセット群を回転させ、回転後のオフセットリストを構築します。
    /// 偶数サイズの回転時におけるピボットずれを補正します。
    /// </summary>
    public static void RotateOffsets(
        List<CopiedRockData> sourceOffsets,
        int rotationIndex,
        int sourceWidth,
        int sourceHeight,
        List<CopiedRockData> rotatedOffsets)
    {
        rotatedOffsets.Clear();
        if (sourceOffsets == null) return;

        int rot = ((rotationIndex % 4) + 4) % 4;

        // ピボット位置の補正値（偶数サイズの場合、ピボットはセルの境界線上にあるため、-0.5ずれているとみなす）
        // 座標系は「Pivot」が原点。
        // SourcePivot = Center + (W_is_even ? 0.5 : 0) ?
        // いや、整数座標系では W=2 のとき Indices 0,1 の Center 0.5。Pivot(centerX) は 1。
        // なので Pivot は Center より +0.5 ずれている。
        // したがって、Center = Pivot - 0.5。
        // Offsets は (Pos - Pivot)。
        // RelToCenter = Pos - Center = Pos - (Pivot - 0.5) = (Pos - Pivot) + 0.5 = Offset + 0.5。
        // なので、Offset に +0.5 ではなく - comp を加える？
        
        // 正しい補正：
        // Even(偶数): PivotはCenterより0.5大きい -> Offsetは本来より0.5小さい -> 本来の位置にするには +0.5
        // srcComp = (isEven) ? +0.5 : 0
        float srcCompX = (sourceWidth % 2 == 0) ? 0.5f : 0f;
        float srcCompY = (sourceHeight % 2 == 0) ? 0.5f : 0f;

        // Target Dimensions
        int targetWidth = (rot % 2 == 0) ? sourceWidth : sourceHeight;
        int targetHeight = (rot % 2 == 0) ? sourceHeight : sourceWidth;
        
        // Target Comp
        // ターゲット座標系でも同様に、もしEvenなら Pivot が +0.5 ずれる。
        // NewOffset = NewPos - NewPivot
        // NewRelToCenter = NewPos - NewCenter
        // NewOffset = (NewRelToCenter + NewCenter) - NewPivot
        //           = NewRelToCenter + (NewCenter - NewPivot)
        //           = NewRelToCenter - 0.5 (if Even)
        // dstComp = (isEven) ? -0.5 : 0
        float dstCompX = (targetWidth % 2 == 0) ? -0.5f : 0f;
        float dstCompY = (targetHeight % 2 == 0) ? -0.5f : 0f;

        foreach (var data in sourceOffsets)
        {
            Vector2Int o = data.offset;
            // Float中心相対座標に変換
            float fx = o.x + srcCompX;
            float fy = o.y + srcCompY;

            float rx, ry;
            switch (rot)
            {
                case 1: // 90° (x,y) -> (y, -x)
                    rx = fy;
                    ry = -fx;
                    break;
                case 2: // 180° (x,y) -> (-x, -y)
                    rx = -fx;
                    ry = -fy;
                    break;
                case 3: // 270° (x,y) -> (-y, x)
                    rx = -fy;
                    ry = fx;
                    break;
                default: // 0°
                    rx = fx;
                    ry = fy;
                    break;
            }

            // 整数オフセットに戻す
            // NewOffset = rx + dstComp
            int nx = Mathf.RoundToInt(rx + dstCompX);
            int ny = Mathf.RoundToInt(ry + dstCompY);

            rotatedOffsets.Add(new CopiedRockData(new Vector2Int(nx, ny), data.value));
        }
    }

    /// <summary>
    /// 貼り付けが可能かどうかを判定します。
    /// </summary>
    public static bool CanPaste(
        List<CopiedRockData> rotatedOffsets,
        int centerX, int centerY,
        int gridWidth, int gridHeight,
        List<StageDatabase.RowData> massStatus,
        List<StageDatabase.RowData> rockStatus)
    {
        if (rotatedOffsets == null || massStatus == null || rockStatus == null)
        {
            return false;
        }

        foreach (var data in rotatedOffsets)
        {
            Vector2Int o = data.offset;
            int gx = centerX + o.x;
            int gy = centerY + o.y;

            if (gx < 0 || gy < 0 || gx >= gridWidth || gy >= gridHeight)
            {
                return false;
            }

            if (gy >= massStatus.Count || massStatus[gy] == null || massStatus[gy].columns == null ||
                gx >= massStatus[gy].columns.Count)
            {
                return false;
            }

            string cellValue = massStatus[gy].columns[gx];
            char baseChar;
            List<string> keys = new List<string>(); // ダミー
            ParseCell(cellValue, out baseChar, keys);
            
            if (baseChar != '.')
            {
                return false;
            }

            if (gy < rockStatus.Count && rockStatus[gy] != null && rockStatus[gy].columns != null &&
                gx < rockStatus[gy].columns.Count)
            {
                string rv = rockStatus[gy].columns[gx];
                char rockBaseChar;
                List<string> dummyKeys = new List<string>();
                ParseCell(rv, out rockBaseChar, dummyKeys);

                if (rockBaseChar == '#')
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 実際にRockStatusへ貼り付けを行います。
    /// </summary>
    public static void ApplyPaste(
        List<CopiedRockData> rotatedOffsets,
        int centerX, int centerY,
        List<StageDatabase.RowData> rockStatus)
    {
        if (rotatedOffsets == null || rockStatus == null) return;

        foreach (var data in rotatedOffsets)
        {
            Vector2Int o = data.offset;
            int gx = centerX + o.x;
            int gy = centerY + o.y;

            if (gy < 0) continue;

            while (gy >= rockStatus.Count)
            {
                rockStatus.Add(new StageDatabase.RowData());
            }
            if (rockStatus[gy] == null)
            {
                rockStatus[gy] = new StageDatabase.RowData();
            }
            while (gx >= rockStatus[gy].columns.Count)
            {
                rockStatus[gy].columns.Add(string.Empty);
            }

            rockStatus[gy].columns[gx] = data.value;
        }

        // 貼り付け時のSEを再生
        if (rotatedOffsets != null && rotatedOffsets.Count > 0)
        {
            PlayPasteSound();
        }
    }

    /// <summary>
    /// Paste SEを再生します
    /// </summary>
    private static void PlayPasteSound()
    {
        GameObject pasteObj = GameObject.Find("Paste(CriAtomSource)");
        if (pasteObj != null)
        {
            CuePlay cuePlay = pasteObj.GetComponent<CuePlay>();
            if (cuePlay != null)
            {
                cuePlay.PlaySound();
            }
        }
    }

    /// <summary>
    /// 指定範囲内のRockを削除します（ステージデータとシーン上のオブジェクトの両方）
    /// </summary>
    /// <param name="stageData">ステージデータ</param>
    /// <param name="minX">範囲の最小X座標（グリッドインデックス）</param>
    /// <param name="minY">範囲の最小Y座標（グリッドインデックス）</param>
    /// <param name="maxX">範囲の最大X座標（グリッドインデックス）</param>
    /// <param name="maxY">範囲の最大Y座標（グリッドインデックス）</param>
    /// <param name="gridParentPosition">グリッド親のワールド座標</param>
    /// <param name="gridOffset">グリッドオフセット</param>
    /// <returns>削除されたRockの数</returns>
    public static int DestroyRocksInRange(
        StageDatabase.StageData stageData,
        int minX, int minY, int maxX, int maxY,
        Vector3 gridParentPosition, Vector3 gridOffset)
    {
        int destroyedCount = 0;

        if (stageData == null || stageData.rockStatus == null)
        {
            return destroyedCount;
        }

        List<StageDatabase.RowData> rockStatus = stageData.rockStatus;

        // 1. ステージデータからRockStatusを削除
        for (int y = minY; y <= maxY; y++)
        {
            if (y >= rockStatus.Count) continue;
            if (rockStatus[y] == null || rockStatus[y].columns == null) continue;

            for (int x = minX; x <= maxX; x++)
            {
                if (x >= rockStatus[y].columns.Count) continue;

                // Rockがある場合は削除（空セル"0"に設定）
                string cellValue = rockStatus[y].columns[x];
                char baseChar;
                List<string> keys = new List<string>();
                ParseCell(cellValue, out baseChar, keys);

                if (baseChar == '#')
                {
                    rockStatus[y].columns[x] = "0";
                    destroyedCount++;
                }
            }
        }

        // 2. シーン上のRockオブジェクトを破壊（TagがRockのもの）
        GameObject[] rocks = GameObject.FindGameObjectsWithTag("Rock");

        foreach (GameObject rock in rocks)
        {
            if (rock == null) continue;

            // Rockの位置をグリッドインデックスに変換
            Vector2Int rockGridIndex = RSGridHelper.WorldToGridIndex(rock.transform.position, gridParentPosition, gridOffset);

            // 範囲内にあるかチェック
            if (rockGridIndex.x >= minX && rockGridIndex.x <= maxX &&
                rockGridIndex.y >= minY && rockGridIndex.y <= maxY)
            {
                // RockDestroyAnimatorがあればアニメーション付きで破壊、なければ即座に破壊
                RockDestroyAnimator animator = rock.GetComponent<RockDestroyAnimator>();
                if (animator != null)
                {
                    animator.StartDestroyAnimation();
                }
                else
                {
                    Object.Destroy(rock);
                }
            }
        }

        // 破壊時のSEを再生
        if (destroyedCount > 0)
        {
            PlayPuzzleBreakSound();
        }

        return destroyedCount;
    }

    /// <summary>
    /// Puzzle_Break SEを再生します
    /// </summary>
    private static void PlayPuzzleBreakSound()
    {
        GameObject puzzleBreakObj = GameObject.Find("Puzzle_Break(CriAtomSource)");
        if (puzzleBreakObj != null)
        {
            CuePlay cuePlay = puzzleBreakObj.GetComponent<CuePlay>();
            if (cuePlay != null)
            {
                cuePlay.PlaySound();
            }
        }
    }

    /// <summary>
    /// Rockの移動情報を保持する構造体
    /// </summary>
    public struct RockMoveInfo
    {
        public Vector2Int fromPosition; // 移動元のグリッド座標
        public Vector2Int toPosition;   // 移動先のグリッド座標

        public RockMoveInfo(Vector2Int from, Vector2Int to)
        {
            fromPosition = from;
            toPosition = to;
        }
    }

    /// <summary>
    /// 指定範囲内のRockの移動情報を計算します（データは変更しない）
    /// </summary>
    /// <param name="stageData">ステージデータ</param>
    /// <param name="minX">範囲の最小X座標（グリッドインデックス）</param>
    /// <param name="minY">範囲の最小Y座標（グリッドインデックス）</param>
    /// <param name="maxX">範囲の最大X座標（グリッドインデックス）</param>
    /// <param name="maxY">範囲の最大Y座標（グリッドインデックス）</param>
    /// <param name="gridParentPosition">グリッド親のワールド座標</param>
    /// <param name="gridOffset">グリッドオフセット</param>
    /// <param name="rotationIndex">回転インデックス（0=上, 1=右, 2=下, 3=左）</param>
    /// <param name="moveInfos">移動情報のリスト（出力）</param>
    public static void CalculateGravityMoves(
        StageDatabase.StageData stageData,
        int minX, int minY, int maxX, int maxY,
        Vector3 gridParentPosition, Vector3 gridOffset,
        int rotationIndex,
        List<RockMoveInfo> moveInfos)
    {
        if (moveInfos == null)
        {
            moveInfos = new List<RockMoveInfo>();
        }
        moveInfos.Clear();

        if (stageData == null || stageData.rockStatus == null || stageData.massStatus == null)
        {
            return;
        }

        List<StageDatabase.RowData> rockStatus = stageData.rockStatus;
        List<StageDatabase.RowData> massStatus = stageData.massStatus;

        // 範囲内のすべてのRockを収集（位置情報も含む）
        List<(int x, int y, string value)> allRocks = new List<(int x, int y, string value)>();
        for (int y = minY; y <= maxY; y++)
        {
            if (y >= rockStatus.Count || rockStatus[y] == null || rockStatus[y].columns == null)
            {
                continue;
            }

            for (int x = minX; x <= maxX; x++)
            {
                if (x >= rockStatus[y].columns.Count)
                {
                    continue;
                }

                string cellValue = rockStatus[y].columns[x] ?? "";
                char baseChar;
                List<string> keys = new List<string>();
                ParseCell(cellValue, out baseChar, keys);

                if (baseChar == '#')
                {
                    allRocks.Add((x, y, cellValue)); // Rockの位置と値を保存
                }
            }
        }

        if (allRocks.Count == 0)
        {
            return; // Rockがない場合は終了
        }

        // 各Rockの現在位置を追跡（元の位置をキーとして、現在位置を値として保持）
        Dictionary<Vector2Int, Vector2Int> rockPositions = new Dictionary<Vector2Int, Vector2Int>();
        foreach (var rock in allRocks)
        {
            Vector2Int originalPos = new Vector2Int(rock.x, rock.y);
            rockPositions[originalPos] = originalPos; // 初期位置は元の位置
        }

        // 回転方向に応じて重力の方向を決定
        // 0=上, 1=右, 2=下, 3=左
        int rot = ((rotationIndex % 4) + 4) % 4;
        
        // 複数回のパスで、各Rockが1セルずつ重力方向に移動できるかチェック
        // 移動できるRockがなくなるまで繰り返す
        bool moved = true;
        int maxIterations = (maxY - minY + 1) * (maxX - minX + 1); // 無限ループ防止
        int iteration = 0;

        while (moved && iteration < maxIterations)
        {
            moved = false;
            iteration++;

            // 重力方向の反対側から走査（重力方向の先頭にあるRockから先に処理することで、自然な落下を実現）
            List<Vector2Int> sortedRocks = new List<Vector2Int>(rockPositions.Keys);
            sortedRocks.Sort((a, b) => 
            {
                Vector2Int posA = rockPositions[a];
                Vector2Int posB = rockPositions[b];
                
                // 回転に応じてソート順を変更
                switch (rot)
                {
                    case 0: // 上方向: 上から下に（Y座標が小さい順）
                        if (posA.y != posB.y) return posA.y.CompareTo(posB.y);
                        return posA.x.CompareTo(posB.x);
                    case 1: // 右方向: 右から左に（X座標が大きい順）
                        if (posA.x != posB.x) return posB.x.CompareTo(posA.x);
                        return posA.y.CompareTo(posB.y);
                    case 2: // 下方向: 下から上に（Y座標が大きい順）
                        if (posA.y != posB.y) return posB.y.CompareTo(posA.y);
                        return posA.x.CompareTo(posB.x);
                    case 3: // 左方向: 左から右に（X座標が小さい順）
                        if (posA.x != posB.x) return posA.x.CompareTo(posB.x);
                        return posA.y.CompareTo(posB.y);
                    default:
                        return 0;
                }
            });

            foreach (Vector2Int originalPos in sortedRocks)
            {
                Vector2Int currentPos = rockPositions[originalPos];
                Vector2Int nextPos;
                bool canCheckMove = false;

                // 回転に応じて移動方向を決定
                switch (rot)
                {
                    case 0: // 上方向（Y座標が減る）
                        nextPos = new Vector2Int(currentPos.x, currentPos.y - 1);
                        canCheckMove = (nextPos.y >= minY);
                        break;
                    case 1: // 右方向（X座標が増える）
                        nextPos = new Vector2Int(currentPos.x + 1, currentPos.y);
                        canCheckMove = (nextPos.x <= maxX);
                        break;
                    case 2: // 下方向（Y座標が増える）
                        nextPos = new Vector2Int(currentPos.x, currentPos.y + 1);
                        canCheckMove = (nextPos.y <= maxY);
                        break;
                    case 3: // 左方向（X座標が減る）
                        nextPos = new Vector2Int(currentPos.x - 1, currentPos.y);
                        canCheckMove = (nextPos.x >= minX);
                        break;
                    default:
                        continue;
                }

                // 範囲外の場合はスキップ
                if (!canCheckMove)
                {
                    continue;
                }

                // MassStatusをチェック（Massがない場所があったら止まる）
                int checkY = nextPos.y;
                int checkX = nextPos.x;
                
                if (checkY < 0 || checkY >= massStatus.Count || massStatus[checkY] == null || massStatus[checkY].columns == null)
                {
                    continue;
                }
                if (checkX < 0 || checkX >= massStatus[checkY].columns.Count)
                {
                    continue;
                }

                string massValue = massStatus[checkY].columns[checkX] ?? "";
                char massBaseChar;
                List<string> massKeys = new List<string>();
                ParseCell(massValue, out massBaseChar, massKeys);

                // Mass（'.'）がない場合は止まる
                if (massBaseChar != '.')
                {
                    continue;
                }

                // 移動先の位置に既にRockがないかチェック（他のRockが既に移動している可能性がある）
                bool canMove = true;
                foreach (var pos in rockPositions.Values)
                {
                    if (pos.Equals(nextPos))
                    {
                        canMove = false;
                        break;
                    }
                }

                if (canMove)
                {
                    // 移動可能：位置を更新
                    rockPositions[originalPos] = nextPos;
                    moved = true;
                }
            }
        }

        // 移動情報を記録（位置が変わった場合のみ）
        foreach (var rock in allRocks)
        {
            Vector2Int originalPos = new Vector2Int(rock.x, rock.y);
            Vector2Int finalPos = rockPositions[originalPos];

            if (!originalPos.Equals(finalPos))
            {
                moveInfos.Add(new RockMoveInfo(originalPos, finalPos));
            }
        }
    }

    /// <summary>
    /// 移動情報に基づいてRockを移動します（データを変更）
    /// </summary>
    /// <param name="stageData">ステージデータ</param>
    /// <param name="moveInfos">移動情報のリスト</param>
    public static void ApplyGravityMoves(
        StageDatabase.StageData stageData,
        List<RockMoveInfo> moveInfos)
    {
        if (stageData == null || stageData.rockStatus == null || moveInfos == null || moveInfos.Count == 0)
        {
            return;
        }

        List<StageDatabase.RowData> rockStatus = stageData.rockStatus;

        // 移動元のRockの値を保存
        Dictionary<Vector2Int, string> rockValues = new Dictionary<Vector2Int, string>();
        foreach (var moveInfo in moveInfos)
        {
            int fromY = moveInfo.fromPosition.y;
            int fromX = moveInfo.fromPosition.x;

            if (fromY >= 0 && fromY < rockStatus.Count && 
                rockStatus[fromY] != null && 
                rockStatus[fromY].columns != null &&
                fromX >= 0 && fromX < rockStatus[fromY].columns.Count)
            {
                string value = rockStatus[fromY].columns[fromX] ?? "";
                if (!string.IsNullOrEmpty(value))
                {
                    char baseChar;
                    List<string> keys = new List<string>();
                    ParseCell(value, out baseChar, keys);
                    if (baseChar == '#')
                    {
                        rockValues[moveInfo.fromPosition] = value;
                        // 移動元をクリア
                        rockStatus[fromY].columns[fromX] = "0";
                    }
                }
            }
        }

        // 移動先にRockを配置
        foreach (var moveInfo in moveInfos)
        {
            if (rockValues.ContainsKey(moveInfo.fromPosition))
            {
                int toY = moveInfo.toPosition.y;
                int toX = moveInfo.toPosition.x;

                // 行と列が存在することを確認
                if (toY >= rockStatus.Count)
                {
                    while (rockStatus.Count <= toY)
                    {
                        rockStatus.Add(new StageDatabase.RowData());
                    }
                }
                if (rockStatus[toY] == null)
                {
                    rockStatus[toY] = new StageDatabase.RowData();
                }
                if (rockStatus[toY].columns == null)
                {
                    rockStatus[toY].columns = new List<string>();
                }
                while (rockStatus[toY].columns.Count <= toX)
                {
                    rockStatus[toY].columns.Add("0");
                }

                // Rockを配置
                rockStatus[toY].columns[toX] = rockValues[moveInfo.fromPosition];
            }
        }
    }

    /// <summary>
    /// Puzzle_Gravity SEを再生します
    /// </summary>
    public static void PlayPuzzleGravitySound()
    {
        GameObject puzzleGravityObj = GameObject.Find("Puzzle_Gravity(CriAtomSource)");
        if (puzzleGravityObj != null)
        {
            CuePlay cuePlay = puzzleGravityObj.GetComponent<CuePlay>();
            if (cuePlay != null)
            {
                cuePlay.PlaySound();
            }
        }
    }

    /// <summary>
    /// Turn SEを再生します
    /// </summary>
    public static void PlayTurnSound()
    {
        GameObject turnObj = GameObject.Find("Turn(CriAtomSource)");
        if (turnObj != null)
        {
            CuePlay cuePlay = turnObj.GetComponent<CuePlay>();
            if (cuePlay != null)
            {
                cuePlay.PlaySound();
            }
        }
    }

    /// <summary>
    /// Cancel SEを再生します
    /// </summary>
    public static void PlayCancelSound()
    {
        GameObject cancelObj = GameObject.Find("Cancel(CriAtomSource)");
        if (cancelObj != null)
        {
            CuePlay cuePlay = cancelObj.GetComponent<CuePlay>();
            if (cuePlay != null)
            {
                cuePlay.PlaySound();
            }
        }
    }

    /// <summary>
    /// 指定範囲内のRockを回転方向に応じて詰めます（重力で落ちるように）
    /// すべてのRockを同時に処理し、互いに干渉しながら落ちます
    /// </summary>
    /// <param name="stageData">ステージデータ</param>
    /// <param name="minX">範囲の最小X座標（グリッドインデックス）</param>
    /// <param name="minY">範囲の最小Y座標（グリッドインデックス）</param>
    /// <param name="maxX">範囲の最大X座標（グリッドインデックス）</param>
    /// <param name="maxY">範囲の最大Y座標（グリッドインデックス）</param>
    /// <param name="gridParentPosition">グリッド親のワールド座標</param>
    /// <param name="gridOffset">グリッドオフセット</param>
    /// <param name="rotationIndex">回転インデックス（0=上, 1=右, 2=下, 3=左）</param>
    /// <param name="moveInfos">移動情報のリスト（出力）</param>
    public static void ApplyGravityToRocksInRange(
        StageDatabase.StageData stageData,
        int minX, int minY, int maxX, int maxY,
        Vector3 gridParentPosition, Vector3 gridOffset,
        int rotationIndex,
        List<RockMoveInfo> moveInfos)
    {
        if (moveInfos == null)
        {
            moveInfos = new List<RockMoveInfo>();
        }
        moveInfos.Clear();

        if (stageData == null || stageData.rockStatus == null || stageData.massStatus == null)
        {
            return;
        }

        List<StageDatabase.RowData> rockStatus = stageData.rockStatus;
        List<StageDatabase.RowData> massStatus = stageData.massStatus;

        // 範囲内のすべてのRockを収集（位置情報も含む）
        List<(int x, int y, string value)> allRocks = new List<(int x, int y, string value)>();
        for (int y = minY; y <= maxY; y++)
        {
            if (y >= rockStatus.Count || rockStatus[y] == null || rockStatus[y].columns == null)
            {
                continue;
            }

            for (int x = minX; x <= maxX; x++)
            {
                if (x >= rockStatus[y].columns.Count)
                {
                    continue;
                }

                string cellValue = rockStatus[y].columns[x] ?? "";
                char baseChar;
                List<string> keys = new List<string>();
                ParseCell(cellValue, out baseChar, keys);

                if (baseChar == '#')
                {
                    allRocks.Add((x, y, cellValue)); // Rockの位置と値を保存
                    // 元の位置をクリア
                    rockStatus[y].columns[x] = "0";
                }
            }
        }

        if (allRocks.Count == 0)
        {
            return; // Rockがない場合は終了
        }

        // 各Rockの現在位置を追跡（元の位置をキーとして、現在位置を値として保持）
        Dictionary<Vector2Int, Vector2Int> rockPositions = new Dictionary<Vector2Int, Vector2Int>();
        foreach (var rock in allRocks)
        {
            Vector2Int originalPos = new Vector2Int(rock.x, rock.y);
            rockPositions[originalPos] = originalPos; // 初期位置は元の位置
        }

        // 回転方向に応じて重力の方向を決定
        // 0=上, 1=右, 2=下, 3=左
        int rot = ((rotationIndex % 4) + 4) % 4;
        
        // 複数回のパスで、各Rockが1セルずつ重力方向に移動できるかチェック
        // 移動できるRockがなくなるまで繰り返す
        bool moved = true;
        int maxIterations = (maxY - minY + 1) * (maxX - minX + 1); // 無限ループ防止
        int iteration = 0;

        while (moved && iteration < maxIterations)
        {
            moved = false;
            iteration++;

            // 重力方向の反対側から走査（重力方向の先頭にあるRockから先に処理することで、自然な落下を実現）
            List<Vector2Int> sortedRocks = new List<Vector2Int>(rockPositions.Keys);
            sortedRocks.Sort((a, b) => 
            {
                Vector2Int posA = rockPositions[a];
                Vector2Int posB = rockPositions[b];
                
                // 回転に応じてソート順を変更
                switch (rot)
                {
                    case 0: // 上方向: 上から下に（Y座標が小さい順）
                        if (posA.y != posB.y) return posA.y.CompareTo(posB.y);
                        return posA.x.CompareTo(posB.x);
                    case 1: // 右方向: 右から左に（X座標が大きい順）
                        if (posA.x != posB.x) return posB.x.CompareTo(posA.x);
                        return posA.y.CompareTo(posB.y);
                    case 2: // 下方向: 下から上に（Y座標が大きい順）
                        if (posA.y != posB.y) return posB.y.CompareTo(posA.y);
                        return posA.x.CompareTo(posB.x);
                    case 3: // 左方向: 左から右に（X座標が小さい順）
                        if (posA.x != posB.x) return posA.x.CompareTo(posB.x);
                        return posA.y.CompareTo(posB.y);
                    default:
                        return 0;
                }
            });

            foreach (Vector2Int originalPos in sortedRocks)
            {
                Vector2Int currentPos = rockPositions[originalPos];
                Vector2Int nextPos;
                bool canCheckMove = false;

                // 回転に応じて移動方向を決定
                switch (rot)
                {
                    case 0: // 上方向（Y座標が減る）
                        nextPos = new Vector2Int(currentPos.x, currentPos.y - 1);
                        canCheckMove = (nextPos.y >= minY);
                        break;
                    case 1: // 右方向（X座標が増える）
                        nextPos = new Vector2Int(currentPos.x + 1, currentPos.y);
                        canCheckMove = (nextPos.x <= maxX);
                        break;
                    case 2: // 下方向（Y座標が増える）
                        nextPos = new Vector2Int(currentPos.x, currentPos.y + 1);
                        canCheckMove = (nextPos.y <= maxY);
                        break;
                    case 3: // 左方向（X座標が減る）
                        nextPos = new Vector2Int(currentPos.x - 1, currentPos.y);
                        canCheckMove = (nextPos.x >= minX);
                        break;
                    default:
                        continue;
                }

                // 範囲外の場合はスキップ
                if (!canCheckMove)
                {
                    continue;
                }

                // MassStatusをチェック（Massがない場所があったら止まる）
                int checkY = nextPos.y;
                int checkX = nextPos.x;
                
                if (checkY < 0 || checkY >= massStatus.Count || massStatus[checkY] == null || massStatus[checkY].columns == null)
                {
                    continue;
                }
                if (checkX < 0 || checkX >= massStatus[checkY].columns.Count)
                {
                    continue;
                }

                string massValue = massStatus[checkY].columns[checkX] ?? "";
                char massBaseChar;
                List<string> massKeys = new List<string>();
                ParseCell(massValue, out massBaseChar, massKeys);

                // Mass（'.'）がない場合は止まる
                if (massBaseChar != '.')
                {
                    continue;
                }

                // 移動先の位置に既にRockがないかチェック（他のRockが既に移動している可能性がある）
                bool canMove = true;
                foreach (var pos in rockPositions.Values)
                {
                    if (pos.Equals(nextPos))
                    {
                        canMove = false;
                        break;
                    }
                }

                if (canMove)
                {
                    // 移動可能：位置を更新
                    rockPositions[originalPos] = nextPos;
                    moved = true;
                }
            }
        }

        // 最終的な位置にRockを配置し、移動情報を記録
        foreach (var rock in allRocks)
        {
            Vector2Int originalPos = new Vector2Int(rock.x, rock.y);
            Vector2Int finalPos = rockPositions[originalPos];

            // 行と列が存在することを確認
            if (finalPos.y >= rockStatus.Count)
            {
                while (rockStatus.Count <= finalPos.y)
                {
                    rockStatus.Add(new StageDatabase.RowData());
                }
            }
            if (rockStatus[finalPos.y] == null)
            {
                rockStatus[finalPos.y] = new StageDatabase.RowData();
            }
            if (rockStatus[finalPos.y].columns == null)
            {
                rockStatus[finalPos.y].columns = new List<string>();
            }
            while (rockStatus[finalPos.y].columns.Count <= finalPos.x)
            {
                rockStatus[finalPos.y].columns.Add("0");
            }

            // Rockを配置
            rockStatus[finalPos.y].columns[finalPos.x] = rock.value;

            // 移動情報を記録（位置が変わった場合のみ）
            if (!originalPos.Equals(finalPos))
            {
                moveInfos.Add(new RockMoveInfo(originalPos, finalPos));
            }
        }

        // シーン上のRockオブジェクトの破壊は行わない
        // （アニメーション中に破壊されないようにするため、グリッド再生成時に自動的に処理される）
    }
}


