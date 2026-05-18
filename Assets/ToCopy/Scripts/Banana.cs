using UnityEngine;

public class Banana : MonoBehaviour
{
    [SerializeField] private GameObject m_winUI;
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<ChimpMovement>() != null)
        {
            Time.timeScale = 0;
            m_winUI.SetActive(true);
        }

    }
}
