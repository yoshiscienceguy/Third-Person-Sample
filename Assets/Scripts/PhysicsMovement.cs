using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public Transform cameraTransform;   // <- drag Main Camera here in Inspector
    public bool rotateToMove = true;    // turn to face movement direction

    CharacterController cc;
    float vy;

    void Awake() { cc = GetComponent<CharacterController>(); }

    void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);

        // Use camera yaw for movement axes
        Vector3 camFwd = cameraTransform.forward;
        camFwd.y = 0f;
        camFwd = camFwd.normalized;

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight = camRight.normalized;

        Vector3 moveFlat = (camFwd * input.y + camRight * input.x) * moveSpeed;

        // Optional: rotate player to face where they're moving (ignores tiny inputs)
        if (rotateToMove && moveFlat.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(moveFlat, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 12f);
        }

        // Gravity + CharacterController move
        if (cc.isGrounded) vy = -0.1f; else vy += gravity * Time.deltaTime;
        Vector3 velocity = moveFlat; velocity.y = vy;

        cc.Move(velocity * Time.deltaTime);
    }
}
