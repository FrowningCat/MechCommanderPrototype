using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    private const string ResolutionIndexKey = "Settings_ResolutionIndex";
    private const string FullscreenKey = "Settings_Fullscreen";
    private const string VolumeKey = "Settings_Volume";

    [Header("UI References")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Slider volumeSlider;

    private readonly List<Resolution> availableResolutions = new();

    private void Start()
    {
        BuildResolutionOptions();
        LoadSettings();
    }

    private void BuildResolutionOptions()
    {
        availableResolutions.Clear();

        HashSet<(int width, int height)> seen = new();
        List<string> options = new();

        foreach (Resolution resolution in Screen.resolutions)
        {
            (int width, int height) key = (resolution.width, resolution.height);

            if (seen.Contains(key))
                continue;

            seen.Add(key);
            availableResolutions.Add(resolution);
            options.Add($"{resolution.width} x {resolution.height}");
        }

        if (resolutionDropdown == null)
            return;

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    private void LoadSettings()
    {
        int savedResolutionIndex = PlayerPrefs.GetInt(ResolutionIndexKey, FindCurrentResolutionIndex());
        bool savedFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);

        if (resolutionDropdown != null && savedResolutionIndex >= 0 && savedResolutionIndex < availableResolutions.Count)
        {
            resolutionDropdown.SetValueWithoutNotify(savedResolutionIndex);
            ApplyResolution(savedResolutionIndex, savedFullscreen);
        }

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(savedFullscreen);

        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(savedVolume);

        AudioListener.volume = savedVolume;
    }

    private int FindCurrentResolutionIndex()
    {
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            if (availableResolutions[i].width == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
            {
                return i;
            }
        }

        return 0;
    }

    public void OnResolutionChanged(int index)
    {
        bool fullscreen = fullscreenToggle != null ? fullscreenToggle.isOn : Screen.fullScreen;

        ApplyResolution(index, fullscreen);

        PlayerPrefs.SetInt(ResolutionIndexKey, index);
        PlayerPrefs.Save();
    }

    public void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;

        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void OnVolumeChanged(float volume)
    {
        AudioListener.volume = volume;

        PlayerPrefs.SetFloat(VolumeKey, volume);
        PlayerPrefs.Save();
    }

    private void ApplyResolution(int index, bool fullscreen)
    {
        if (index < 0 || index >= availableResolutions.Count)
            return;

        Resolution resolution = availableResolutions[index];

        Screen.SetResolution(resolution.width, resolution.height, fullscreen);
    }
}