using UnityEngine;

/// <summary>
/// RS用の回転処理のヘルパー関数群
/// </summary>
public static class RSRotationHelper
{
    /// <summary>
    /// 回転時のピボットに基づいた位置シフト量を計算します
    /// </summary>
    /// <param name="rotationStep">回転ステップ（1: 時計回り90度, -1: 反時計回り90度）</param>
    /// <param name="currentRotationIndex">現在の回転インデックス（0-3）</param>
    /// <param name="copiedSize">コピーしたサイズ（W, H）</param>
    /// <param name="selectorPosition">セレクターの現在位置</param>
    /// <param name="mouseWorldPosition">マウスのワールド座標</param>
    /// <returns>位置シフト量</returns>
    public static Vector3 CalculatePivotBasedShift(
        int rotationStep,
        int currentRotationIndex,
        Vector2Int copiedSize,
        Vector3 selectorPosition,
        Vector3 mouseWorldPosition,
        float scale = 1.0f)
    {
        // 更新前の rotationIndex
        int prevRot = (currentRotationIndex - rotationStep + 4) % 4;

        // 更新前のサイズ
        int w = (prevRot % 2 == 0) ? copiedSize.x : copiedSize.y;
        int h = (prevRot % 2 == 0) ? copiedSize.y : copiedSize.x;

        // ピボット候補の中心からの距離（短辺を一辺とする正方形の中心）
        // 長辺の方向に ±(W-H)/2 ずらす
        float diff = (Mathf.Abs(w - h)) * 0.5f;

        Vector3 pivot1 = Vector3.zero;
        Vector3 pivot2 = Vector3.zero;

        if (w > h)
        {
            // 横長：左右に候補
            pivot1 = new Vector3(-diff, 0, 0); // 左
            pivot2 = new Vector3(diff, 0, 0);  // 右
        }
        else if (h > w)
        {
            // 縦長：上下に候補
            pivot1 = new Vector3(0, -diff, 0); // 下
            pivot2 = new Vector3(0, diff, 0);  // 上
        }
        // w == h の場合は (0,0) なのでそのままでOK

        // カーソル位置（中心からの相対座標）をスケールで割ってグリッド単位にする
        Vector3 mouseLocal = (mouseWorldPosition - selectorPosition) / scale;

        // 近い方のピボットを採用
        float d1 = (mouseLocal - pivot1).sqrMagnitude;
        float d2 = (mouseLocal - pivot2).sqrMagnitude;
        Vector3 chosenPivot = (d1 < d2) ? pivot1 : pivot2;

        // 回転ベクトルを計算
        // Pivotを中心に回転するとは、CenterがPivotの周りを回ること。
        // NewCenter = Pivot + Rotate(OldCenter - Pivot)
        // OldCenter = 0 (local)
        // NewCenterLocal = Pivot + Rotate(-Pivot) = Pivot - Rotate(Pivot)
        // Shift = NewCenterLocal

        Vector3 rotatedPivot = Vector3.zero;

        // 回転ロジック
        // rotationStep = 1 (CW 90deg) とする。
        // Rotate(v) = (v.y, -v.x) if step=1
        // Rotate(v) = (-v.y, v.x) if step=-1
        if (rotationStep == 1)
        {
            // CW (時計回り)
            rotatedPivot = new Vector3(chosenPivot.y, -chosenPivot.x, 0);
        }
        else
        {
            // CCW (反時計回り)
            rotatedPivot = new Vector3(-chosenPivot.y, chosenPivot.x, 0);
        }

        Vector3 shift = chosenPivot - rotatedPivot;
        
        // グリッド単位のシフト量をワールド単位（スケール適用）に戻して返す
        return shift * scale;
    }
}

