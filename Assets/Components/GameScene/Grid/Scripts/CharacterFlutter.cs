using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// sin波を使って複数のオブジェクトをz回転で揺らすスクリプト
/// </summary>
public class CharacterFlutter : MonoBehaviour
{
    [System.Serializable]
    public class FlutterItem
    {
        [Tooltip("揺らす対象のTransform")]
        public Transform targetTransform;
        
        [Tooltip("回転の最小値（度）")]
        public float minRotation = -5f;
        
        [Tooltip("回転の最大値（度）")]
        public float maxRotation = 5f;
        
        [Tooltip("揺れの間隔（秒）。この時間で1周期のsin波を描きます")]
        public float flutterInterval = 1f;
        
        [HideInInspector]
        public float initialRotationZ;
        
        [HideInInspector]
        public float timeElapsed;
    }

    [Header("揺れ設定")]
    [Tooltip("揺らすオブジェクトのリスト")]
    [SerializeField] private List<FlutterItem> flutterItems = new List<FlutterItem>();

    [Header("有効/無効")]
    [Tooltip("揺れを有効にするかどうか")]
    [SerializeField] private bool enableFluttering = true;

    private void Start()
    {
        // 各アイテムの初期回転値を保存
        foreach (var item in flutterItems)
        {
            if (item.targetTransform != null)
            {
                // z軸の回転のみを保存（Euler角から取得）
                Vector3 euler = item.targetTransform.localEulerAngles;
                item.initialRotationZ = euler.z;
                item.timeElapsed = 0f;
                
                // flutterIntervalが0以下または無効な場合は警告
                if (item.flutterInterval <= 0f || float.IsNaN(item.flutterInterval) || float.IsInfinity(item.flutterInterval))
                {
                    Debug.LogWarning($"CharacterFlutter: FlutterItemのflutterIntervalが無効です ({item.flutterInterval})。0より大きい値を設定してください。");
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (!enableFluttering)
        {
            // 無効な場合は初期回転に戻す
            foreach (var item in flutterItems)
            {
                if (item.targetTransform != null)
                {
                    Vector3 euler = item.targetTransform.localEulerAngles;
                    euler.z = item.initialRotationZ;
                    item.targetTransform.localEulerAngles = euler;
                }
            }
            return;
        }

        // 各アイテムを揺らす
        foreach (var item in flutterItems)
        {
            if (item.targetTransform == null)
            {
                continue;
            }

            // flutterIntervalが0以下または無効な場合はスキップ
            if (item.flutterInterval <= 0f || float.IsNaN(item.flutterInterval) || float.IsInfinity(item.flutterInterval))
            {
                continue;
            }

            // 経過時間を更新
            item.timeElapsed += Time.deltaTime;

            // sin関数を使って揺れ値を計算
            // sin(2π * t / interval) で周期intervalの振動を生成
            // -1から1の範囲を、minRotationからmaxRotationの範囲にマッピング
            float sinValue = Mathf.Sin(2f * Mathf.PI * item.timeElapsed / item.flutterInterval);
            
            // sinValue (-1～1) を minRotation～maxRotation の範囲にマッピング
            float rotationRange = item.maxRotation - item.minRotation;
            float rotationOffset = item.minRotation + (sinValue + 1f) * 0.5f * rotationRange;

            // 値が有効かチェック（NaNやInfinityを防ぐ）
            if (float.IsNaN(rotationOffset) || float.IsInfinity(rotationOffset))
            {
                continue;
            }

            // 現在のEuler角を取得
            Vector3 euler = item.targetTransform.localEulerAngles;
            
            // z軸の回転を計算（初期値 + 揺れのオフセット）
            // Euler角のzは0～360度なので、初期値も0～360度に正規化
            float currentZ = item.initialRotationZ;
            if (currentZ > 180f)
            {
                currentZ -= 360f;
            }
            
            float newZ = currentZ + rotationOffset;
            
            // -180～180度の範囲に正規化
            while (newZ > 180f) newZ -= 360f;
            while (newZ < -180f) newZ += 360f;
            
            // z軸の回転を更新
            euler.z = newZ;
            item.targetTransform.localEulerAngles = euler;
        }
    }

    /// <summary>
    /// 揺れを有効/無効にします
    /// </summary>
    public void SetFlutteringEnabled(bool enabled)
    {
        enableFluttering = enabled;
        
        // 無効にした場合は初期回転に戻す
        if (!enabled)
        {
            foreach (var item in flutterItems)
            {
                if (item.targetTransform != null)
                {
                    Vector3 euler = item.targetTransform.localEulerAngles;
                    euler.z = item.initialRotationZ;
                    item.targetTransform.localEulerAngles = euler;
                    item.timeElapsed = 0f;
                }
            }
        }
    }

    /// <summary>
    /// 初期回転値を再設定します（現在の回転を新しい初期値として設定）
    /// </summary>
    public void ResetInitialRotations()
    {
        foreach (var item in flutterItems)
        {
            if (item.targetTransform != null)
            {
                Vector3 euler = item.targetTransform.localEulerAngles;
                item.initialRotationZ = euler.z;
                item.timeElapsed = 0f;
            }
        }
    }
}

