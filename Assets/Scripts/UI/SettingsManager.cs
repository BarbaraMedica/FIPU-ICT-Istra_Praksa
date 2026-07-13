using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("UI elementi")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    private const string VolumeKey = "MasterVolume";
    private const string FullscreenKey = "Fullscreen";

    private void Start()
    {
        LoadSettings();

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    private void LoadSettings()
    {
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);

        bool savedFullscreen = PlayerPrefs.GetInt(
            FullscreenKey,
            Screen.fullScreen ? 1 : 0
        ) == 1;

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(savedVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(savedFullscreen);
        }

        AudioListener.volume = savedVolume;
        Screen.fullScreen = savedFullscreen;
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;

        PlayerPrefs.SetFloat(VolumeKey, volume);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;

        PlayerPrefs.SetInt(
            FullscreenKey,
            isFullscreen ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    public void BackToMainMenu()
    {
        Debug.Log("Povratak u MainMenuScene");

        SceneManager.LoadScene(
            "MainMenuScene",
            LoadSceneMode.Single
        );
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
        }
    }


}