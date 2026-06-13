using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PausePanelController : MonoBehaviour
{
    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button hudPauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button backToMapButton;
    [SerializeField] private Toggle cheatModeToggle;

    [Header("Audio Settings")]
    [SerializeField] private Slider overallVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Player")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private int cheatModeLives = 1000;

    [Header("Scene")]
    [SerializeField] private string startScreenSceneName = "StartScreen";

    private bool isPaused;
    private bool cheatModeEnabled;
    private int cachedLives;

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        hudPauseButton?.onClick.AddListener(PauseGame);
        resumeButton?.onClick.AddListener(ResumeGame);
        backToMapButton?.onClick.AddListener(BackToMap);
        cheatModeToggle?.onValueChanged.AddListener(SetCheatMode);
        SetupAudioSliders();

        SetPaused(false);

        if (cheatModeToggle != null && cheatModeToggle.isOn)
        {
            SetCheatMode(true);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            SetPaused(!isPaused);
        }
    }

    private void OnDestroy()
    {
        hudPauseButton?.onClick.RemoveListener(PauseGame);
        resumeButton?.onClick.RemoveListener(ResumeGame);
        backToMapButton?.onClick.RemoveListener(BackToMap);
        cheatModeToggle?.onValueChanged.RemoveListener(SetCheatMode);
        overallVolumeSlider?.onValueChanged.RemoveListener(SetOverallVolume);
        musicVolumeSlider?.onValueChanged.RemoveListener(SetMusicVolume);
        sfxVolumeSlider?.onValueChanged.RemoveListener(SetSfxVolume);

        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }

    public void PauseGame()
    {
        SetPaused(true);
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    public void BackToMap()
    {
        Time.timeScale = 1f;
        isPaused = false;
        CoinProgress.DiscardRun();
        MenuController.OpenLevelPanelOnNextStart();
        SceneManager.LoadScene(startScreenSceneName);
    }

    public void SetCheatMode(bool enabled)
    {
        if (playerHealth == null || enabled == cheatModeEnabled)
        {
            return;
        }

        cheatModeEnabled = enabled;

        if (enabled)
        {
            cachedLives = playerHealth.CurrentLives;
            playerHealth.SetCurrentLives(cheatModeLives);
        }
        else
        {
            playerHealth.SetCurrentLives(cachedLives);
        }
    }

    public void SetOverallVolume(float volume)
    {
        GameAudioSettings.SetOverallVolume(volume);
    }

    public void SetMusicVolume(float volume)
    {
        GameAudioSettings.SetMusicVolume(volume);
    }

    public void SetSfxVolume(float volume)
    {
        GameAudioSettings.SetSfxVolume(volume);
    }

    private void SetupAudioSliders()
    {
        SetupSlider(overallVolumeSlider, GameAudioSettings.OverallVolume, SetOverallVolume);
        SetupSlider(musicVolumeSlider, GameAudioSettings.MusicVolume, SetMusicVolume);
        SetupSlider(sfxVolumeSlider, GameAudioSettings.SfxVolume, SetSfxVolume);
    }

    private static void SetupSlider(
        Slider slider,
        float value,
        UnityEngine.Events.UnityAction<float> listener
    )
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(listener);
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(paused);
        }

        SetSliderActive(overallVolumeSlider, paused);
        SetSliderActive(musicVolumeSlider, paused);
        SetSliderActive(sfxVolumeSlider, paused);
    }

    private static void SetSliderActive(Slider slider, bool active)
    {
        if (slider != null && slider.gameObject.activeSelf != active)
        {
            slider.gameObject.SetActive(active);
        }
    }
}
