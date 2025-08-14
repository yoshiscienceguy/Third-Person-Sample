using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimation : MonoBehaviour
{
    [Header("Refs")]
    public EnemyChaser ai;        // drag EnemyChaseAI
    public NavMeshAgent agent;     // auto-grab if null
    public Animator animator;      // auto-grab if null

    [Header("Animator Params")]
    public string speedParam = "Speed"; // blend tree float
    public string attackTrig = "Attack";
    public string deathTrig = "Death";
    public string hasTargetBool = "HasTarget"; // optional bool in your graph

    [Header("Tuning")]
    public bool normalizeSpeed = false;  // true if your tree expects 0..1
    public float speedDampTime = 0.08f;  // smooth blend
    public bool useAgentRotation = true; // if false, rotate model toward velocity

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!ai) ai = GetComponent<EnemyChaser>();

        if (ai != null)
        {
            ai.OnAttack += HandleAttack;
            ai.OnDeath += HandleDeath;
        }

        // Let Animator (root) control rotation if you want;
        // otherwise let agent do it (default). Keep consistent.
        if (agent) agent.updateRotation = useAgentRotation;
    }

    void OnDestroy()
    {
        if (ai != null)
        {
            ai.OnAttack -= HandleAttack;
            ai.OnDeath -= HandleDeath;
        }
    }

    void Update()
    {
        if (!animator || !agent) return;

        // Compute planar speed (ignore any vertical motion)
        Vector3 v = agent.velocity; v.y = 0f;
        float speed = v.magnitude;

        if (normalizeSpeed && agent.speed > 0.001f)
            speed = Mathf.Clamp01(speed / agent.speed); // 0..1

        animator.SetFloat(speedParam, speed, speedDampTime, Time.deltaTime);

        // Optional: set a "HasTarget" bool for state machine decisions
        if (!string.IsNullOrEmpty(hasTargetBool) && ai != null)
            animator.SetBool(hasTargetBool, ai.HasTarget);

        // If you turned off agent.updateRotation, face move direction for nicer strafing
        if (!useAgentRotation && speed > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(v.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 10f);
        }
    }

    void HandleAttack() { if (!string.IsNullOrEmpty(attackTrig)) animator.SetTrigger(attackTrig); }
    void HandleDeath() { if (!string.IsNullOrEmpty(deathTrig)) animator.SetTrigger(deathTrig); }
}
