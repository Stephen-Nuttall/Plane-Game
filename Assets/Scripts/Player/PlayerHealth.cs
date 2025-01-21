using System;
using TMPro;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] Hitbox balloonHitbox;
    [SerializeField] Hitbox propHitbox;
    [SerializeField] TMP_Text balloonCountText;

    [SerializeField] int balloonsLeft = 10;
    [SerializeField] int maxBalloons = 30;

    [SerializeField] int popPenalty = 1;
    [SerializeField] int crashPenalty = 10;

    public event Action<int> onBalloonsCountChange;

    void OnEnable()
    {
        balloonHitbox.onCollision += BalloonPop;
        propHitbox.onCollision += PropellerCollision;
    }

    void OnDisable()
    {
        balloonHitbox.onCollision -= BalloonPop;
        propHitbox.onCollision -= PropellerCollision;
    }

    void Start()
    {
        onBalloonsCountChange?.Invoke(balloonsLeft);
        balloonCountText.text = balloonsLeft.ToString();
    }

    void BalloonPop()
    {
        SubtractBalloons(popPenalty);
    }

    void PropellerCollision()
    {
        SubtractBalloons(crashPenalty);
    }

    void SubtractBalloons(int amount)
    {
        balloonsLeft -= amount;

        if (balloonsLeft <= 0)
        {
            balloonsLeft = 0;
            Die();
        }

        balloonCountText.text = balloonsLeft.ToString();
        onBalloonsCountChange?.Invoke(balloonsLeft);
    }

    public void AddBalloons(int amount)
    {
        balloonsLeft += amount;

        if (balloonsLeft > maxBalloons)
        {
            balloonsLeft = maxBalloons;
        }

        balloonCountText.text = balloonsLeft.ToString();
        onBalloonsCountChange?.Invoke(balloonsLeft);
    }

    void Die()
    {
        //Destroy(gameObject);
    }
}
