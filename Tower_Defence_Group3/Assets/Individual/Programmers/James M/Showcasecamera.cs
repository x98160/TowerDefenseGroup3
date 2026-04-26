using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Showcase Camera System
/// ----------------------
/// Drop this script onto your Main Camera (or any GameObject).
/// 
/// CONTROLS (Play Mode):
///   Space       - Play / Pause the camera sequence
///   R           - Reset to first waypoint
///   N           - Skip to next waypoint
///   P           - Go to previous waypoint
///   F           - Toggle free-look mode (orbit with mouse)
///   Scroll      - Zoom in/out during free-look
/// 
/// SETTING UP WAYPOINTS (Edit Mode or Play Mode):
///   1. Select this GameObject in the Hierarchy
///   2. In the Inspector, expand "Waypoints"
///   3. Click "Add Waypoint Here" to place one at the camera's current position
///   4. Or manually set size and drag in transforms
///   5. Each waypoint has: position, look-at target, duration, and easing
/// </summary>
public class ShowcaseCamera : MonoBehaviour
{
    // ---------------------------------------------
    // Data
    // ---------------------------------------------

    [System.Serializable]
    public class Waypoint
    {
        public string label = "Waypoint";
        public Vector3 position;
        public Vector3 lookAtPosition;
        public bool useLookAtTarget = false;
        public Transform lookAtTarget;          // Optional: follow a live transform
        [Range(0.5f, 20f)]
        public float duration = 3f;             // Time to spend travelling TO this waypoint
        [Range(0f, 10f)]
        public float holdDuration = 1f;         // Time to hold still at this waypoint
        public EaseType easeIn = EaseType.EaseInOut;
    }

    public enum EaseType { Linear, EaseIn, EaseOut, EaseInOut, Bounce, Elastic }
    public enum PlayMode { Once, Loop, PingPong }

    // ---------------------------------------------
    // Inspector Fields
    // ---------------------------------------------

    [Header("Waypoints")]
    public List<Waypoint> waypoints = new List<Waypoint>();

    [Header("Playback")]
    public bool playOnStart = false;
    public PlayMode playMode = PlayMode.Once;
    [Range(0.1f, 5f)]
    public float globalSpeedMultiplier = 1f;

    [Header("Smoothing")]
    [Range(0f, 1f)]
    public float positionSmoothness = 0f;       // Extra lag on position (0 = exact)
    [Range(0f, 1f)]
    public float rotationSmoothness = 0.05f;    // Smooth rotation blending

    [Header("Free Look (F key)")]
    public float freeLookSensitivity = 3f;
    public float freeLookZoomSpeed = 5f;
    [Range(1f, 20f)]
    public float freeLookDistance = 5f;

    [Header("Gizmos")]
    public Color waypointColor = new Color(0.2f, 0.8f, 1f, 1f);
    public Color pathColor = new Color(1f, 0.6f, 0.1f, 0.8f);
    public bool showLabels = true;

    // ---------------------------------------------
    // Private State
    // ---------------------------------------------

    public bool isPlaying = false;
    public bool isFreeLook = false;
    public int currentWaypointIndex = 0;
    private int direction = 1; // for PingPong
    private float segmentTimer = 0f;
    private bool isHolding = false;
    private float holdTimer = 0f;

    private Vector3 smoothPosition;
    private Quaternion smoothRotation;

    // Free look state
    private float freeLookYaw = 0f;
    private float freeLookPitch = 0f;
    private Vector3 freeLookPivot;

    // ---------------------------------------------
    // Unity Lifecycle
    // ---------------------------------------------

    void Start()
    {
        if (waypoints.Count > 0)
        {
            transform.position = waypoints[0].position;
            transform.LookAt(GetLookAt(waypoints[0]));
            smoothPosition = transform.position;
            smoothRotation = transform.rotation;
        }

        if (playOnStart && waypoints.Count > 1)
            StartPlayback();
    }

    void Update()
    {
        HandleInput();

        if (isFreeLook)
        {
            UpdateFreeLook();
            return;
        }

        if (isPlaying && waypoints.Count > 1)
            UpdatePlayback();
    }

    // ---------------------------------------------
    // Input
    // ---------------------------------------------

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            TogglePlayback();

        if (Input.GetKeyDown(KeyCode.R))
            ResetToStart();

        if (Input.GetKeyDown(KeyCode.N))
            SkipToNext();

        if (Input.GetKeyDown(KeyCode.P))
            SkipToPrevious();

        if (Input.GetKeyDown(KeyCode.F))
            ToggleFreeLook();
    }

    // ---------------------------------------------
    // Playback
    // ---------------------------------------------

    public void StartPlayback()
    {
        isPlaying = true;
        isFreeLook = false;
        currentWaypointIndex = 0;
        segmentTimer = 0f;
        isHolding = false;
    }

    public void TogglePlayback()
    {
        if (waypoints.Count < 2) return;
        isPlaying = !isPlaying;
    }

    public void ResetToStart()
    {
        isPlaying = false;
        currentWaypointIndex = 0;
        segmentTimer = 0f;
        isHolding = false;
        direction = 1;
        if (waypoints.Count > 0)
        {
            transform.position = waypoints[0].position;
            transform.LookAt(GetLookAt(waypoints[0]));
            smoothPosition = transform.position;
            smoothRotation = transform.rotation;
        }
    }

    public void SkipToNext()
    {
        currentWaypointIndex = Mathf.Min(currentWaypointIndex + 1, waypoints.Count - 1);
        segmentTimer = 0f;
        isHolding = false;
        SnapToWaypoint(waypoints[currentWaypointIndex]);
    }

    public void SkipToPrevious()
    {
        currentWaypointIndex = Mathf.Max(currentWaypointIndex - 1, 0);
        segmentTimer = 0f;
        isHolding = false;
        SnapToWaypoint(waypoints[currentWaypointIndex]);
    }

    void SnapToWaypoint(Waypoint wp)
    {
        transform.position = wp.position;
        transform.LookAt(GetLookAt(wp));
        smoothPosition = transform.position;
        smoothRotation = transform.rotation;
    }

    void UpdatePlayback()
    {
        if (currentWaypointIndex >= waypoints.Count) return;

        Waypoint current = waypoints[currentWaypointIndex];

        // -- Hold phase --
        if (isHolding)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= current.holdDuration)
            {
                isHolding = false;
                holdTimer = 0f;
                AdvanceWaypoint();
            }
            return;
        }

        // -- Travel phase --
        int prevIndex = currentWaypointIndex - 1;
        if (prevIndex < 0) prevIndex = 0;

        Waypoint from = waypoints[prevIndex];
        Waypoint to = current;

        float totalDuration = to.duration / globalSpeedMultiplier;
        segmentTimer += Time.deltaTime;
        float t = Mathf.Clamp01(segmentTimer / totalDuration);
        float easedT = ApplyEase(t, to.easeIn);

        // Position
        Vector3 targetPos = Vector3.Lerp(from.position, to.position, easedT);
        smoothPosition = positionSmoothness > 0
            ? Vector3.Lerp(smoothPosition, targetPos, Time.deltaTime / Mathf.Max(positionSmoothness * 2f, 0.01f))
            : targetPos;
        transform.position = smoothPosition;

        // Rotation - look at target or interpolate look direction
        Vector3 fromLook = GetLookAt(from);
        Vector3 toLook = GetLookAt(to);
        Vector3 lookTarget = Vector3.Lerp(fromLook, toLook, easedT);

        Quaternion targetRot = Quaternion.LookRotation((lookTarget - transform.position).normalized);
        smoothRotation = Quaternion.Slerp(smoothRotation, targetRot, 1f - rotationSmoothness);
        transform.rotation = smoothRotation;

        // Segment complete
        if (t >= 1f)
        {
            segmentTimer = 0f;
            if (to.holdDuration > 0f)
            {
                isHolding = true;
                holdTimer = 0f;
            }
            else
            {
                AdvanceWaypoint();
            }
        }
    }

    void AdvanceWaypoint()
    {
        if (playMode == PlayMode.Once)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                isPlaying = false;
                currentWaypointIndex = waypoints.Count - 1;
            }
        }
        else if (playMode == PlayMode.Loop)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        }
        else if (playMode == PlayMode.PingPong)
        {
            currentWaypointIndex += direction;
            if (currentWaypointIndex >= waypoints.Count - 1 || currentWaypointIndex <= 0)
                direction = -direction;
            currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, waypoints.Count - 1);
        }
    }

    // ---------------------------------------------
    // Free Look
    // ---------------------------------------------

    void ToggleFreeLook()
    {
        isFreeLook = !isFreeLook;
        isPlaying = false;

        if (isFreeLook)
        {
            freeLookPivot = transform.position + transform.forward * freeLookDistance;
            Vector3 angles = transform.eulerAngles;
            freeLookYaw = angles.y;
            freeLookPitch = angles.x;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void UpdateFreeLook()
    {
        freeLookYaw += Input.GetAxis("Mouse X") * freeLookSensitivity;
        freeLookPitch -= Input.GetAxis("Mouse Y") * freeLookSensitivity;
        freeLookPitch = Mathf.Clamp(freeLookPitch, -89f, 89f);

        freeLookDistance -= Input.mouseScrollDelta.y * freeLookZoomSpeed * Time.deltaTime * 10f;
        freeLookDistance = Mathf.Clamp(freeLookDistance, 0.5f, 50f);

        Quaternion rot = Quaternion.Euler(freeLookPitch, freeLookYaw, 0f);
        transform.position = freeLookPivot - rot * Vector3.forward * freeLookDistance;
        transform.rotation = rot;
    }

    // ---------------------------------------------
    // Helpers
    // ---------------------------------------------

    Vector3 GetLookAt(Waypoint wp)
    {
        if (wp.useLookAtTarget && wp.lookAtTarget != null)
            return wp.lookAtTarget.position;
        return wp.lookAtPosition;
    }

    float ApplyEase(float t, EaseType ease)
    {
        switch (ease)
        {
            case EaseType.Linear: return t;
            case EaseType.EaseIn: return t * t;
            case EaseType.EaseOut: return 1f - (1f - t) * (1f - t);
            case EaseType.EaseInOut: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
            case EaseType.Bounce: return BounceEase(t);
            case EaseType.Elastic: return ElasticEase(t);
            default: return t;
        }
    }

    float BounceEase(float t)
    {
        if (t < 1f / 2.75f) return 7.5625f * t * t;
        else if (t < 2f / 2.75f) { t -= 1.5f / 2.75f; return 7.5625f * t * t + 0.75f; }
        else if (t < 2.5f / 2.75f) { t -= 2.25f / 2.75f; return 7.5625f * t * t + 0.9375f; }
        else { t -= 2.625f / 2.75f; return 7.5625f * t * t + 0.984375f; }
    }

    float ElasticEase(float t)
    {
        if (t == 0 || t == 1) return t;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - 0.075f) * (2f * Mathf.PI) / 0.3f) + 1f;
    }

    // ---------------------------------------------
    // Gizmos
    // ---------------------------------------------

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        for (int i = 0; i < waypoints.Count; i++)
        {
            Waypoint wp = waypoints[i];

            // Sphere at waypoint position
            Gizmos.color = (i == currentWaypointIndex && Application.isPlaying)
                ? Color.yellow
                : waypointColor;
            Gizmos.DrawSphere(wp.position, 0.15f);
            Gizmos.DrawWireSphere(wp.position, 0.3f);

            // Look-at line
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
            Gizmos.DrawLine(wp.position, GetLookAt(wp));
            Gizmos.DrawWireSphere(GetLookAt(wp), 0.08f);

            // Path line between waypoints
            if (i > 0)
            {
                Gizmos.color = pathColor;
                Gizmos.DrawLine(waypoints[i - 1].position, wp.position);
            }

            // Label
            if (showLabels)
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = waypointColor;
                style.fontSize = 12;
                style.fontStyle = FontStyle.Bold;
                Handles.Label(wp.position + Vector3.up * 0.4f, $"[{i}] {wp.label}", style);
            }
        }
    }
#endif
}


// -----------------------------------------------------------------------------
// Custom Inspector
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
[CustomEditor(typeof(ShowcaseCamera))]
public class ShowcaseCameraEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ShowcaseCamera cam = (ShowcaseCamera)target;

        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("-- Quick Actions --", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Waypoint Here", GUILayout.Height(30)))
        {
            Undo.RecordObject(cam, "Add Waypoint");
            var wp = new ShowcaseCamera.Waypoint();
            wp.label = $"Waypoint {cam.waypoints.Count}";
            wp.position = cam.transform.position;
            wp.lookAtPosition = cam.transform.position + cam.transform.forward * 5f;
            cam.waypoints.Add(wp);
            EditorUtility.SetDirty(cam);
        }

        if (GUILayout.Button("Remove Last", GUILayout.Height(30)))
        {
            if (cam.waypoints.Count > 0)
            {
                Undo.RecordObject(cam, "Remove Waypoint");
                cam.waypoints.RemoveAt(cam.waypoints.Count - 1);
                EditorUtility.SetDirty(cam);
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Prev", GUILayout.Height(25)))
            cam.SkipToPrevious();

        if (GUILayout.Button(cam.isPlaying ? "Pause" : "Play", GUILayout.Height(25)))
            cam.TogglePlayback();

        if (GUILayout.Button("Next", GUILayout.Height(25)))
            cam.SkipToNext();

        if (GUILayout.Button("Reset", GUILayout.Height(25)))
            cam.ResetToStart();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "CONTROLS  |  Space: Play/Pause  |  R: Reset  |  N: Next  |  P: Prev  |  F: Free-Look",
            MessageType.Info);

        if (Application.isPlaying)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Status: {(cam.isPlaying ? "Playing" : "Paused")}  --  Waypoint {cam.currentWaypointIndex}/{cam.waypoints.Count - 1}");
        }
    }

    // Allow clicking waypoints in the Scene view to select & edit them
    void OnSceneGUI()
    {
        ShowcaseCamera cam = (ShowcaseCamera)target;
        if (cam.waypoints == null) return;

        for (int i = 0; i < cam.waypoints.Count; i++)
        {
            ShowcaseCamera.Waypoint wp = cam.waypoints[i];

            EditorGUI.BeginChangeCheck();

            // Drag handle on waypoint position
            Vector3 newPos = Handles.PositionHandle(wp.position, Quaternion.identity);

            // Drag handle on look-at position
            Handles.color = new Color(0f, 1f, 0.5f, 0.8f);
            Vector3 newLook = Handles.PositionHandle(wp.lookAtPosition, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(cam, "Move Waypoint");
                wp.position = newPos;
                wp.lookAtPosition = newLook;
                EditorUtility.SetDirty(cam);
            }
        }
    }
}
#endif