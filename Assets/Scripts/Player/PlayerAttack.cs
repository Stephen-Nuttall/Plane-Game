using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] InputActionReference shootBullet;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform attackPoint;

    void OnEnable()
    {
        shootBullet.action.started += FireBullet;
    }

    void OnDisable()
    {
        shootBullet.action.started -= FireBullet;
    }

    void FireBullet(InputAction.CallbackContext obj)
    {
        Instantiate(bulletPrefab, attackPoint.position, transform.rotation);
    }
}
