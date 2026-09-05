using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    private const string VolumePrefKey = "MasterVolume";

    [Header("Optional Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Mixer exposed parameter name")]
    public string mixerVolumeParameter = "MyExposedVolume";

    private void Awake()
    {
        ApplySavedVolume();
    }

    public void SetVolume(float sliderValue)
    {
        float volume = Mathf.Clamp01(sliderValue);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumePrefKey, volume);
        PlayerPrefs.Save();

        if (audioMixer != null)
        {
            float dB = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
            audioMixer.SetFloat(mixerVolumeParameter, dB);
        }
    }

    public float GetSavedVolume()
    {
        return PlayerPrefs.GetFloat(VolumePrefKey, 1f);
    }

    private void ApplySavedVolume()
    {
        SetVolume(GetSavedVolume());
    }
}
