using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SimpleHoppingEnemy : MonoBehaviour
{
    [Header("Hop")]
    [SerializeField] private float hopHeight = 1.2f;
    [SerializeField] private float hopSpeed = 2.5f;
    [SerializeField] private bool usePhysicsIfAvailable = true;

    private Rigidbody rb;
    private Vector3 startPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = false;
    }

    private void FixedUpdate()
    {
        if (usePhysicsIfAvailable && rb != null && !rb.isKinematic)
        {
            HopWithPhysics();
            return;
        }

        HopWithTransform();
    }

    private void HopWithPhysics()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.y = Mathf.Sin(Time.time * hopSpeed) * hopHeight * hopSpeed;
        rb.linearVelocity = velocity;
    }

    private void HopWithTransform()
    {
        Vector3 position = startPosition;
        position.y += Mathf.Abs(Mathf.Sin(Time.time * hopSpeed)) * hopHeight;
        transform.position = position;
    }
}
