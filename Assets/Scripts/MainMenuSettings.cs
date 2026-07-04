using UnityEngine;
using UnityEngine.UI;

public class MainMenuSettings : MonoBehaviour
{
    public Slider volumeSlider;
    public Text musicStatusText;

    private const string VolumeKey = "GameVolume";

    private void Awake()
    {
        // Load saved volume, default to 1.0 (100%)
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1.0f);
        
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        AudioListener.volume = savedVolume;
        UpdateMusicVisuals(savedVolume);
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
        UpdateMusicVisuals(value);
    }

    private void UpdateMusicVisuals(float volume)
    {
        if (musicStatusText != null)
        {
            if (volume <= 0f)
            {
                musicStatusText.text = "Âm nhạc: TẮT";
            }
            else
            {
                musicStatusText.text = $"Âm nhạc: {Mathf.RoundToInt(volume * 100)}%";
            }
        }
    }
}
