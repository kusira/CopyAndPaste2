using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CriWare;

namespace Components.Game.Canvas.Scripts
{
    public class VolumeManager : MonoBehaviour
    {
        [Header("PlayerPrefs設定")]
        [SerializeField] private string bgmPrefKey = "VolumeManager_BGM";
        [SerializeField] private string sePrefKey = "VolumeManager_SE";

        [Header("BGM Settings")]
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private TMP_Text bgmValueText;

        [Header("SE Settings")]
        [SerializeField] private Slider seSlider;
        [SerializeField] private TMP_Text seValueText;

        [Header("スライダー操作時のプレビュー音")]
        [Tooltip("スライダー操作時に鳴らすCriAtomSource（Prefab）")]
        [SerializeField] private CriAtomSource sliderPreviewAtomSource;
        [Tooltip("スライダー操作中に鳴らす最小間隔（秒）。ドラッグ中の鳴りすぎ防止。")]
        [SerializeField] private float sliderPreviewMinIntervalSeconds = 0.08f;

        private float lastSliderPreviewTime = -999f;

        private void Start()
        {
            float savedBgm = PlayerPrefs.HasKey(bgmPrefKey) ? PlayerPrefs.GetFloat(bgmPrefKey) : (bgmSlider != null ? bgmSlider.value : 1f);
            float savedSe = PlayerPrefs.HasKey(sePrefKey) ? PlayerPrefs.GetFloat(sePrefKey) : (seSlider != null ? seSlider.value : 1f);

            if (bgmSlider != null)
            {
                bgmSlider.value = savedBgm;
                SetBGMVolume(bgmSlider.value);
                // リスナー登録
                bgmSlider.onValueChanged.AddListener(OnBgmSliderValueChanged);
            }

            if (seSlider != null)
            {
                seSlider.value = savedSe;
                SetSEVolume(seSlider.value);
                // リスナー登録
                seSlider.onValueChanged.AddListener(OnSeSliderValueChanged);
            }
        }

        private void OnBgmSliderValueChanged(float value)
        {
            SetBGMVolume(value);
        }

        private void OnSeSliderValueChanged(float value)
        {
            SetSEVolume(value);
            PlaySliderPreviewIfNeeded(value);
        }

        private void PlaySliderPreviewIfNeeded(float sliderValue)
        {
            if (sliderPreviewAtomSource == null) return;

            // 音量0のときは鳴らさない
            if (sliderValue <= 0f) return;

            float interval = Mathf.Max(0f, sliderPreviewMinIntervalSeconds);
            if (Time.unscaledTime - lastSliderPreviewTime < interval) return;

            lastSliderPreviewTime = Time.unscaledTime;
            sliderPreviewAtomSource.Play();
        }

        public void SetBGMVolume(float value)
        {
            // UI更新 (0-100)
            if (bgmValueText != null)
            {
                bgmValueText.text = (value * 100f).ToString("F0");
            }

            // PlayerPrefsに保存
            PlayerPrefs.SetFloat(bgmPrefKey, value);
            PlayerPrefs.Save();

            // SoundManagerに通知（存在する場合）
            SoundManager soundManager = FindFirstObjectByType<SoundManager>();
            if (soundManager != null)
            {
                soundManager.ApplyBGMVolume(value);
            }
        }

        public void SetSEVolume(float value)
        {
            // UI更新 (0-100)
            if (seValueText != null)
            {
                seValueText.text = (value * 100f).ToString("F0");
            }

            // PlayerPrefsに保存
            PlayerPrefs.SetFloat(sePrefKey, value);
            PlayerPrefs.Save();

            // SoundManagerに通知（存在する場合）
            SoundManager soundManager = FindFirstObjectByType<SoundManager>();
            if (soundManager != null)
            {
                soundManager.ApplySEVolume(value);
            }
        }
    }
}

