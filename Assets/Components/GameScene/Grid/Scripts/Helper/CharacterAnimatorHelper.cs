using UnityEngine;
using System.Collections;

/// <summary>
/// CharacterAnimatorの操作を簡略化するヘルパークラス
/// </summary>
public class CharacterAnimatorHelper : MonoBehaviour
{
    private static CharacterAnimatorHelper _instance;
    public static CharacterAnimatorHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                // シーン内から検索、なければ作成
                _instance = FindFirstObjectByType<CharacterAnimatorHelper>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CharacterAnimatorHelper");
                    _instance = go.AddComponent<CharacterAnimatorHelper>();
                }
            }
            return _instance;
        }
    }

    private Coroutine currentCoroutine;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void SetIdle()
    {
        StopCurrentCoroutine();
        if (CharacterAnimator.Instance != null)
        {
            CharacterAnimator.Instance.SetIdle();
        }
    }

    public void SetNSelect()
    {
        StopCurrentCoroutine();
        if (CharacterAnimator.Instance != null)
        {
            CharacterAnimator.Instance.SetNSelect();
        }
    }

    public void SetCopy()
    {
        StopCurrentCoroutine();
        if (CharacterAnimator.Instance != null)
        {
            CharacterAnimator.Instance.SetCopy();
        }
    }

    public void SetGSelect()
    {
        StopCurrentCoroutine();
        if (CharacterAnimator.Instance != null)
        {
            CharacterAnimator.Instance.SetGSelect();
        }
    }

    public void SetPSelect()
    {
        StopCurrentCoroutine();
        if (CharacterAnimator.Instance != null)
        {
            CharacterAnimator.Instance.SetPSelect();
        }
    }

    public void SetClear()
    {
        StopCurrentCoroutine();
        if (CharacterAnimator.Instance != null)
        {
            CharacterAnimator.Instance.SetClear();
        }
    }

    /// <summary>
    /// 指定時間待機後にIdle状態に戻ります
    /// </summary>
    /// <param name="delay">待機時間（秒）</param>
    public void ReturnToIdleAfterDelay(float delay = 0.3f)
    {
        StopCurrentCoroutine();
        currentCoroutine = StartCoroutine(ReturnToIdleCoroutine(delay));
    }

    private IEnumerator ReturnToIdleCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (CharacterAnimator.Instance != null)
        {
            CharacterAnimator.Instance.SetIdle();
        }
        currentCoroutine = null;
    }

    private void StopCurrentCoroutine()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
    }
}
