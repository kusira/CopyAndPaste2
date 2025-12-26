using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Image要素の透明な部分をクリック判定から除外するスクリプト
/// </summary>
[RequireComponent(typeof(Image))]
public class PanelHitDetedtor : MonoBehaviour
{
    [Header("Hit Test Settings")]
    [Tooltip("この値以上のアルファ値を持つピクセルのみがクリック可能になります（0.0～1.0）")]
    [SerializeField] [Range(0f, 1f)] private float alphaHitTestMinimumThreshold = 0.1f;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (image == null)
        {
            Debug.LogWarning("PanelHitDetedtor: Imageコンポーネントが見つかりませんでした");
            return;
        }

        // 透明な部分をクリック判定から除外
        image.alphaHitTestMinimumThreshold = alphaHitTestMinimumThreshold;
    }

    private void OnValidate()
    {
        // エディタ上で値が変更された場合も反映
        if (image != null)
        {
            image.alphaHitTestMinimumThreshold = alphaHitTestMinimumThreshold;
        }
    }

    /// <summary>
    /// アルファヒットテストの閾値を設定します
    /// </summary>
    /// <param name="threshold">閾値（0.0～1.0）</param>
    public void SetAlphaHitTestThreshold(float threshold)
    {
        alphaHitTestMinimumThreshold = Mathf.Clamp01(threshold);
        if (image != null)
        {
            image.alphaHitTestMinimumThreshold = alphaHitTestMinimumThreshold;
        }
    }
}

