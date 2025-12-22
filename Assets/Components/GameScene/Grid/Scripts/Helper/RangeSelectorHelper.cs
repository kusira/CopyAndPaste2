using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RangeSelector用のヘルパー関数群
/// コピー／回転／貼り付けのロジックをこのクラスにまとめます。
/// </summary>
public static class RangeSelectorHelper
{
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
        List<Vector2Int> copiedOffsets)
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
                if (!string.IsNullOrEmpty(v) && v == "#")
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    copiedOffsets.Add(new Vector2Int(dx, dy));
                }
            }
        }
    }

    /// <summary>
    /// コピー済みオフセット群を回転させ、回転後のオフセットリストを構築します。
    /// </summary>
    public static void RotateOffsets(
        List<Vector2Int> sourceOffsets,
        int rotationIndex,
        List<Vector2Int> rotatedOffsets)
    {
        rotatedOffsets.Clear();
        if (sourceOffsets == null) return;

        int rot = ((rotationIndex % 4) + 4) % 4;

        foreach (var o in sourceOffsets)
        {
            Vector2Int r;
            switch (rot)
            {
                case 1: // 90°
                    r = new Vector2Int(
                        Mathf.RoundToInt(o.y),
                        Mathf.RoundToInt(-o.x));
                    break;
                case 2: // 180°
                    r = new Vector2Int(
                        Mathf.RoundToInt(-o.x),
                        Mathf.RoundToInt(-o.y));
                    break;
                case 3: // 270°
                    r = new Vector2Int(
                        Mathf.RoundToInt(-o.y),
                        Mathf.RoundToInt(o.x));
                    break;
                default: // 0°
                    r = new Vector2Int(
                        Mathf.RoundToInt(o.x),
                        Mathf.RoundToInt(o.y));
                    break;
            }
            rotatedOffsets.Add(r);
        }
    }

    /// <summary>
    /// 貼り付けが可能かどうかを判定します。
    /// </summary>
    public static bool CanPaste(
        List<Vector2Int> rotatedOffsets,
        int centerX, int centerY,
        int gridWidth, int gridHeight,
        List<StageDatabase.RowData> massStatus,
        List<StageDatabase.RowData> rockStatus)
    {
        if (rotatedOffsets == null || massStatus == null || rockStatus == null)
        {
            return false;
        }

        foreach (var o in rotatedOffsets)
        {
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
                if (!string.IsNullOrEmpty(rv) && rv == "#")
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
        List<Vector2Int> rotatedOffsets,
        int centerX, int centerY,
        List<StageDatabase.RowData> rockStatus)
    {
        if (rotatedOffsets == null || rockStatus == null) return;

        foreach (var o in rotatedOffsets)
        {
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

            rockStatus[gy].columns[gx] = "#";
        }
    }
}


