using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [SerializeField] InputActionReference mouseX;
    [SerializeField] InputActionReference mouseY;
    [SerializeField] float moveSpeed;
    [SerializeField] Vector2 mouseSensitivity;
    [SerializeField] Vector2 maxTurnSpeed;
    [SerializeField] bool invertX = false;
    [SerializeField] bool invertY = false;

    Vector2 mouseMovement;

    void Update()
    {
        // X and Y actually translate to pitch and yaw, and thus up/down and left/right instead of the expected vise versa.
        float verticalInput = mouseX.action.ReadValue<float>();
        float horizontalInput = mouseY.action.ReadValue<float>();

        // check that we're not turning too much
        if (verticalInput > maxTurnSpeed.x)
        {
            verticalInput = maxTurnSpeed.x;
        }
        else if (verticalInput < -maxTurnSpeed.x)
        {
            verticalInput = -maxTurnSpeed.x;
        }

        if (horizontalInput > maxTurnSpeed.y)
        {
            horizontalInput = maxTurnSpeed.y;
        }
        else if (horizontalInput < -maxTurnSpeed.y)
        {
            horizontalInput = -maxTurnSpeed.y;
        }

        // X and Y have the intuitive meaning here, where X is left/right and Y is up/down.
        if (invertX)
        {
            horizontalInput = -horizontalInput;
        }

        if (invertY)
        {
            verticalInput = -verticalInput;
        }

        // determine rotation amount
        mouseMovement = new Vector2(horizontalInput * mouseSensitivity.y, verticalInput * mouseSensitivity.x);
    }

    void FixedUpdate()
    {
        transform.Rotate(mouseMovement.x, mouseMovement.y, 0);
        rb.linearVelocity = transform.forward * moveSpeed;
    }
}
