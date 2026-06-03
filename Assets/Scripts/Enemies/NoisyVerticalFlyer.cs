using UnityEngine;

public class NoisyVerticalFlyer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float maxHeight = 3f;

    [Header("Noise")]
    [SerializeField] private float noiseStrength = 0.25f;
    [SerializeField] private float noiseFrequency = 1.5f;

    private Vector3 startPosition;
    private int direction = 1;
    private float noiseOffset;

    private void Awake()
    {
        startPosition = transform.position;
        noiseOffset = Random.value * 1000f;
    }

    private void Update()
    {
        float topY = startPosition.y + Mathf.Max(0f, maxHeight);
        float currentY = transform.position.y;

        if (currentY >= topY)
        {
            direction = -1;
        }
        else if (currentY <= startPosition.y)
        {
            direction = 1;
        }

        float noise = Mathf.PerlinNoise(noiseOffset, Time.time * noiseFrequency);
        float noiseMultiplier = 1f + ((noise * 2f - 1f) * noiseStrength);
        float verticalSpeed = Mathf.Max(0f, speed * noiseMultiplier);

        Vector3 position = transform.position;
        position.y += direction * verticalSpeed * Time.deltaTime;
        position.y = Mathf.Clamp(position.y, startPosition.y, topY);
        transform.position = position;
    }
}
