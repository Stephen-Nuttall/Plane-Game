using UnityEngine;
using TMPro;

public class BalloonHitbox : MonoBehaviour
{
    [SerializeField] TMP_Text balloonCountText;
    [SerializeField] Rigidbody rb;
    [SerializeField] GameObject ignoreCollision;

    [SerializeField] int balloonsLeft = 1;
    [SerializeField] float minSize = 5;
    [SerializeField] float sizeIncrement = 1;
    [SerializeField] bool showBalloonText = false;

    [SerializeField] bool moveForward = false;  // for testing only
    [SerializeField] float moveSpeed;

    void Start()
    {
        balloonCountText.gameObject.SetActive(showBalloonText);
        UpdateBalloonSize();
    }

    void OnTriggerEnter(Collider collider)
    {
        if (ignoreCollision != null && collider.gameObject == ignoreCollision)
            return;

        UpdateBalloonSize();
    }

    void UpdateBalloonSize()
    {
        balloonsLeft--;
        balloonCountText.text = balloonsLeft.ToString();

        if (balloonsLeft <= 0)
        {
            Destroy(gameObject);
        }

        float newScaleVal = minSize + sizeIncrement * balloonsLeft;
        transform.localScale = new Vector3(newScaleVal, newScaleVal, newScaleVal);
    }

    void FixedUpdate()
    {
        if (moveForward)
        {
            rb.MovePosition(rb.position + transform.forward * moveSpeed);
        }
    }
}
