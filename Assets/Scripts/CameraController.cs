using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Targets")]
    public Transform target;              // Player root (for orbit focus & yaw)
    public Transform firstPersonAnchor;   // Empty at eye level

    [Header("Zoom")]
    public float minDistance = 0.1f;      // When <= threshold, snap to 1P
    public float maxDistance = 6f;        // Typical Roblox feel: 2f–10f
    public float firstPersonThreshold = 0.35f;
    public float zoomSpeed = 6f;          // Scroll sensitivity

    [Header("Orbit")]
    public float orbitSensitivity = 120f; // Mouse look sensitivity
    public float minPitch = -45f;
    public float maxPitch = 70f;
    public float orbitHeightOffset = 1.6f; // Keeps pivot roughly at torso

    [Header("Smoothing")]
    public float distanceSmooth = 0.08f;   // 0 = snappy, 0.1 = buttery
    public float rotationSmooth = 12f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;   // What blocks the camera
    public float collisionRadius = 0.2f;

    [Header("First Person")]
    public bool hidePlayerInFirstPerson = true;
    public Renderer[] renderersToHide;     // e.g., body/arms to avoid clipping

    [Header("Cursor")]
    public bool lockCursor = true;

    float yaw;
    float pitch;
    float targetDistance;
    float currentDistance;
    bool inFirstPerson;

    void Start()
    {
        transform.SetParent(null);
        if (target == null)
        {
            Debug.LogError("RobloxStyleCamera: Assign a Target (player root).");
            enabled = false; return;
        }
        if (firstPersonAnchor == null)
            firstPersonAnchor = target; // fallback

        Vector3 toCam = transform.position - (target.position + Vector3.up * orbitHeightOffset);
        currentDistance = targetDistance = Mathf.Clamp(toCam.magnitude, minDistance, maxDistance);

        // Initialize yaw/pitch from current camera
        Vector3 fwd = transform.forward;
        yaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        pitch = Mathf.Asin(fwd.y) * Mathf.Rad2Deg;

        if (lockCursor) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    void Update()
    {
        // Mouse look
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        yaw += mx * orbitSensitivity * Time.deltaTime;
        pitch -= my * orbitSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Zoom with scroll wheel
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetDistance -= scroll * zoomSpeed * Time.deltaTime * 60f;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        // Determine 1P vs 3P
        bool wantFirstPerson = (targetDistance <= firstPersonThreshold);
        if (wantFirstPerson != inFirstPerson)
        {
            inFirstPerson = wantFirstPerson;
            ToggleFirstPersonRenderers(inFirstPerson);
        }
    }

    void LateUpdate()
    {
        if (inFirstPerson)
        {
            // First-person: stick to head/eye anchor, rotate camera from yaw/pitch
            Quaternion camRot = Quaternion.Euler(pitch, yaw, 0f);

            // Optionally rotate player only on yaw for natural movement
            Vector3 bodyEuler = target.eulerAngles;
            bodyEuler.y = yaw;
            target.rotation = Quaternion.Lerp(target.rotation, Quaternion.Euler(bodyEuler), Time.deltaTime * rotationSmooth);

            transform.SetPositionAndRotation(firstPersonAnchor.position, camRot);
        }
        else
        {
            // Third-person orbit around a pivot near torso
            Quaternion orbitRot = Quaternion.Euler(pitch, yaw, 0f);

            // smooth distance
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, distanceSmooth)));

            Vector3 pivot = target.position + Vector3.up * orbitHeightOffset;
            Vector3 desiredPos = pivot + orbitRot * new Vector3(0f, 0f, -currentDistance);

            // Camera collision (pull in if obstructed)
            desiredPos = ResolveCollision(pivot, desiredPos);

            // Smooth rotation towards orbit
            transform.rotation = Quaternion.Slerp(transform.rotation, orbitRot, Time.deltaTime * rotationSmooth);
            transform.position = desiredPos;
        }
    }

    Vector3 ResolveCollision(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 0.0001f) return to;
        dir /= dist;

        if (Physics.SphereCast(from, collisionRadius, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            // Pull camera forward just in front of the obstacle
            return hit.point + hit.normal * collisionRadius * 1.05f;
        }
        return to;
    }

    void ToggleFirstPersonRenderers(bool hide)
    {
        if (!hidePlayerInFirstPerson || renderersToHide == null) return;
        foreach (var r in renderersToHide)
        {
            if (r) r.enabled = !hide;
        }
    }
}
