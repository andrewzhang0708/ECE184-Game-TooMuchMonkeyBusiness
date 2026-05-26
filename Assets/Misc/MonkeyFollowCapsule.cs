using UnityEngine;

public class MonkeyFollowCapsule : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform targetCapsule;

    [Header("Visual Root")]
    public Transform monkeyVisual;

    [Header("Offset")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffsetEuler = Vector3.zero;

    [Header("Follow Options")]
    public bool followPosition = true;
    public bool followRotation = false;

    [Header("Smoothing")]
    public bool smoothFollow = false;
    public float positionSmooth = 20f;
    public float rotationSmooth = 20f;

    void LateUpdate()
    {
        if (targetCapsule == null || monkeyVisual == null) return;

        Vector3 targetPos = targetCapsule.position + targetCapsule.TransformDirection(positionOffset);
        Quaternion targetRot = targetCapsule.rotation * Quaternion.Euler(rotationOffsetEuler);

        if (smoothFollow)
        {
            if (followPosition)
            {
                monkeyVisual.position = Vector3.Lerp(
                    monkeyVisual.position,
                    targetPos,
                    Time.deltaTime * positionSmooth
                );
            }

            if (followRotation)
            {
                monkeyVisual.rotation = Quaternion.Slerp(
                    monkeyVisual.rotation,
                    targetRot,
                    Time.deltaTime * rotationSmooth
                );
            }
        }
        else
        {
            if (followPosition)
            {
                monkeyVisual.position = targetPos;
            }

            if (followRotation)
            {
                monkeyVisual.rotation = targetRot;
            }
        }
    }
}