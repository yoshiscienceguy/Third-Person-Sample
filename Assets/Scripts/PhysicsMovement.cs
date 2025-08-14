using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PhysicsMovement : MonoBehaviour
{
    
    [Header("References")]
    public Transform cameraTransform;    // Drag Main Camera here

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 8.5f;
    public float acceleration = 18f;     // Ground accel
    public float airAcceleration = 6f;   // Air control
    public bool rotateToMove = true;     // Turn to face motion

    [Header("Jumping & Gravity")]
    public float jumpHeight = 1.6f;      // ~1.6m hop
    public float gravity = -9.81f;
    public float fallMultiplier = 2.2f;  // Faster fall
    public float coyoteTime = 0.15f;     // Grace after leaving ground
    public float jumpBuffer = 0.12f;     // Grace before landing

    CharacterController cc;
    Vector3 velocity;   // x/z horizontal, y vertical
    float groundedTimer;
    float jumpBufferTimer;

    public event Action OnJump;
    public event Action OnDeath; // call this from your health logic

    public Vector3 Velocity => velocity; // expose current velocity
    public bool IsGrounded => cc.isGrounded;


    void Awake() { cc = GetComponent<CharacterController>(); }

    void Update()
    {
        // --- INPUT ---
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);
        bool wantsRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (Input.GetKeyDown(KeyCode.Space)) jumpBufferTimer = jumpBuffer;

        // --- CAMERA-RELATIVE DIRECTIONS ---
        Vector3 camFwd = cameraTransform.forward; camFwd.y = 0f; camFwd.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();
        Vector3 wishDir = (camFwd * input.y + camRight * input.x);
        if (wishDir.sqrMagnitude > 1e-4f) wishDir.Normalize();

        // --- GROUNDING (CharacterController) ---
        bool grounded = cc.isGrounded;
        if (grounded) groundedTimer = coyoteTime; else groundedTimer -= Time.deltaTime;
        jumpBufferTimer -= Time.deltaTime;

        // --- TARGET SPEED ---
        float targetSpeed = (wantsRun ? runSpeed : walkSpeed) * input.magnitude;

        // --- ACCELERATION (separate horizontal & vertical) ---
        Vector3 horizVel = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 desiredHoriz = wishDir * targetSpeed;
        float accel = grounded ? acceleration : airAcceleration;
        horizVel = Vector3.MoveTowards(horizVel, desiredHoriz, accel * Time.deltaTime);

        // --- JUMP ---
        if (jumpBufferTimer > 0f && groundedTimer > 0f)
        {
            jumpBufferTimer = 0f;
            groundedTimer = 0f;
            velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity); // v = sqrt(2gh)
        }

        // --- GRAVITY & VARIABLE JUMP HEIGHT ---
        bool rising = velocity.y > 0.01f;
        float g = gravity * (rising && !Input.GetKey(KeyCode.Space) ? fallMultiplier : 1f);
        velocity.y += g * Time.deltaTime;

        // Re-apply combined velocity
        velocity.x = horizVel.x;
        velocity.z = horizVel.z;

        // --- MOVE ---
        cc.Move(velocity * Time.deltaTime);

        // Tiny stick to ground
        if (grounded && velocity.y < 0f) velocity.y = -0.1f;

        // --- FACE MOVE DIRECTION (optional) ---
        if (rotateToMove && wishDir.sqrMagnitude > 1e-4f)
        {
            Quaternion look = Quaternion.LookRotation(wishDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 12f);
        }

        if (jumpBufferTimer > 0f && groundedTimer > 0f)
        {
            jumpBufferTimer = 0f;
            groundedTimer = 0f;
            velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity);
            OnJump?.Invoke(); // <— fire event
        }

    }
    public void Die()
    {
        OnDeath?.Invoke();
        // disable input/move here if you want
        enabled = false;
    }
}
