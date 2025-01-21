using UnityEngine;
using TMPro;

[System.Obsolete("Replaced by Hitbox, PlayerHealth, and PlayerBalloonSize.")]
public class BalloonHitbox : MonoBehaviour
{
    [SerializeField] TMP_Text balloonCountText;
    [SerializeField] GameObject ignoreCollision;
    [SerializeField] string ignoreCollisionTag = "Doesn't Pop Balloons";

    [SerializeField] int balloonsLeft = 10;
    [SerializeField] int maxBalloons = 30;
    [SerializeField] bool showBalloonText = true;

    [SerializeField] float minSize = 5;
    [SerializeField] float sizeIncrement = 1;

    void Start()
    {
        balloonCountText.gameObject.SetActive(showBalloonText);
        UpdateBalloonSize();
    }

    void OnTriggerEnter(Collider collider)
    {
        if ((ignoreCollision != null && collider.gameObject == ignoreCollision) || collider.CompareTag(ignoreCollisionTag))
            return;

        balloonsLeft--;
        UpdateBalloonSize();
    }

    void UpdateBalloonSize()
    {
        balloonCountText.text = balloonsLeft.ToString();

        if (balloonsLeft <= 0)
        {
            Destroy(gameObject);
        }

        float newScaleVal = minSize + sizeIncrement * balloonsLeft;
        transform.localScale = new Vector3(newScaleVal, newScaleVal, newScaleVal);
    }

    public void AddBalloons(int amount)
    {
        balloonsLeft += amount;

        if (balloonsLeft > maxBalloons)
        {
            balloonsLeft = maxBalloons;
        }

        UpdateBalloonSize();
    }
}
