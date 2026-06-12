using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    private static bool openLevelPanelOnStart;
    private static bool openWinPanelOnStart;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject creditPanel;
    public GameObject levelPanel;
    public GameObject introPanel;
    public GameObject winPanel;

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

    public static void OpenLevelPanelOnNextStart()
    {
        openLevelPanelOnStart = true;
    }

    public static void OpenWinPanelOnNextStart()
    {
        openWinPanelOnStart = true;
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
        mainMenuPanel.SetActive(false);
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
