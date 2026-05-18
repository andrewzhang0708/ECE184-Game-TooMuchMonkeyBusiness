using UnityEngine;

public class CamFollow : MonoBehaviour
{
    [SerializeField] private Transform m_target;
    [SerializeField] private float m_distance;

    private void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, m_target.position + m_distance * Vector3.right, 0.01f);
    }
}
