using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class EnemyChaser : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;            // Drag your Player root here
    public NavMeshAgent agent;          // Auto-filled if missing
    public LayerMask obstacleMask = ~0; // What can block line-of-sight (e.g., Default, Environment)

    [Header("Detect")]
    public float detectRadius = 12f;    // How far we can detect
    public float fovDegrees = 120f;     // Field of view cone
    public float loseSightTime = 2f;    // How long after losing LOS to stop chasing

    [Header("Chase/Attack")]
    public float attackRange = 1.8f;    // When close enough to "attack"
    public float repathInterval = 0.1f; // How often we update destination while chasing
    public float faceTargetSpeed = 10f; // How quickly to face player at stop


    public event Action OnAttack;
    public event Action OnDeath;
    public event Action OnStartMove;
    public event Action OnStopMove;


    [Header("Debug")]
    public bool drawGizmos = true;

    float lastSeenTime = -999f;
    float repathTimer;
    bool wasMoving;

    enum State { Idle, Chasing, Attacking }
    State state = State.Idle;

    public Vector3 Velocity => agent ? agent.velocity : Vector3.zero;
    public bool IsMoving => agent && agent.velocity.sqrMagnitude > 0.01f;
    public bool HasTarget => player != null;

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!agent) Debug.LogError("EnemyChaseAI needs a NavMeshAgent.");
        if (!player) Debug.LogWarning("Assign the Player transform on EnemyChaseAI.");
    }

    void Update()
    {
        if (!player || !agent) return;

        bool canSee = CanSeePlayer();
        float dist = Vector3.Distance(transform.position, player.position);

        // Memory of last seen time (to allow short occlusions)
        if (canSee) lastSeenTime = Time.time;

        switch (state)
        {
            case State.Idle:
                agent.ResetPath();
                if (canSee && dist <= detectRadius)
                {
                    state = State.Chasing;
                    repathTimer = 0f;
                }
                break;

            case State.Chasing:
                repathTimer -= Time.deltaTime;
                if (repathTimer <= 0f)
                {
                    agent.SetDestination(player.position);
                    repathTimer = repathInterval;
                }

                if (dist <= attackRange + agent.stoppingDistance)
                {
                    state = State.Attacking;
                    agent.ResetPath();
                    OnAttack?.Invoke(); // fire once on entry; drive your attack anim
                }
                else if (Time.time - lastSeenTime > loseSightTime)
                {
                    state = State.Idle;
                    agent.ResetPath();
                }
                break;

            case State.Attacking:
                FaceTarget(player.position);
                if (dist > attackRange + agent.stoppingDistance)
                {
                    state = State.Chasing;
                }
                else if (Time.time - lastSeenTime > loseSightTime)
                {
                    state = State.Idle;
                }
                break;
        }

        // Movement start/stop notifications (for footstep sfx etc.)
        bool moving = IsMoving;
        if (moving != wasMoving)
        {
            if (moving) OnStartMove?.Invoke();
            else OnStopMove?.Invoke();
            wasMoving = moving;
        }
    }

    bool CanSeePlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        if (toPlayer.sqrMagnitude > detectRadius * detectRadius) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > fovDegrees * 0.5f) return false;

        Vector3 eye = transform.position + Vector3.up * 1.6f;
        Vector3 tgt = player.position + Vector3.up * 1.2f;
        if (Physics.Raycast(eye, (tgt - eye).normalized, out var hit, detectRadius, ~0, QueryTriggerInteraction.Ignore))
            return hit.transform == player || hit.transform.IsChildOf(player);
        return true;
    }

    void FaceTarget(Vector3 pos)
    {
        Vector3 d = pos - transform.position; d.y = 0;
        if (d.sqrMagnitude < 1e-4f) return;
        Quaternion r = Quaternion.LookRotation(d, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, r, Time.deltaTime * 10f);
    }

    // Call from your health/damage system:
    public void Die()
    {
        OnDeath?.Invoke();
        if (agent) agent.isStopped = true;
        enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        // Detect radius
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        // FOV
        Vector3 fwd = transform.forward;
        Quaternion left = Quaternion.AngleAxis(-fovDegrees * 0.5f, Vector3.up);
        Quaternion right = Quaternion.AngleAxis(fovDegrees * 0.5f, Vector3.up);
        Vector3 leftDir = left * fwd;
        Vector3 rightDir = right * fwd;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, leftDir * detectRadius);
        Gizmos.DrawRay(transform.position, rightDir * detectRadius);
    }
}
