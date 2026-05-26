using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Swingable : MonoBehaviour
{
    private HandSwingBar handSwingBar;

    private void Awake()
    {
        handSwingBar = GetComponent<HandSwingBar>();
    }

    public Vector3 GetGrabPoint()
    {
        return handSwingBar != null ? handSwingBar.GrabPoint : transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        ChimpMovement chimp = other.GetComponentInParent<ChimpMovement>();
        SwingPart swingPart = other.GetComponentInParent<SwingPart>();

        if (chimp && swingPart != null)
        {
            if (!chimp.IsConnected())
            {
                chimp.Connect(this, swingPart);
            }
        }
    }
}
