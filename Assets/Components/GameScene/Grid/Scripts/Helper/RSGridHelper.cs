using UnityEngine;

/// <summary>
/// RS用のグリッド座標変換・スナップ処理のヘルパー関数群
/// </summary>
public static class RSGridHelper
{
    /// <summary>
    /// グリッドインデックスをワールド座標に変換します
    /// </summary>
    /// <summary>
    /// グリッドインデックスをワールド座標に変換します
    /// </summary>
    public static Vector3 GridIndexToWorld(int gx, int gy, float z, Vector3 gridParentPosition, Vector3 gridOffset, float scale = 1.0f)
    {
        return gridParentPosition + (new Vector3(gridOffset.x + gx, gridOffset.y + gy, 0f) * scale) + new Vector3(0f, 0f, z);
    }

    /// <summary>
    /// ワールド座標からグリッドの中心座標（グリッドインデックス）を計算します
    /// </summary>
    public static Vector2 WorldToGridCenter(Vector3 worldPosition, Vector3 gridParentPosition, Vector3 gridOffset, float scale = 1.0f)
    {
        // 親からの相対座標をスケールで割ってグリッド単位にする
        Vector3 vec = worldPosition - gridParentPosition;
        Vector3 localPos = vec / scale;
        
        float floatCenterX = localPos.x - gridOffset.x;
        float floatCenterY = localPos.y - gridOffset.y;
        return new Vector2(floatCenterX, floatCenterY);
    }

    /// <summary>
    /// ワールド座標からグリッドの中心セルインデックスを計算します
    /// </summary>
    public static Vector2Int WorldToGridIndex(Vector3 worldPosition, Vector3 gridParentPosition, Vector3 gridOffset, float scale = 1.0f)
    {
        Vector2 center = WorldToGridCenter(worldPosition, gridParentPosition, gridOffset, scale);
        int centerX = Mathf.FloorToInt(center.x + 0.5f);
        int centerY = Mathf.FloorToInt(center.y + 0.5f);
        return new Vector2Int(centerX, centerY);
    }

    /// <summary>
    /// 選択矩形の範囲（グリッドインデックス）を計算します
    /// </summary>
    /// <param name="centerX">中心X座標（グリッド座標系）</param>
    /// <param name="centerY">中心Y座標（グリッド座標系）</param>
    /// <param name="width">幅（セル数）</param>
    /// <param name="height">高さ（セル数）</param>
    /// <returns>矩形範囲（minX, minY, maxX, maxY）</returns>
    public static (int minX, int minY, int maxX, int maxY) CalculateSelectionBounds(float centerX, float centerY, int width, int height)
    {
        float halfW = (width - 1) * 0.5f;
        float halfH = (height - 1) * 0.5f;
        int minX = Mathf.RoundToInt(centerX - halfW);
        int maxX = Mathf.RoundToInt(centerX + halfW);
        int minY = Mathf.RoundToInt(centerY - halfH);
        int maxY = Mathf.RoundToInt(centerY + halfH);
        return (minX, minY, maxX, maxY);
    }

    /// <summary>
    /// 位置をグリッドにスナップします（偶数サイズでは中心を半整数座標に補正）
    /// </summary>
    public static Vector3 SnapToGrid(Vector3 worldPosition, Vector3 gridParentPosition, Vector3 gridOffset, int selectorWidth, int selectorHeight, float scale = 1.0f)
    {
        // グリッドの親位置を基準にローカル座標に変換(スケール考慮)
        Vector3 vec = worldPosition - gridParentPosition;
        Vector3 localPosition = vec / scale;

        // グリッドオフセット
        float ox = gridOffset.x;
        float oy = gridOffset.y;

        // セレクターの中心が、グリッドの整数座標(セルの中心)に来るべきか、半整数座標(セルの境界)に来るべきか
        // セレクター幅が奇数(1,3,5)なら、中心はセルの中心（整数）に合う
        // セレクター幅が偶数(2,4,6)なら、中心はセルの境界（半整数）に合う

        // X座標のスナップ
        float targetX;
        if (selectorWidth % 2 != 0)
        {
            // 奇数サイズ：整数座標にスナップ
            float relativeX = localPosition.x - ox;
            targetX = Mathf.Round(relativeX) + ox;
        }
        else
        {
            // 偶数サイズ：半整数座標にスナップ
            float relativeX = localPosition.x - ox;
            targetX = Mathf.Floor(relativeX) + 0.5f + ox;
        }

        // Y座標のスナップ
        float targetY;
        if (selectorHeight % 2 != 0)
        {
            // 奇数サイズ：整数座標にスナップ
            float relativeY = localPosition.y - oy;
            targetY = Mathf.Round(relativeY) + oy;
        }
        else
        {
            // 偶数サイズ：半整数座標にスナップ
            float relativeY = localPosition.y - oy;
            targetY = Mathf.Floor(relativeY) + 0.5f + oy;
        }

        // ワールド座標に戻す
        return gridParentPosition + (new Vector3(targetX, targetY, 0f) * scale) + new Vector3(0f, 0f, worldPosition.z);
    }

    /// <summary>
    /// グリッド範囲内に位置を制限（パディング考慮）
    /// </summary>
    public static Vector3 ClampToGrid(Vector3 worldPosition, Vector3 gridParentPosition, Vector3 gridOffset, int gridWidth, int gridHeight, int selectorWidth, int selectorHeight, float scale = 1.0f)
    {
        Vector3 vec = worldPosition - gridParentPosition;
        Vector3 local = vec / scale;

        // セレクターの半サイズ
        float halfW = selectorWidth * 0.5f;
        float halfH = selectorHeight * 0.5f;

        // グリッドの物理的な端（セルの外枠）
        // gridOffsetは(0,0)セルの中心。セル幅1なので、左端は -0.5
        float gridLeft = gridOffset.x - 0.5f;
        float gridBottom = gridOffset.y - 0.5f;
        float gridRight = gridOffset.x + gridWidth - 0.5f;
        float gridTop = gridOffset.y + gridHeight - 0.5f;

        // セレクター中心の可動範囲
        float minX = gridLeft + halfW;
        float maxX = gridRight - halfW;
        float minY = gridBottom + halfH;
        float maxY = gridTop - halfH;

        // セレクターがグリッドより大きい場合の考慮（中心に固定など）
        if (minX > maxX) minX = maxX = (gridLeft + gridRight) * 0.5f;
        if (minY > maxY) minY = maxY = (gridBottom + gridTop) * 0.5f;

        float cx = Mathf.Clamp(local.x, minX, maxX);
        float cy = Mathf.Clamp(local.y, minY, maxY);

        return gridParentPosition + (new Vector3(cx, cy, 0f) * scale) + new Vector3(0f, 0f, worldPosition.z);
    }

    /// <summary>
    /// グリッド情報を取得します（GridGeneratorから）
    /// </summary>
    /// <param name="gridGenerator">GridGeneratorインスタンス</param>
    /// <param name="stageData">ステージデータ</param>
    /// <returns>グリッド情報（width, height, parentPosition, offset）</returns>
    public static (int width, int height, Vector3 parentPosition, Vector3 offset) GetGridInfo(GridGenerator gridGenerator, StageDatabase.StageData stageData)
    {
        int width = 0;
        int height = 0;

        if (stageData != null && stageData.massStatus != null && stageData.massStatus.Count > 0)
        {
            height = stageData.massStatus.Count;
            if (stageData.massStatus[0] != null && stageData.massStatus[0].columns != null)
            {
                width = stageData.massStatus[0].columns.Count;
            }
        }

        // MassParentの位置を取得（GridGeneratorから取得）
        Transform massParent = null;
        if (gridGenerator != null)
        {
            System.Reflection.FieldInfo massParentField = typeof(GridGenerator).GetField("massParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (massParentField != null)
            {
                Transform massParentTransform = (Transform)massParentField.GetValue(gridGenerator);
                massParent = massParentTransform != null ? massParentTransform : gridGenerator.transform;
            }
        }

        Vector3 parentPosition = Vector3.zero;
        Vector3 offset = Vector3.zero;

        if (massParent != null)
        {
            parentPosition = massParent.position;
            // GridGeneratorと同じオフセット計算
            offset = new Vector3(-(width - 1) * 0.5f, -(height - 1) * 0.5f, 0f);
        }

        return (width, height, parentPosition, offset);
    }
}

