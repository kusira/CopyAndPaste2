using UnityEngine;

/// <summary>
/// キャラクターのアニメーションを管理するスクリプト
/// 状態: Idling, NSelection, Copying, GSelecting, Clearing, PSelection
/// パラメーター(Bool): Idle, NSelect, Copy, Gselect, Clear, Pselect
/// </summary>
public class CharacterAnimator : MonoBehaviour
{
    private static CharacterAnimator _instance;
    public static CharacterAnimator Instance
    {
        get
        {
            if (_instance == null)
            {
                // シーン内から検索
                _instance = Object.FindFirstObjectByType<CharacterAnimator>();
                if (_instance == null)
                {
                    Debug.LogWarning("CharacterAnimatorが見つかりませんでした");
                }
            }
            return _instance;
        }
    }

    [Header("Animator")]
    [Tooltip("Animatorコンポーネントをアサインします")]
    [SerializeField] private Animator animator;

    // Animatorパラメーター名の定数
    private const string PARAM_IDLE = "Idle";
    private const string PARAM_NSELECT = "NSelect";
    private const string PARAM_COPY = "Copy";
    private const string PARAM_GSELECT = "GSelect";
    private const string PARAM_CLEAR = "Clear";
    private const string PARAM_PSELECT = "PSelect";

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

        // Animatorがアサインされていない場合は自動検索
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("CharacterAnimator: Animatorコンポーネントが見つかりませんでした");
            }
        }
    }

    /// <summary>
    /// すべてのBoolパラメーターをfalseにリセットします
    /// </summary>
    private void ResetAllBools()
    {
        if (animator == null) return;

        animator.SetBool(PARAM_IDLE, false);
        animator.SetBool(PARAM_NSELECT, false);
        animator.SetBool(PARAM_COPY, false);
        animator.SetBool(PARAM_GSELECT, false);
        animator.SetBool(PARAM_CLEAR, false);
        animator.SetBool(PARAM_PSELECT, false);
    }

    /// <summary>
    /// Idling状態に設定します
    /// </summary>
    public void SetIdle()
    {
        if (animator == null)
        {
            Debug.LogWarning("CharacterAnimator: Animatorがアサインされていません");
            return;
        }

        ResetAllBools();
        animator.SetBool(PARAM_IDLE, true);
        Debug.Log("アニメーション: Idlingに設定しました");
    }

    /// <summary>
    /// NSelection状態に設定します
    /// </summary>
    public void SetNSelect()
    {
        if (animator == null)
        {
            Debug.LogWarning("CharacterAnimator: Animatorがアサインされていません");
            return;
        }

        ResetAllBools();
        animator.SetBool(PARAM_NSELECT, true);
        Debug.Log("アニメーション: NSelectionに設定しました");
    }

    /// <summary>
    /// Copying状態に設定します
    /// </summary>
    public void SetCopy()
    {
        if (animator == null)
        {
            Debug.LogWarning("CharacterAnimator: Animatorがアサインされていません");
            return;
        }

        ResetAllBools();
        animator.SetBool(PARAM_COPY, true);
        Debug.Log("アニメーション: Copyingに設定しました");
    }

    /// <summary>
    /// GSelecting状態に設定します
    /// </summary>
    public void SetGSelect()
    {
        if (animator == null)
        {
            Debug.LogWarning("CharacterAnimator: Animatorがアサインされていません");
            return;
        }

        ResetAllBools();
        animator.SetBool(PARAM_GSELECT, true);
        Debug.Log("アニメーション: GSelectingに設定しました");
    }

    /// <summary>
    /// Clearing状態に設定します
    /// </summary>
    public void SetClear()
    {
        if (animator == null)
        {
            Debug.LogWarning("CharacterAnimator: Animatorがアサインされていません");
            return;
        }

        ResetAllBools();
        animator.SetBool(PARAM_CLEAR, true);
        Debug.Log("アニメーション: Clearingに設定しました");
    }

    /// <summary>
    /// PSelection状態に設定します
    /// </summary>
    public void SetPSelect()
    {
        if (animator == null)
        {
            Debug.LogWarning("CharacterAnimator: Animatorがアサインされていません");
            return;
        }

        ResetAllBools();
        animator.SetBool(PARAM_PSELECT, true);
        Debug.Log("アニメーション: PSelectionに設定しました");
    }
}
