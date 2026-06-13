using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    private static bool openLevelPanelOnStart;
    private static bool openWinPanelOnStart;
    private static bool openCreditPanelOnStart;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject creditPanel;
    public GameObject levelPanel;
    public GameObject introPanel;
    public GameObject winPanel;

    [Header("Audio Settings")]
    [SerializeField] private Slider overallVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Scenes")]
    // public string levelSelectSceneName = "LevelSelect";
    public string Scene1Name = "Level1";
    public string Scene2Name = "Level2";
    public string Scene3Name = "Level3";
    public string Scene4Name = "Level4";
    public string Scene5Name = "Level5";
    // public string Scene6Name = "Level6";

    private void Start()
    {
        SetupAudioSliders();

        if (openCreditPanelOnStart)
        {
            openCreditPanelOnStart = false;
            OpenCredit();
            return;
        }

        if (openWinPanelOnStart)
        {
            openWinPanelOnStart = false;
            OpenWinPanel();
            return;
        }

        if (openLevelPanelOnStart)
        {
            openLevelPanelOnStart = false;
            StartChooseLevel();
            return;
        }

        BackToMainMenu();
    }

    private void OnDestroy()
    {
        overallVolumeSlider?.onValueChanged.RemoveListener(SetOverallVolume);
        musicVolumeSlider?.onValueChanged.RemoveListener(SetMusicVolume);
        sfxVolumeSlider?.onValueChanged.RemoveListener(SetSfxVolume);
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

    public static void OpenLevelPanelOnNextStart()
    {
        openLevelPanelOnStart = true;
    }

    public static void OpenWinPanelOnNextStart()
    {
        openWinPanelOnStart = true;
    }

    public static void OpenCreditPanelOnNextStart()
    {
        openCreditPanelOnStart = true;
    }

    private void StartLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Public wrappers so UI Buttons can call specific level loads
    public void StartLevel1()
    {
        StartLevel(Scene1Name);
    }

    public void StartLevel2()
    {
        StartLevel(Scene2Name);
    }

    public void StartLevel3()
    {
        StartLevel(Scene3Name);
    }

    public void StartLevel4()
    {
        StartLevel(Scene4Name);
    }

    public void StartLevel5()
    {
        StartLevel(Scene5Name);
    }

    public void Play()
    {
        // SceneManager.LoadScene(levelSelectSceneName);
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditPanel.SetActive(false);
        levelPanel.SetActive(false);
        introPanel.SetActive(true);
        SetPanelActive(winPanel, false);
    }

    public void StartChooseLevel()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditPanel.SetActive(false);
        levelPanel.SetActive(true);
        introPanel.SetActive(false);
        SetPanelActive(winPanel, false);
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(true);
        creditPanel.SetActive(false);
        levelPanel.SetActive(false);
        introPanel.SetActive(false);
        SetPanelActive(winPanel, false);
    }

    public void OpenCredit()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditPanel.SetActive(true);
        levelPanel.SetActive(false);
        introPanel.SetActive(false);
        SetPanelActive(winPanel, false);
    }

    public void OpenWinPanel()
    {
        if (winPanel == null)
        {
            winPanel = FindPanel("Win Panel", "Win");
        }

        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditPanel.SetActive(false);
        levelPanel.SetActive(false);
        introPanel.SetActive(false);
        SetPanelActive(winPanel, true);
    }

    public void BackToMainMenu()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        creditPanel.SetActive(false);
        levelPanel.SetActive(false);
        introPanel.SetActive(false);
        SetPanelActive(winPanel, false);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private static GameObject FindPanel(params string[] panelNames)
    {
        foreach (GameObject rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform child in rootObject.GetComponentsInChildren<Transform>(true))
            {
                foreach (string panelName in panelNames)
                {
                    if (child.name == panelName)
                    {
                        return child.gameObject;
                    }
                }
            }
        }

        Debug.LogWarning("MenuController could not find the Win Panel.");
        return null;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
