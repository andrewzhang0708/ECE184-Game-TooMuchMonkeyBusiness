using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Cage : MonoBehaviour
{
    [Header("Pressure Valve")]
    [SerializeField] private Collider pressureValve;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float topContactTolerance = 0.2f;
    [SerializeField] private float horizontalContactPadding = 0.05f;
    [SerializeField] private float minimumDownwardVelocity = -0.05f;

    [Header("Cage Parts")]
    [SerializeField] private Transform cageRoot;
    [SerializeField] private Transform cageTop;
    [SerializeField] private Transform topPivot;
    [SerializeField] private Vector3 topOpenAxis = Vector3.right;
    [SerializeField] private float topOpenAngle = 90f;
    [SerializeField] private float topOpenDuration = 1f;

    [Header("Sink")]
    [SerializeField] private Vector3 sinkDirection = Vector3.down;
    [SerializeField] private float sinkDistance = 4f;
    [SerializeField] private float sinkDuration = 1.25f;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float cameraFocusDuration = 0.75f;
    [SerializeField] private Vector2 cameraFocusOffset;
    [SerializeField] private float cameraShakeDuration = 0.75f;
    [SerializeField] private float cameraShakeStrength = 0.25f;
    [SerializeField] private bool restoreCameraFollow = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sinkClip;
    [SerializeField, Range(0f, 3f)] private float sinkVolume = 1f;

    [Header("Animal")]
    [SerializeField] private GameObject animalToFree;
    [SerializeField] private string animalTriggerMethod = "TriggerFly";
    [SerializeField] private Animator animalAnimator;
    [SerializeField] private string animalAnimatorTrigger;

    [Header("Star Reward")]
    [Tooltip("Unique save ID for this cage. Leave empty to generate one from its scene hierarchy.")]
    [SerializeField] private string achievementId;
    [SerializeField, Min(0)] private int starReward = 1;

    private Quaternion topClosedRotation;
    private Quaternion topPivotClosedRotation;
    private Vector3 cageStartPosition;
    private bool hasTriggered;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (cageRoot == null)
        {
            cageRoot = transform;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (cageTop != null)
        {
            topClosedRotation = cageTop.localRotation;
        }

        if (topPivot != null)
        {
            topPivotClosedRotation = topPivot.localRotation;
        }

        cageStartPosition = cageRoot.position;
        SetupPressureValve();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTrigger(other);
    }

    public void TryTrigger(Collider other)
    {
        if (hasTriggered || other == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
        {
            return;
        }

        if (!IsPressedFromAbove(other))
        {
            return;
        }

        hasTriggered = true;
        PlaySinkSound();
        StartCoroutine(OpenCageRoutine());
    }

    private bool IsPressedFromAbove(Collider other)
    {
        if (pressureValve == null)
        {
            return true;
        }

        Bounds valveBounds = pressureValve.bounds;
        Bounds otherBounds = other.bounds;
        Vector3 otherCenter = otherBounds.center;

        bool isAboveValve =
            otherBounds.min.y >= valveBounds.max.y - topContactTolerance ||
            otherCenter.y > valveBounds.center.y;
        if (!isAboveValve)
        {
            return false;
        }

        bool overlapsValveX =
            otherBounds.max.x >= valveBounds.min.x - horizontalContactPadding &&
            otherBounds.min.x <= valveBounds.max.x + horizontalContactPadding;
        bool overlapsValveZ =
            otherBounds.max.z >= valveBounds.min.z - horizontalContactPadding &&
            otherBounds.min.z <= valveBounds.max.z + horizontalContactPadding;
        if (!overlapsValveX || !overlapsValveZ)
        {
            return false;
        }

        Rigidbody otherRigidbody = other.attachedRigidbody;
        return otherRigidbody == null ||
            otherRigidbody.linearVelocity.y <= minimumDownwardVelocity;
    }

    private void SetupPressureValve()
    {
        if (pressureValve == null)
        {
            return;
        }

        pressureValve.isTrigger = true;

        CagePressureValve relay = pressureValve.GetComponent<CagePressureValve>();
        if (relay == null)
        {
            relay = pressureValve.gameObject.AddComponent<CagePressureValve>();
        }

        relay.Initialize(this);
    }

    private IEnumerator OpenCageRoutine()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        CameraFollow cameraFollow = null;
        Vector3 cameraStartPosition = Vector3.zero;

        if (targetCamera != null)
        {
            cameraFollow = targetCamera.GetComponent<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.enabled = false;
            }

            cameraStartPosition = targetCamera.transform.position;
            Vector3 cameraTargetPosition = new Vector3(
                cageRoot.position.x + cameraFocusOffset.x,
                cageRoot.position.y + cameraFocusOffset.y,
                cameraStartPosition.z
            );

            yield return MoveCamera(cameraStartPosition, cameraTargetPosition, cameraFocusDuration);
        }

        yield return OpenTop();
        AwardStar();

        Coroutine shakeRoutine = null;
        if (targetCamera != null && cameraShakeDuration > 0f && cameraShakeStrength > 0f)
        {
            shakeRoutine = StartCoroutine(ShakeCamera(cameraShakeDuration, cameraShakeStrength));
        }

        yield return SinkCage();

        if (shakeRoutine != null)
        {
            yield return shakeRoutine;
        }

        TriggerAnimal();

        Time.timeScale = previousTimeScale;

        if (cameraFollow != null && restoreCameraFollow)
        {
            cameraFollow.enabled = true;
        }
    }

    private IEnumerator MoveCamera(Vector3 from, Vector3 to, float duration)
    {
        if (targetCamera == null)
        {
            yield break;
        }

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeDuration));
            targetCamera.transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        targetCamera.transform.position = to;
    }

    private IEnumerator OpenTop()
    {
        Transform target = topPivot != null ? topPivot : cageTop;
        if (target == null)
        {
            yield break;
        }

        Vector3 axis = topOpenAxis.sqrMagnitude > 0f ? topOpenAxis.normalized : Vector3.right;
        Quaternion closedRotation = topPivot != null
            ? topPivotClosedRotation
            : topClosedRotation;
        Quaternion openRotation =
            closedRotation * Quaternion.AngleAxis(topOpenAngle, axis);
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, topOpenDuration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeDuration));
            target.localRotation = Quaternion.Slerp(closedRotation, openRotation, t);

            yield return null;
        }

        target.localRotation = openRotation;
    }

    private IEnumerator SinkCage()
    {
        Vector3 direction = sinkDirection.sqrMagnitude > 0f ? sinkDirection.normalized : Vector3.down;
        Vector3 from = cageStartPosition;
        Vector3 to = cageStartPosition + direction * sinkDistance;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, sinkDuration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeDuration));
            cageRoot.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        cageRoot.position = to;
    }

    private IEnumerator ShakeCamera(float duration, float strength)
    {
        Vector3 basePosition = targetCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector2 shakeOffset = Random.insideUnitCircle * strength;
            targetCamera.transform.position = basePosition + new Vector3(shakeOffset.x, shakeOffset.y, 0f);
            yield return null;
        }

        targetCamera.transform.position = basePosition;
    }

    private void PlaySinkSound()
    {
        if (audioSource == null || sinkClip == null)
        {
            return;
        }

        audioSource.PlayOneShot(sinkClip, sinkVolume);
    }

    private void TriggerAnimal()
    {
        if (animalAnimator != null && !string.IsNullOrEmpty(animalAnimatorTrigger))
        {
            animalAnimator.SetTrigger(animalAnimatorTrigger);
        }

        if (animalToFree == null || string.IsNullOrEmpty(animalTriggerMethod))
        {
            return;
        }

        animalToFree.SendMessage(animalTriggerMethod, SendMessageOptions.DontRequireReceiver);
    }

    private void AwardStar()
    {
        string cageAchievementId = string.IsNullOrWhiteSpace(achievementId)
            ? BuildAutomaticAchievementId()
            : achievementId.Trim();

        if (StarProgress.AwardAchievement(cageAchievementId, starReward))
        {
            Debug.Log(
                "Cage opened for the first time. Awarded " + starReward +
                " star(s). Total stars: " + StarProgress.TotalStars,
                this
            );
        }
    }

    private string BuildAutomaticAchievementId()
    {
        StringBuilder hierarchyPath = new StringBuilder();
        Transform current = transform;

        while (current != null)
        {
            hierarchyPath.Insert(
                0,
                "/" + current.name + "[" + current.GetSiblingIndex() + "]"
            );
            current = current.parent;
        }

        return SceneManager.GetActiveScene().name + ".Cage" + hierarchyPath;
    }
}

class CagePressureValve : MonoBehaviour
{
    private Cage cage;

    public void Initialize(Cage owner)
    {
        cage = owner;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (cage != null)
        {
            cage.TryTrigger(other);
        }
    }
}
