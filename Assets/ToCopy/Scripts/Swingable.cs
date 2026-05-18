using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Swingable : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        ChimpMovement chimp = other.GetComponentInParent<ChimpMovement>();
        SwingPart swingPart = other.GetComponentInParent<SwingPart>();
        if (chimp)
        {
            if (!chimp.IsConnected())
            {
                chimp.Connect(this, swingPart);
            }
        }
    }
}
