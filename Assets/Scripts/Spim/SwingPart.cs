using System;
using UnityEngine;

public class SwingPart : MonoBehaviour
{

    public event Action OnConnect;

    [SerializeField] private Transform m_end;

    private Collider m_grabCollider;
    private Rigidbody m_rb;

    private HingeJoint m_grabJoint;

    private void Awake()
    {
        if (m_end == null)
        {
            Debug.LogWarning($"{name} SwingPart has no end transform assigned.", this);
        }
        else if (!m_end.TryGetComponent(out m_grabCollider))
        {
            Debug.LogWarning($"{name} SwingPart end '{m_end.name}' has no Collider.", m_end);
        }

        m_rb = GetComponent<Rigidbody>();
    }

    public void EnableGrabCollider(bool grabbing)
    {
        if (m_grabCollider == null)
        {
            return;
        }

        m_grabCollider.enabled = grabbing;
    }

    public Vector3 GetEndPoint()
    {
        return m_end != null ? m_end.position : transform.position;
    }

    public float GetGrabRadius()
    {
        if (m_grabCollider == null)
        {
            return 0.25f;
        }

        Bounds bounds = m_grabCollider.bounds;
        return Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z, 0.25f);
    }

    public Vector3 GetDirection()
    {
        return (GetEndPoint() - transform.position).normalized;
    }

    public void ConnectToSwingable(Swingable swingable)
    {
        if (m_grabJoint)
        {
            Destroy(m_grabJoint);
        }
        m_grabJoint = gameObject.AddComponent<HingeJoint>();
        m_grabJoint.axis = transform.InverseTransformDirection(-Vector3.right);
        m_grabJoint.anchor = GetLocalEndPoint();
        m_rb.centerOfMass = Vector3.zero;
        m_rb.mass = 1;
        OnConnect?.Invoke();
    }

    public void SetBottom()
    {
        m_rb.centerOfMass = GetLocalEndPoint();
        m_rb.mass = 5;
    }

    public void SetAerial()
    {
        m_rb.centerOfMass = GetLocalEndPoint();
        m_rb.mass = 1;
    }

    private Vector3 GetLocalEndPoint()
    {
        return m_end != null ? m_end.localPosition : Vector3.zero;
    }

    public void Disconnect()
    {
        if (m_grabJoint)
        {
            Destroy(m_grabJoint);
        }
    }

    public void SetMotor(JointMotor motor)
    {
        if (TryGetComponent(out HingeJoint joint))
        {
            joint.motor = motor;
        }
    }

    public void SetUseMotor(bool useMotor)
    {
        if (TryGetComponent(out HingeJoint joint))
        {
            joint.useMotor = useMotor;
        }

    }
}
