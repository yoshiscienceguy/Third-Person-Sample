using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorController : MonoBehaviour
{
    [Header("Refs")]
    public PhysicsMovement movement; // drag your movement component
    public Animator animator;               // or auto-grab in Awake

    [Header("Params")]
    public string speedParam = "Speed";
    public string jumpTrigger = "Jump";
    public string deathTrigger = "Death";

    [Header("Tuning")]
    public float speedDampTime = 0.08f;     // smoothing for blend tree
    public bool usePlanarSpeedOnly = true; // ignore vertical speed for Speed

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!movement) movement = GetComponent<PhysicsMovement>();
        if (movement)
        {
            movement.OnJump += HandleJump;
            movement.OnDeath += HandleDeath;
        }
    }

    void OnDestroy()
    {
        if (movement)
        {
            movement.OnJump -= HandleJump;
            movement.OnDeath -= HandleDeath;
        }
    }

    void Update()
    {
        if (!movement) return;

        // Compute speed for the blend tree
        Vector3 v = movement.Velocity;
        if (usePlanarSpeedOnly) v.y = 0f;
        float speed = v.magnitude; // If your blend tree expects normalized 0..1, divide by runSpeed

        animator.SetFloat(speedParam, speed, speedDampTime, Time.deltaTime);
        // If you also want grounded state for landing logic:
        // animator.SetBool("Grounded", movement.IsGrounded);
    }

    void HandleJump() => animator.SetTrigger(jumpTrigger);
    void HandleDeath() => animator.SetTrigger(deathTrigger);
}
