using DG.Tweening;
using UnityEngine;

/// <summary>
/// フレームの4隅にある各パーツのアニメーションを制御するスクリプト
/// 自身の大きさの指定倍率分、指定方向に移動するアニメーションを繰り返します
/// </summary>
public class SelectionPartsAnimator : MonoBehaviour
{
    /// <summary>
    /// アニメーションの移動方向
    /// </summary>
    public enum Direction
    {
        [Tooltip("左上方向")]
        TopLeft,
        [Tooltip("右上方向")]
        TopRight,
        [Tooltip("左下方向")]
        BottomLeft,
        [Tooltip("右下方向")]
        BottomRight
    }

    [Header("アニメーション設定")]
    [Tooltip("移動方向を指定します")]
    [SerializeField] private Direction direction = Direction.TopLeft;

    [Tooltip("自身の大きさに対する移動倍率（0.5倍など）")]
    [SerializeField] [Range(0f, 2f)] private float moveMultiplier = 0.5f;

    [Tooltip("アニメーションの周期（秒）")]
    [SerializeField] [Range(0.1f, 5f)] private float duration = 1f;

    [Tooltip("イージングタイプ")]
    [SerializeField] private Ease easeType = Ease.OutBounce;

    private Vector3 initialPosition;
    private Tween moveTween;

    private void Start()
    {
        StartAnimation();
    }

    private void OnEnable()
    {
        // 有効化時にアニメーションを開始
        if (initialPosition != Vector3.zero || transform.position != Vector3.zero)
        {
            StartAnimation();
        }
    }

    private void OnDisable()
    {
        // 無効化時にアニメーションを停止
        StopAnimation();
    }

    private void OnDestroy()
    {
        // 破棄時にアニメーションを停止
        StopAnimation();
    }

    /// <summary>
    /// アニメーションを開始します
    /// </summary>
    public void StartAnimation()
    {
        StopAnimation(); // 既存のアニメーションを停止

        // 初期位置を保存
        initialPosition = transform.localPosition;

        // 自身の大きさを取得
        Vector3 scale = transform.localScale;
        float sizeX = Mathf.Abs(scale.x);
        float sizeY = Mathf.Abs(scale.y);
        
        // 移動距離を計算（大きさ × 倍率）
        float moveDistanceX = sizeX * moveMultiplier;
        float moveDistanceY = sizeY * moveMultiplier;

        // 方向に応じて移動ベクトルを計算
        Vector3 moveVector = GetMoveVector(moveDistanceX, moveDistanceY);

        // 目標位置を計算
        Vector3 targetPosition = initialPosition + moveVector;

        // DOTweenでアニメーションを作成（往復）
        moveTween = transform.DOLocalMove(targetPosition, duration)
            .SetEase(easeType)
            .SetLoops(-1, LoopType.Yoyo); // 無限ループ、往復
    }

    /// <summary>
    /// アニメーションを停止します
    /// </summary>
    public void StopAnimation()
    {
        if (moveTween != null)
        {
            moveTween.Kill();
            moveTween = null;
        }

        // 初期位置に戻す
        if (initialPosition != Vector3.zero)
        {
            transform.localPosition = initialPosition;
        }
    }

    /// <summary>
    /// 方向に応じた移動ベクトルを取得します
    /// </summary>
    /// <param name="moveX">X方向の移動距離</param>
    /// <param name="moveY">Y方向の移動距離</param>
    /// <returns>移動ベクトル</returns>
    private Vector3 GetMoveVector(float moveX, float moveY)
    {
        switch (direction)
        {
            case Direction.TopLeft:
                return new Vector3(moveX, -moveY, 0f); // 右下方向
            case Direction.TopRight:
                return new Vector3(-moveX, -moveY, 0f); // 左下方向
            case Direction.BottomLeft:
                return new Vector3(moveX, moveY, 0f); // 右上方向
            case Direction.BottomRight:
                return new Vector3(-moveX, moveY, 0f); // 左上方向
            default:
                return new Vector3(moveX, -moveY, 0f);
        }
    }

    /// <summary>
    /// インスペクタで値が変更されたときに呼ばれます
    /// </summary>
    private void OnValidate()
    {
        // エディタ実行中で、アニメーションが開始されている場合のみ再開
        if (Application.isPlaying && moveTween != null)
        {
            StartAnimation();
        }
    }
}

