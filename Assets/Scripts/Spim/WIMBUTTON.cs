using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WIMBUTTON : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); });
    }
}
