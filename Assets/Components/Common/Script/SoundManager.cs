using UnityEngine;
using CriWare;
using System.Collections.Generic;

/// <summary>
/// BGMとSEのCriAtomSourceを管理し、VolumeManagerから保存された音量値を適用するマネージャー
/// </summary>
public class SoundManager : MonoBehaviour
{
    [Header("PlayerPrefs設定（VolumeManagerと同じキーを使用）")]
    [SerializeField] private string bgmPrefKey = "VolumeManager_BGM";
    [SerializeField] private string sePrefKey = "VolumeManager_SE";

    [Header("BGM Settings")]
    [Tooltip("BGMの音量を変更する対象のCriAtomSourceのリスト")]
    [SerializeField] private List<CriAtomSource> bgmAtomSources = new List<CriAtomSource>();

    [Header("SE Settings")]
    [Tooltip("SEの音量を変更する対象のCriAtomSourceのリスト")]
    [SerializeField] private List<CriAtomSource> seAtomSources = new List<CriAtomSource>();

    private void Start()
    {
        // VolumeManagerから保存された値を読み込んで適用
        LoadAndApplyVolumes();
    }

    /// <summary>
    /// VolumeManagerから保存された音量値を読み込んで適用します
    /// </summary>
    private void LoadAndApplyVolumes()
    {
        // BGM音量を読み込んで適用
        float bgmVolume = PlayerPrefs.HasKey(bgmPrefKey) ? PlayerPrefs.GetFloat(bgmPrefKey) : 1f;
        ApplyBGMVolume(bgmVolume);

        // SE音量を読み込んで適用
        float seVolume = PlayerPrefs.HasKey(sePrefKey) ? PlayerPrefs.GetFloat(sePrefKey) : 1f;
        ApplySEVolume(seVolume);
    }

    /// <summary>
    /// BGM音量を適用します
    /// </summary>
    public void ApplyBGMVolume(float value)
    {
        foreach (var atomSource in bgmAtomSources)
        {
            if (atomSource != null)
            {
                atomSource.volume = value;
            }
        }
    }

    /// <summary>
    /// SE音量を適用します
    /// </summary>
    public void ApplySEVolume(float value)
    {
        foreach (var atomSource in seAtomSources)
        {
            if (atomSource != null)
            {
                atomSource.volume = value;
            }
        }
    }
}

