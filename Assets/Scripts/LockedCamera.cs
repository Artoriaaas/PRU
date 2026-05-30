using UnityEngine;

/// <summary>
/// ╔══════════════════════════════════════════════════════════════════╗
/// ║                    LOCKED BATTLE CAMERA                          ║
/// ║                                                                  ║
/// ║  Locks the camera to a fixed isometric angle matching the        ║
/// ║  reference game screenshot:                                       ║
/// ║   • Looking down at ~55° from horizontal                         ║
/// ║   • Grid prominently in foreground                               ║
/// ║   • Castle visible in the far background                         ║
/// ║                                                                  ║
/// ║  HOW TO USE:                                                     ║
/// ║  • SceneBootstrapper adds this automatically — nothing to do     ║
/// ║  • OR: manually add to Main Camera and tweak values below        ║
/// ╚══════════════════════════════════════════════════════════════════╝
/// </summary>
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class LockedCamera : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────
    // Inspector — Preset matches the reference image exactly.
    // Change values here if you want to tweak the framing.
    // ──────────────────────────────────────────────────────────────

    [Header("── Position & Rotation ─────────────────────────────────")]
    [Tooltip("World position of the camera. X=left/right, Y=height, Z=depth.")]
    public Vector3 position = new Vector3(9.513777f, 19f, 12.63755f);

    [Tooltip("Euler angles copied from Inspector: X=pitch, Y=yaw, Z=roll.")]
    public Vector3 rotation = new Vector3(51f, -87.369f, 0.011f);

    [Header("── Lens ─────────────────────────────────────────────────")]
    [Range(30f, 90f)]
    [Tooltip("Field of view. 60° matches the Inspector screenshot.")]
    public float fieldOfView = 60f;

    [Header("── Background ───────────────────────────────────────────")]
    public Color skyColor = new Color(0.44f, 0.67f, 0.94f, 1f);
    [Tooltip("Use SolidColor for a clean sky. Use Skybox if you have a skybox assigned.")]
    public CameraClearFlags clearMode = CameraClearFlags.SolidColor;

    [Header("── Lock Settings ─────────────────────────────────────────")]
    [Tooltip("Prevent any script or editor from moving this camera at runtime.")]
    public bool lockPosition = true;
    [Tooltip("Prevent any script or editor from rotating this camera at runtime.")]
    public bool lockRotation = true;

    // ──────────────────────────────────────────────────────────────
    // Private
    // ──────────────────────────────────────────────────────────────
    private Camera _cam;

    // ──────────────────────────────────────────────────────────────
    void Awake()
    {
        _cam = GetComponent<Camera>();
        Apply();
    }

    void LateUpdate()
    {
        // Re-enforce every frame so nothing can drift the camera
        if (lockPosition) transform.position = position;
        if (lockRotation) transform.rotation = Quaternion.Euler(rotation);
    }

    // ──────────────────────────────────────────────────────────────
    /// <summary>Apply all settings to the camera immediately.</summary>
    public void Apply()
    {
        if (_cam == null) _cam = GetComponent<Camera>();

        transform.position = position;
        transform.rotation = Quaternion.Euler(rotation);

        _cam.fieldOfView  = fieldOfView;
        _cam.backgroundColor = skyColor;
        _cam.clearFlags   = clearMode;
        _cam.nearClipPlane = 0.3f;
        _cam.farClipPlane  = 250f;
    }

#if UNITY_EDITOR
    // ── Gizmo: draw a frustum so you can see framing in Scene view ──
    void OnDrawGizmosSelected()
    {
        var cam = GetComponent<Camera>();
        if (cam == null) return;

        Gizmos.color  = new Color(0.3f, 0.9f, 1f, 0.6f);
        Gizmos.matrix = Matrix4x4.TRS(
            new Vector3(0f, 17f, -13.5f),
            Quaternion.Euler(rotation),
            Vector3.one);
        Gizmos.DrawFrustum(Vector3.zero, fieldOfView,
            40f,   // draw-far  (visual only)
            0.3f,  // near
            cam.aspect);
    }
#endif
}

// ═══════════════════════════════════════════════════════════════════════
//  CUSTOM INSPECTOR — quick preset buttons + live preview
// ═══════════════════════════════════════════════════════════════════════
#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(LockedCamera))]
public class LockedCameraEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var lc = (LockedCamera)target;

        GUILayout.Space(10);
        GUILayout.Label("── Quick Presets ──────────────────────────", UnityEditor.EditorStyles.boldLabel);

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("🖥️  Full Grid View\n(default)", GUILayout.Height(44)))
            {
                UnityEditor.Undo.RecordObject(lc, "Camera Preset: FullGrid");
                lc.position    = new Vector3(9.513777f, 19f, 12.63755f);
                lc.rotation    = new Vector3(51f, -87.369f, 0.011f);
                lc.fieldOfView = 60f;
                lc.Apply();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }

            if (GUILayout.Button("📱  Mobile Portrait\n(55° steep)", GUILayout.Height(44)))
            {
                UnityEditor.Undo.RecordObject(lc, "Camera Preset: Portrait");
                lc.position    = new Vector3(0f, 17f, -13.5f);
                lc.rotation    = new Vector3(55f, 0f, 0f);
                lc.fieldOfView = 50f;
                lc.Apply();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }

            if (GUILayout.Button("🔽  More Horizon\n(shallower)", GUILayout.Height(44)))
            {
                UnityEditor.Undo.RecordObject(lc, "Camera Preset: Horizon");
                lc.position    = new Vector3(0f, 13f, -16f);
                lc.rotation    = new Vector3(43f, 0f, 0f);
                lc.fieldOfView = 52f;
                lc.Apply();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }
        }

        GUILayout.Space(6);
        if (GUILayout.Button("▶  Apply Now (Editor Preview)", GUILayout.Height(32)))
        {
            UnityEditor.Undo.RecordObject(lc, "Apply LockedCamera");
            lc.Apply();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}
#endif
