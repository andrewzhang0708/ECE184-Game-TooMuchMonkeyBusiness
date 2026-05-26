using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GrabState
{
    Free,
    Trying,
    Arms,
    Legs
}

public class ChimpMovement : MonoBehaviour
{
    [SerializeField] private SwingPart m_arms;
    [SerializeField] private SwingPart m_legs;

    [SerializeField] private float m_hingeForce;
    [SerializeField] private float m_targetVelocity;

    private GrabState m_grabState = GrabState.Free;

    private HingeJoint m_moveJoint;

    private void StartTryGrab()
    {
        m_grabState = GrabState.Trying;
        m_arms.EnableGrabCollider(true);
        m_legs.EnableGrabCollider(true);
    }

    private void StopTryGrab()
    {
        m_grabState = GrabState.Free;
        Disconnect();
        m_arms.EnableGrabCollider(false);
        m_legs.EnableGrabCollider(false);
    }

    private void OnEnable()
    {
        m_arms.OnConnect += m_legs.SetBottom;
        m_legs.OnConnect += m_arms.SetBottom;
    }

    private void OnDisable()
    {
        m_arms.OnConnect -= m_legs.SetBottom;
        m_legs.OnConnect -= m_arms.SetBottom;
    }

    private void Awake()
    {
        Time.timeScale = 1;
        m_moveJoint = m_legs.GetComponent<HingeJoint>();
        m_arms.EnableGrabCollider(false);
        m_legs.EnableGrabCollider(false);
    }

    private void Update()
    {
        if (!WasGrabPressedThisFrame())
        {
            return;
        }

        if (m_grabState == GrabState.Free)
        {
            StartTryGrab();
            return;
        }

        StopTryGrab();
    }

    private void FixedUpdate()
    {
        float swingInput = ReadSwingInput();

        bool swinging = Math.Abs(swingInput) > 0;

        Debug.Log($"GrabState: {m_grabState}");

        int direction = m_grabState == GrabState.Legs ? -1 : 1;

        m_moveJoint.useMotor = swinging;

        m_moveJoint.motor = new JointMotor
        {
            force = Math.Abs(swingInput) * m_hingeForce,
            targetVelocity = direction * swingInput * m_targetVelocity
        };

        if (m_arms.transform.position.y < 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }


    }

    public bool IsConnected()
    {
        return !(m_grabState == GrabState.Free || m_grabState == GrabState.Trying);
    }

    public void Connect(Swingable swingable, SwingPart swingPart)
    {
        if (IsConnected()) return;
        transform.position += swingable.transform.position - swingPart.GetEndPoint();
        // Physics.SyncTransforms();
        m_grabState = swingPart == m_arms ? GrabState.Arms : GrabState.Legs;
        swingPart.ConnectToSwingable(swingable);
    }

    public void Disconnect()
    {
        m_grabState = GrabState.Free;
        m_arms.Disconnect();
        m_arms.SetAerial();
        m_legs.Disconnect();
        m_legs.SetAerial();
    }

    private static bool WasGrabPressedThisFrame()
    {
        return (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
    }

    private static float ReadSwingInput()
    {
        float keyboardInput = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                keyboardInput -= 1f;
            }

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                keyboardInput += 1f;
            }
        }

        float gamepadInput = Gamepad.current != null ? Gamepad.current.leftStick.x.ReadValue() : 0f;

        return Math.Abs(gamepadInput) > Math.Abs(keyboardInput) ? gamepadInput : keyboardInput;
    }
}
