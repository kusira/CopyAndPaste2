using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

namespace Components.Game.Canvas.Scripts
{
    public class VolumeManager : MonoBehaviour
    {
        [Header("Audio Mixer")]
        [Tooltip("AudioMixer をアサインしてください（Exposed Parameters に 'BGM' と 'SE' が必要です）")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string bgmPrefKey = "VolumeManager_BGM";
        [SerializeField] private string sePrefKey = "VolumeManager_SE";

        [Header("BGM Settings")]
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private TMP_Text bgmValueText;

        [Header("SE Settings")]
        [SerializeField] private Slider seSlider;
        [SerializeField] private TMP_Text seValueText;

        [Header("SEプレビュー音（任意）")]
        [Tooltip("SEスライダー操作時に鳴らすAudioSource（未設定なら、SESlider自身のAudioSourceを取得します）")]
        [SerializeField] private AudioSource sePreviewAudioSource;
        [Tooltip("スライダー操作中に鳴らす最小間隔（秒）。ドラッグ中の鳴りすぎ防止。")]
        [SerializeField] private float sePreviewMinIntervalSeconds = 0.08f;

        // AudioMixerのExposed Parameter名
        private const string BGM_PARAM = "BGM";
        private const string SE_PARAM = "SE";

        private float lastSePreviewTime = -999f;

        private void Start()
        {
            float savedBgm = PlayerPrefs.HasKey(bgmPrefKey) ? PlayerPrefs.GetFloat(bgmPrefKey) : (bgmSlider != null ? bgmSlider.value : 1f);
            float savedSe = PlayerPrefs.HasKey(sePrefKey) ? PlayerPrefs.GetFloat(sePrefKey) : (seSlider != null ? seSlider.value : 1f);

            if (bgmSlider != null)
            {
                bgmSlider.value = savedBgm;
                SetBGMVolume(bgmSlider.value);
                // リスナー登録
                bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            }

            if (seSlider != null)
            {
                seSlider.value = savedSe;
                SetSEVolume(seSlider.value);
                // リスナー登録
                seSlider.onValueChanged.AddListener(OnSeSliderValueChanged);

                if (sePreviewAudioSource == null)
                {
                    sePreviewAudioSource = seSlider.GetComponent<AudioSource>();
                }
            }
        }

        private void OnSeSliderValueChanged(float value)
        {
            SetSEVolume(value);
            PlaySePreviewIfNeeded(value);
        }

        private void PlaySePreviewIfNeeded(float seValue)
        {
            if (sePreviewAudioSource == null) return;
            if (sePreviewAudioSource.clip == null) return;

            // 音量0のときは鳴らさない
            if (seValue <= 0f) return;

            float interval = Mathf.Max(0f, sePreviewMinIntervalSeconds);
            if (Time.unscaledTime - lastSePreviewTime < interval) return;

            lastSePreviewTime = Time.unscaledTime;
            sePreviewAudioSource.PlayOneShot(sePreviewAudioSource.clip);
        }

        public void SetBGMVolume(float value)
        {
            // UI更新 (0-100)
            if (bgmValueText != null)
            {
                bgmValueText.text = (value * 100f).ToString("F0");
            }

            // AudioMixer更新 (Decibel変換)
            // スライダー0のときは -80dB (無音) にする
            float db = value <= 0 ? -80f : Mathf.Log10(value) * 20f;
            
            if (audioMixer != null)
            {
                audioMixer.SetFloat(BGM_PARAM, db);
            }

            PlayerPrefs.SetFloat(bgmPrefKey, value);
            PlayerPrefs.Save();
        }

        public void SetSEVolume(float value)
        {
            // UI更新 (0-100)
            if (seValueText != null)
            {
                seValueText.text = (value * 100f).ToString("F0");
            }

            // AudioMixer更新 (Decibel変換)
            float db = value <= 0 ? -80f : Mathf.Log10(value) * 20f;

            if (audioMixer != null)
            {
                audioMixer.SetFloat(SE_PARAM, db);
            }

            PlayerPrefs.SetFloat(sePrefKey, value);
            PlayerPrefs.Save();
        }
    }
}

