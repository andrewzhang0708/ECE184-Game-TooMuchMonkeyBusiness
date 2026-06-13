using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class SpecialWin : MonoBehaviour
{
    [Header("Win")]
    [SerializeField, Min(0f)] private float winDelay = 0.5f;
    [SerializeField] private string startScreenSceneName = "StartScreen";
    [SerializeField] private string playerTag = "Player";

    private bool hasWon;

    private void Awake()
    {
        WinObject normalWin = GetComponent<WinObject>();
        if (normalWin != null)
        {
            normalWin.enabled = false;
        }
    }

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

        StartCoroutine(OpenCreditsRoutine());
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        Rigidbody attachedBody = other.attachedRigidbody;
        if (
            attachedBody != null &&
            !string.IsNullOrEmpty(playerTag) &&
            attachedBody.CompareTag(playerTag)
        )
        {
            return true;
        }

        return other.GetComponentInParent<PlayerHealth>() != null
            || other.GetComponentInParent<PlayerController2D>() != null;
    }

    private IEnumerator OpenCreditsRoutine()
    {
        hasWon = true;

        yield return new WaitForSecondsRealtime(winDelay);

        Time.timeScale = 1f;
        CoinProgress.CommitRun();
        MenuController.OpenCreditPanelOnNextStart();
        SceneManager.LoadScene(startScreenSceneName);
    }
}
