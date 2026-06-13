using UnityEngine;
using UnityEngine.UI;

public class StartMenuSettingsController : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private Slider overallVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private void Awake()
    {
        SetupAudioSliders();
    }

    private void OnDestroy()
    {
        overallVolumeSlider?.onValueChanged.RemoveListener(SetOverallVolume);
        musicVolumeSlider?.onValueChanged.RemoveListener(SetMusicVolume);
        sfxVolumeSlider?.onValueChanged.RemoveListener(SetSfxVolume);
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

    public void ResetGameProgress()
    {
        CoinProgress.ResetProgress();
        StarProgress.ResetProgress();
        PowerUpProgress.ResetProgress();

        DoubleJumpPurchaseController[] purchaseControllers =
            FindObjectsByType<DoubleJumpPurchaseController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (DoubleJumpPurchaseController purchaseController in purchaseControllers)
        {
            purchaseController.Refresh();
        }

        LevelStarGate[] levelGates = FindObjectsByType<LevelStarGate>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (LevelStarGate levelGate in levelGates)
        {
            levelGate.Refresh();
        }
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
}
