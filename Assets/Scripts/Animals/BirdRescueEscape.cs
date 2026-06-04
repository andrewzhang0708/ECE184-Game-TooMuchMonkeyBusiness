using System.Collections;
using UnityEngine;

public class BirdRescueEscape : MonoBehaviour
{
    [Header("Escape")]
    [SerializeField] private Vector3 escapeDirection = new Vector3(1f, 1f, 0f);
    [SerializeField] private float escapeSpeed = 6f;
    [SerializeField] private float escapeDuration = 10f;
    [SerializeField] private float verticalBobAmplitude = 0.35f;
    [SerializeField] private float verticalBobFrequency = 1.25f;
    [SerializeField] private bool deactivateAfterEscape = true;

    [Header("Optional")]
    [SerializeField] private Transform birdRoot;
    [SerializeField] private BirdFlight birdFlight;
    [SerializeField] private Behaviour[] behavioursToDisableOnEscape;

    private Coroutine escapeRoutine;
    private bool hasEscaped;

    private void Awake()
    {
        if (birdRoot == null)
        {
            birdRoot = transform;
        }

        if (birdFlight == null)
        {
            birdFlight = GetComponentInChildren<BirdFlight>();
        }
    }

    public void TriggerFly()
    {
        StartEscape();
    }

    public void TriggerEscape()
    {
        StartEscape();
    }

    private void StartEscape()
    {
        if (hasEscaped)
        {
            return;
        }

        hasEscaped = true;

        if (birdFlight != null)
        {
            birdFlight.enabled = true;
            birdFlight.TriggerFly();
            birdFlight.SetMovementEnabled(false);
        }

        for (int i = 0; i < behavioursToDisableOnEscape.Length; i++)
        {
            if (
                behavioursToDisableOnEscape[i] != null &&
                behavioursToDisableOnEscape[i] != birdFlight
            )
            {
                behavioursToDisableOnEscape[i].enabled = false;
            }
        }

        if (escapeRoutine != null)
        {
            StopCoroutine(escapeRoutine);
        }

        escapeRoutine = StartCoroutine(EscapeRoutine());
    }

    private IEnumerator EscapeRoutine()
    {
        Vector3 direction = escapeDirection.sqrMagnitude > 0f
            ? escapeDirection.normalized
            : Vector3.right;
        Vector3 basePosition = birdRoot.position;
        float bobOffset = Random.value * Mathf.PI * 2f;
        float elapsed = 0f;

        while (elapsed < escapeDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsed += deltaTime;

            basePosition += direction * escapeSpeed * deltaTime;
            float bob =
                Mathf.Sin((elapsed * verticalBobFrequency * Mathf.PI * 2f) + bobOffset) *
                verticalBobAmplitude;
            birdRoot.position = basePosition + Vector3.up * bob;

            yield return null;
        }

        if (deactivateAfterEscape)
        {
            birdRoot.gameObject.SetActive(false);
        }

        escapeRoutine = null;
    }
}
