using UnityEngine;

public class BalloonResize : MonoBehaviour
{
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] float minSize = 1;
    [SerializeField] float sizeIncrement = 0.1f;

    void OnEnable()
    {
        playerHealth.onBalloonsCountChange += UpdateBalloonSize;
    }

    void OnDisable()
    {
        playerHealth.onBalloonsCountChange -= UpdateBalloonSize;
    }

    void UpdateBalloonSize(int balloonsLeft)
    {
        float newScaleVal = minSize + sizeIncrement * balloonsLeft;
        transform.localScale = new Vector3(newScaleVal, newScaleVal, newScaleVal);
    }
}
