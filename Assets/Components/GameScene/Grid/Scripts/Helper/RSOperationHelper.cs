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
                Object.Destroy(rock);
            }
        }

        return destroyedCount;
    }
}


