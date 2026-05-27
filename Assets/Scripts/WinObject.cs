using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinObject : MonoBehaviour
{
    [Header("Win")]
    [SerializeField] private float winDelay = 0.5f;
    [SerializeField] private string startScreenSceneName = "StartScreen";
    [SerializeField] private string playerTag = "Player";

    private bool hasWon;

    private void OnCollisionEnter(Collision collision)
    {
        TryWin(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryWin(other);
    }

    private void TryWin(Collider other)
    {
        if (hasWon || !IsPlayer(other))
        {
            return;
        }

        StartCoroutine(WinRoutine());
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        Rigidbody attachedBody = other.attachedRigidbody;

        if (attachedBody != null && !string.IsNullOrEmpty(playerTag) && attachedBody.CompareTag(playerTag))
        {
            return true;
        }

        return other.GetComponentInParent<PlayerHealth>() != null
            || other.GetComponentInParent<PlayerController2D>() != null
            || other.GetComponentInParent<ChimpMovement>() != null;
    }

    private IEnumerator WinRoutine()
    {
        hasWon = true;
        Debug.Log("Player won.");

        yield return new WaitForSecondsRealtime(winDelay);

        Time.timeScale = 1f;
        MenuController.OpenLevelPanelOnNextStart();
        SceneManager.LoadScene(startScreenSceneName);
    }
}
