using UnityEngine;

public class BalloonPackage : MonoBehaviour
{
    [SerializeField] int amountToAdd = 10;

    void OnTriggerEnter(Collider col)
    {
        if (col.TryGetComponent<PlayerHealth>(out var health))
        {
            health.AddBalloons(amountToAdd);
            gameObject.SetActive(false);
        }
    }
}
