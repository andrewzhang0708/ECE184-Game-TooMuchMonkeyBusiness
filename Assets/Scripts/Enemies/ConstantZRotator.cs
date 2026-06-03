using UnityEngine;

public class ConstantZRotator : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 90f;
    [SerializeField] private bool useLocalAxis = true;

    private void Update()
    {
        Space rotationSpace = useLocalAxis ? Space.Self : Space.World;
        transform.Rotate(Vector3.forward, degreesPerSecond * Time.deltaTime, rotationSpace);
    }
}
