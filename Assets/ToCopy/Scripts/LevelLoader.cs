using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelLoader : Singleton<LevelLoader>
{
    [SerializeField] private float transitionTime;
    [SerializeField] private TextMeshProUGUI loadingText;

    private Animator animator;

    protected override void Awake()
    { 
        base.Awake();

        loadingText.text = "";
        animator = GetComponent<Animator>();
    }

    public void LoadLevel(string name) {
        StartCoroutine(LoadLevelCoroutine(name));
    }

    public void LoadLevelWithoutAsync(string name) {
        SceneManager.LoadScene(name);
    }

    public void Quit() {
        Application.Quit();
    }

    private IEnumerator LoadLevelCoroutine(string name) {
        animator.SetTrigger("Start");
        yield return new WaitForSeconds(transitionTime);
        AsyncOperation loadScene = SceneManager.LoadSceneAsync(name);
        while (!loadScene.isDone) {
            loadingText.text = "Loading... " + ((int)(loadScene.progress*100)).ToString() + "%";
            yield return null;
        }
    }
}
