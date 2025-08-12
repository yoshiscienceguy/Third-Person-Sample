using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    [Header("Debug")]
    public bool drawGizmos = true;

    float lastSeenTime = -999f;
    float repathTimer;

    enum State { Idle, Chasing, Attacking }
    State state = State.Idle;

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
                if (canSee && dist <= detectRadius)
                {
                    state = State.Chasing;
                    repathTimer = 0f;
                }
                break;

            case State.Chasing:
                // Repath periodically to follow moving target
                repathTimer -= Time.deltaTime;
                if (repathTimer <= 0f)
                {
                    agent.SetDestination(player.position);
                    repathTimer = repathInterval;
                }

                // Switch to attack if close enough
                if (dist <= attackRange + agent.stoppingDistance)
                {
                    state = State.Attacking;
                    agent.ResetPath();
                }
                else if (Time.time - lastSeenTime > loseSightTime)
                {
                    // Lost the player long enough — stop
                    state = State.Idle;
                    agent.ResetPath();
                }
                break;

            case State.Attacking:
                // Face the player
                FaceTarget(player.position);

                // If they move away, resume chase
                if (dist > attackRange + agent.stoppingDistance)
                {
                    state = State.Chasing;
                }
                // If we lose them for too long, go idle
                else if (Time.time - lastSeenTime > loseSightTime)
                {
                    state = State.Idle;
                }

                // TODO: trigger your attack animation / damage here (e.g., via Animator)
                break;
        }
    }

    bool CanSeePlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        float sqrDist = toPlayer.sqrMagnitude;
        if (sqrDist > detectRadius * detectRadius) return false;

        // FOV check
        Vector3 toPlayerDir = toPlayer.normalized;
        Vector3 fwd = transform.forward;
        float angle = Vector3.Angle(fwd, toPlayerDir);
        if (angle > fovDegrees * 0.5f) return false;

        // Line-of-sight raycast (ignore self height differences)
        Vector3 eye = transform.position + Vector3.up * 1.6f;
        Vector3 target = player.position + Vector3.up * 1.2f;
        if (Physics.Raycast(eye, (target - eye).normalized, out RaycastHit hit, detectRadius, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform.IsChildOf(player) || hit.transform == player;
        }
        return true; // nothing hit = clear
    }

    void FaceTarget(Vector3 worldPos)
    {
        Vector3 look = worldPos - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(look, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * faceTargetSpeed);
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
