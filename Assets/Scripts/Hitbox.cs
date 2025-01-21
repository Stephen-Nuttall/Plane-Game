using System;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [SerializeField] GameObject[] collidersToIgnore;
    [SerializeField] string tagToIgnore = "Doesn't Pop Balloons";
    public event Action onCollision;

    void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag(tagToIgnore))
        {
            return;
        }

        foreach (GameObject col in collidersToIgnore)
        {
            if (collider.gameObject == col)
            {
                return;
            }
        }
        print(gameObject.name + " collided with " + collider.gameObject.name);
        onCollision?.Invoke();
    }
}
