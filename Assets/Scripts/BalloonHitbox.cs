using UnityEngine;

public class BalloonHitbox : MonoBehaviour
{
    [SerializeField] bool moveForward = false;
    [SerializeField] float moveSpeed;
    [SerializeField] Rigidbody rb;

    void OnTriggerEnter(Collider collider)
    {
        if (collider.TryGetComponent<Projectile>(out var proj))
        {
            Destroy(collider);
        }
        Debug.Log("Pop!");
        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (moveForward)
        {
            rb.MovePosition(rb.position + transform.forward * moveSpeed);
        }
    }
}
