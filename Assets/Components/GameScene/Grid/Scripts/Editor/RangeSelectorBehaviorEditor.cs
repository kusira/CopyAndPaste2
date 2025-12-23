using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RangeSelectorBehavior))]
public class RangeSelectorBehaviorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var behavior = (RangeSelectorBehavior)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("コピー状態 (Runtime)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("コピー済み", behaviorDebugHasCopy(behavior) ? "はい" : "いいえ");
        EditorGUILayout.LabelField("コピー数", behaviorDebugCopiedCount(behavior).ToString());
        EditorGUILayout.LabelField("回転Index", behaviorDebugRotationIndex(behavior).ToString());
        EditorGUILayout.LabelField("状態メッセージ", behaviorDebugStateMessage(behavior));
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("選択矩形 (グリッド座標)", EditorStyles.boldLabel);
        DrawSelectionBounds(behavior);

        DrawCopyGrid(behavior);

        // 位置変化にも追従するため常にRepaint
        Repaint();

        if (Application.isPlaying)
        {
            // ランタイム中は更新し続ける
            Repaint();
        }
    }

    private bool behaviorDebugHasCopy(RangeSelectorBehavior b)
    {
        var type = typeof(RangeSelectorBehavior);
        var field = type.GetField("debugHasCopy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null && (bool)field.GetValue(b);
    }

    private int behaviorDebugCopiedCount(RangeSelectorBehavior b)
    {
        var type = typeof(RangeSelectorBehavior);
        var field = type.GetField("debugCopiedCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (int)field.GetValue(b) : 0;
    }

    private int behaviorDebugRotationIndex(RangeSelectorBehavior b)
    {
        var type = typeof(RangeSelectorBehavior);
        var field = type.GetField("debugRotationIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (int)field.GetValue(b) : 0;
    }

    private string behaviorDebugStateMessage(RangeSelectorBehavior b)
    {
        var type = typeof(RangeSelectorBehavior);
        var field = type.GetField("debugStateMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (string)field.GetValue(b) : string.Empty;
    }

    private int behaviorDebugSelInt(RangeSelectorBehavior b, string name)
    {
        var type = typeof(RangeSelectorBehavior);
        var field = type.GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (int)field.GetValue(b) : 0;
    }

    private System.Collections.Generic.List<RangeSelectorHelper.CopiedRockData> behaviorDebugOffsets(RangeSelectorBehavior b)
    {
        var type = typeof(RangeSelectorBehavior);
        var field = type.GetField("debugSnapshotOffsets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (System.Collections.Generic.List<RangeSelectorHelper.CopiedRockData>)field.GetValue(b) : null;
    }

    private int behaviorDebugInt(RangeSelectorBehavior b, string name)
    {
        var type = typeof(RangeSelectorBehavior);
        var field = type.GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (int)field.GetValue(b) : 0;
    }

    private void DrawCopyGrid(RangeSelectorBehavior b)
    {
        var offsets = behaviorDebugOffsets(b);
        if (offsets == null || offsets.Count == 0)
        {
            EditorGUILayout.HelpBox("コピー形状なし", MessageType.Info);
            return;
        }

        int minX = behaviorDebugInt(b, "debugMinX");
        int maxX = behaviorDebugInt(b, "debugMaxX");
        int minY = behaviorDebugInt(b, "debugMinY");
        int maxY = behaviorDebugInt(b, "debugMaxY");

        EditorGUILayout.LabelField("コピー形状プレビュー（中心=0,0）", EditorStyles.boldLabel);

        // 左上・右下座標（ワールド座標）
        Vector3 origin = b.transform.position;
        Vector3 topLeft = origin + new Vector3(minX, maxY, 0f);
        Vector3 bottomRight = origin + new Vector3(maxX, minY, 0f);
        EditorGUILayout.LabelField($"左上: ({topLeft.x:0.##}, {topLeft.y:0.##})");
        EditorGUILayout.LabelField($"右下: ({bottomRight.x:0.##}, {bottomRight.y:0.##})");

        for (int y = maxY; y >= minY; y--)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Y{y:00}", GUILayout.Width(40));

            for (int x = minX; x <= maxX; x++)
            {
                bool exists = offsets.Exists(o => o.offset.x == x && o.offset.y == y);
                string cell = exists ? "■" : "□";
                EditorGUILayout.LabelField(cell, GUILayout.Width(18));
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawSelectionBounds(RangeSelectorBehavior b)
    {
        int minX = behaviorDebugSelInt(b, "debugSelMinX");
        int maxX = behaviorDebugSelInt(b, "debugSelMaxX");
        int minY = behaviorDebugSelInt(b, "debugSelMinY");
        int maxY = behaviorDebugSelInt(b, "debugSelMaxY");

        EditorGUILayout.LabelField($"左上(グリッド): ({minX}, {maxY})");
        EditorGUILayout.LabelField($"右下(グリッド): ({maxX}, {minY})");
    }
}

