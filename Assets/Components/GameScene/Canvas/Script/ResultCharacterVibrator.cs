using UnityEngine;

/// <summary>
/// sin関数を使用してスクリーン空間（RectTransform）で一定周期で振動するスクリプト
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ResultCharacterVibrator : MonoBehaviour
{
    private static ResultCharacterVibrator _instance;
    public static ResultCharacterVibrator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<ResultCharacterVibrator>();
            }
            return _instance;
        }
    }

    [Header("Vibration Settings")]
    [Tooltip("振動の振幅（距離）")]
    [SerializeField] private float amplitude = 0.1f;
    
    [Tooltip("振動の周期（秒）")]
    [SerializeField] private float period = 1.0f;
    
    [Tooltip("振動の方向")]
    [SerializeField] private Vector3 direction = Vector3.up;
    
    [Tooltip("振動を有効にするか")]
    [SerializeField] private bool enableVibration = true;

    private RectTransform rectTransform;
    private Vector3 initialPosition;
    private float timeElapsed = 0f;
    private bool isInitialized = false;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            initialPosition = rectTransform.anchoredPosition3D;
            isInitialized = true;
        }
        else
        {
            Debug.LogWarning("ResultCharacterVibrator: RectTransformコンポーネントが見つかりません");
        }
    }

    private void Start()
    {
        // Startでも初期位置を確認・更新（Awakeで保存できなかった場合のフォールバック）
        if (!isInitialized || initialPosition == Vector3.zero)
        {
            if (rectTransform != null && rectTransform.gameObject != null)
            {
                if (rectTransform.anchoredPosition3D != Vector3.zero)
                {
                    initialPosition = rectTransform.anchoredPosition3D;
                    isInitialized = true;
                }
            }
        }
    }

    private void Update()
    {
        // 初期化されていない場合は処理をスキップ
        if (!isInitialized)
        {
            return;
        }

        // RectTransformが破棄されている場合は処理をスキップ
        if (rectTransform == null || rectTransform.gameObject == null)
        {
            return;
        }

        if (!enableVibration)
        {
            // 振動が無効な場合は初期位置に戻す
            ResetToInitialPosition();
            return;
        }

        // 経過時間を更新
        timeElapsed += Time.deltaTime;

        // periodが0以下または無効な場合はスキップ
        if (period <= 0f || float.IsNaN(period) || float.IsInfinity(period))
        {
            return;
        }

        // sin関数を使って振動値を計算
        // sin(2π * t / period) で周期periodの振動を生成
        float sinValue = Mathf.Sin(2f * Mathf.PI * timeElapsed / period);

        // 振動方向に振幅を適用
        Vector3 offset = direction.normalized * (sinValue * amplitude);

        // 初期位置からオフセットを加算
        if (rectTransform != null && rectTransform.gameObject != null)
        {
            rectTransform.anchoredPosition3D = initialPosition + offset;
        }
    }

    /// <summary>
    /// 振動を有効/無効にします
    /// </summary>
    public void SetVibrationEnabled(bool enabled)
    {
        enableVibration = enabled;
        
        // 無効にした場合は初期位置に戻す
        if (!enabled)
        {
            ResetToInitialPosition();
            timeElapsed = 0f;
        }
    }

    private void ResetToInitialPosition()
    {
        if (rectTransform != null && rectTransform.gameObject != null)
        {
            if (rectTransform.anchoredPosition3D != initialPosition)
            {
                rectTransform.anchoredPosition3D = initialPosition;
            }
        }
    }

    /// <summary>
    /// 振動の振幅を設定します
    /// </summary>
    public void SetAmplitude(float newAmplitude)
    {
        amplitude = newAmplitude;
    }

    /// <summary>
    /// 振動の周期を設定します
    /// </summary>
    public void SetPeriod(float newPeriod)
    {
        period = Mathf.Max(0.01f, newPeriod); // 0以下を防ぐ
    }

    /// <summary>
    /// 振動の方向を設定します
    /// </summary>
    public void SetDirection(Vector3 newDirection)
    {
        direction = newDirection;
    }

    /// <summary>
    /// 初期位置を再設定します（現在位置を新しい初期位置として設定）
    /// </summary>
    public void ResetInitialPosition()
    {
        if (rectTransform != null && rectTransform.gameObject != null)
        {
            initialPosition = rectTransform.anchoredPosition3D;
            timeElapsed = 0f;
        }
    }
}

