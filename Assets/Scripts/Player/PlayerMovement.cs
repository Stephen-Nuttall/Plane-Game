using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Input References")]
    [SerializeField] InputActionReference mouseX;
    [SerializeField] InputActionReference mouseY;
    [SerializeField] InputActionReference tiltLeftKey;
    [SerializeField] InputActionReference tiltRightKey;
    [SerializeField] InputActionReference airBreakKey;
    [SerializeField] InputActionReference precisionModeKey;

    [Header("Basic Forward Movement")]
    [SerializeField] Rigidbody rb;
    [SerializeField] float moveSpeed;

    [Header("Steering With Mouse")]
    [SerializeField] Vector2 mouseSensitivity;
    [SerializeField] Vector2 precisionModeSensitivity;
    [SerializeField] Vector2 maxTurnSpeed;
    [SerializeField] float tiltSpeed = 3f;
    [SerializeField] bool invertX = false;
    [SerializeField] bool invertY = false;
    Vector2 mouseMovement;

    [Header("Gliding")]
    [SerializeField] GameObject PropellerBlurred;
    [SerializeField] GameObject PropellerUnblurred;
    [SerializeField] float minThrustSpeed;
    [SerializeField] float maxThrustSpeed;
    [SerializeField] float thrustFactor;
    [SerializeField] float dragFactor;
    [SerializeField] float minDrag;
    [SerializeField] float lowGlidePercent = 0.1f;
    [SerializeField] float highGlidePercent = 1f;
    float currentGlideThrust = 0;

    // Determine the amount the plane should rotate based on the user's mouse movement.
    void Update()
    {
        // get horizontal and vertical input from mouse movement
        float horizontalInput = mouseX.action.ReadValue<float>();
        float verticalInput = mouseY.action.ReadValue<float>();

        // check that we're not turning too much horizontally
        if (horizontalInput > maxTurnSpeed.x)
        {
            horizontalInput = maxTurnSpeed.x;
        }
        else if (horizontalInput < -maxTurnSpeed.x)
        {
            horizontalInput = -maxTurnSpeed.x;
        }

        // check that we're not turning too much vertically
        if (verticalInput > maxTurnSpeed.y)
        {
            verticalInput = maxTurnSpeed.y;
        }
        else if (verticalInput < -maxTurnSpeed.y)
        {
            verticalInput = -maxTurnSpeed.y;
        }

        // invert X if the user wants to
        if (invertX)
        {
            horizontalInput = -horizontalInput;
        }

        // invert Y if the user wants to
        if (!invertY)
        {
            verticalInput = -verticalInput;
        }

        // Determine rotation amount. Use precision mode sensitivity if the precision mode key is currently being held.
        // Note: X in rotation is pitch and Y in rotation is yaw, so counterintuitively, our vector should be (verticalInput, horizontalInput)
        if (precisionModeKey.action.inProgress)
        {
            mouseMovement = new Vector2(verticalInput * precisionModeSensitivity.y, horizontalInput * precisionModeSensitivity.x);
        }
        else
        {
            mouseMovement = new Vector2(verticalInput * mouseSensitivity.y, horizontalInput * mouseSensitivity.x);
        }

        // Show blurred propeller if not gliding. Else, show unblurred propeller.
        if (!airBreakKey.action.inProgress)
        {
            PropellerBlurred.SetActive(true);
            PropellerUnblurred.SetActive(false);
        }
        else
        {
            PropellerBlurred.SetActive(false);
            PropellerUnblurred.SetActive(true);
        }
    }

    // Rotate the plane based on the calculations in Update() and either push the plane in the direction it's facing or glide if airbreak key is held
    void FixedUpdate()
    {
        // Record tilt input
        float tiltMovement = 0;
        if (tiltLeftKey.action.inProgress)
        {
            tiltMovement += tiltSpeed;
        }
        if (tiltRightKey.action.inProgress)
        {
            tiltMovement -= tiltSpeed;
        }

        transform.Rotate(mouseMovement.x, mouseMovement.y, tiltMovement);

        // if airbreak isn't held, push the plane forward. If it is held, glide.
        if (!airBreakKey.action.inProgress)
        {
            rb.linearVelocity = transform.forward * moveSpeed;
        }
        else
        {
            Glide();
        }
    }

    // CREDIT: https://github.com/SOMENULL/Gliding-Project/blob/main/Gliding%20%5BTutorial%5D/Assets/Scripts/GlidingSystem.cs
    void Glide()
    {
        float pitchInDeg = transform.eulerAngles.x % 360;
        float pitchInRads = transform.eulerAngles.x * Mathf.Deg2Rad;
        float mappedPitch = -Mathf.Sin(pitchInRads);
        float offsetMappedPitch = Mathf.Cos(pitchInRads) * dragFactor;
        float accelerationPercent = pitchInDeg >= 300f ? lowGlidePercent : highGlidePercent;
        Vector3 glidingForce = -Vector3.forward * currentGlideThrust;

        currentGlideThrust += mappedPitch * accelerationPercent * thrustFactor;
        currentGlideThrust = Mathf.Clamp(currentGlideThrust, -maxThrustSpeed, 0);

        if (rb.linearVelocity.magnitude >= minThrustSpeed)
        {
            rb.AddRelativeForce(glidingForce);
            rb.linearDamping = Mathf.Clamp(offsetMappedPitch, minDrag, dragFactor);
        }
        else
        {
            currentGlideThrust = 0;
        }
    }
}
