using UnityEngine;

public class BalloonPackage : MonoBehaviour
{
    [SerializeField] int amountToAdd = 10;

    void OnTriggerEnter(Collider col)
    {
        if (col.TryGetComponent<BalloonHitbox>(out var balloon))
        {
            balloon.AddBalloons(amountToAdd);
            gameObject.SetActive(false);
        }
    }
}
