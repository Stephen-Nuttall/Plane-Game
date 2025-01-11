using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float moveSpeed;
    [SerializeField] float lifeTime = 1f;
    [SerializeField] int invisFrames = 1;
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] Rigidbody rb;

    void Start()
    {
        StartCoroutine(DespawnTimer());
        StartCoroutine(InvisTimer());
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + transform.forward * moveSpeed);
    }

    void OnCollisionEnter(Collision col)
    {
        Destroy(gameObject);
    }

    IEnumerator DespawnTimer()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }

    IEnumerator InvisTimer()
    {
        if (invisFrames > 0)
        {
            int framesWaited = 0;
            meshRenderer.enabled = false;

            while (framesWaited < invisFrames)
            {
                yield return null;
                framesWaited++;
            }

            meshRenderer.enabled = true;
        }
    }
}
