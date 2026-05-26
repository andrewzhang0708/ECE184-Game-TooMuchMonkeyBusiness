using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 180f;
    [SerializeField] private Vector3 rotateVector;

    private void FixedUpdate()
    {
        transform.Rotate(rotateVector * Time.deltaTime * rotationSpeed);
    }
}
