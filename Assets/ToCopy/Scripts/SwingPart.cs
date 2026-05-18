using System;
using System.Runtime.CompilerServices;
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
        m_grabCollider = m_end.GetComponent<Collider>();
        m_rb = GetComponent<Rigidbody>();
    }

    public void EnableGrabCollider(bool grabbing)
    {
        m_grabCollider.enabled = grabbing;
    }

    public Vector3 GetEndPoint()
    {
        return m_end.position;
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
        m_grabJoint.anchor = m_end.localPosition;
        m_rb.centerOfMass = Vector3.zero;
        m_rb.mass = 1;
        OnConnect?.Invoke();
    }

    public void SetBottom()
    {
        m_rb.centerOfMass = m_end.localPosition;
        m_rb.mass = 5;
    }

    public void SetAerial()
    {
        m_rb.centerOfMass = m_end.localPosition;
        m_rb.mass = 1;
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
