using UnityEngine;

/// <summary>
/// sin関数を使用して一定周期で振動するスクリプト
/// </summary>
public class CharacterVibrator : MonoBehaviour
{
    private static CharacterVibrator _instance;
    public static CharacterVibrator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<CharacterVibrator>();
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

    private Vector3 initialPosition;
    private float timeElapsed = 0f;

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
    }

    private void Start()
    {
        // 初期位置を保存
        initialPosition = transform.localPosition;
    }

    private void Update()
    {
        if (!enableVibration)
        {
            return;
        }

        // 経過時間を更新
        timeElapsed += Time.deltaTime;

        // sin関数を使って振動値を計算
        // sin(2π * t / period) で周期periodの振動を生成
        float sinValue = Mathf.Sin(2f * Mathf.PI * timeElapsed / period);

        // 振動方向に振幅を適用
        Vector3 offset = direction.normalized * (sinValue * amplitude);

        // 初期位置からオフセットを加算
        transform.localPosition = initialPosition + offset;
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
            transform.localPosition = initialPosition;
            timeElapsed = 0f;
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
        initialPosition = transform.localPosition;
        timeElapsed = 0f;
    }
}

