using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject creditPanel;
    public GameObject levelPanel;
    public GameObject introPanel;

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
        BackToMainMenu();
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
    }

    public void StartChooseLevel()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditPanel.SetActive(false);
        levelPanel.SetActive(true);
        introPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        creditPanel.SetActive(false);
        levelPanel.SetActive(false);
        introPanel.SetActive(false);
    }

    public void OpenCredit()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditPanel.SetActive(true);
        levelPanel.SetActive(false);
        introPanel.SetActive(false);
    }

    public void BackToMainMenu()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        creditPanel.SetActive(false);
        levelPanel.SetActive(false);
        introPanel.SetActive(false);
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