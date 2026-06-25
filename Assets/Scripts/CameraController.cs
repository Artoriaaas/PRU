using UnityEngine;

public enum CameraView { PlayerSetup, EnemySetup, Battle }

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Player Setup View (Looking down at grid)")]
    public Vector3 playerViewPos = new Vector3(-34.4f, 273.51f, 485.3f);
    public Vector3 playerViewRot = new Vector3(25.5f, -180f, 0f);

    [Header("Enemy Setup View")]
    public Vector3 enemyViewPos = new Vector3(-34.4f, 273.51f, 505.3f);
    public Vector3 enemyViewRot = new Vector3(25.5f, -180f, 0f);

    [Header("Battle View (Cinematic RTS)")]
    public Vector3 battleViewPos = new Vector3(-34.4f, 450f, 900f);
    public Vector3 battleViewRot = new Vector3(38f, -180f, 0f);

    [Header("Camera Settings (Setup Phase)")]
    public bool useOrthographic = false;
    public float orthographicSize = 135f;
    public float fieldOfView = 50f;
    public float transitionSpeed = 4f;

    [Header("Camera Settings (Battle Phase)")]
    public float battleFieldOfView = 60f;

    [Header("Dynamic View Calculation Settings (Setup Phase)")]
    public float baselineCameraHeight = 304.4f;
    public float baselineXOffsetFromLayout = 40f;
    public float baselineZOffsetToGrid = 280.895f;
    public float baselineRotationX = 48.9f;

    [Header("Dynamic View Calculation Settings (Battle Phase)")]
    [Tooltip("Height of battle camera above ground.")]
    public float battleCameraHeight = 200f;
    [Tooltip("X offset of battle camera from battlefield center.")]
    public float battleXOffset = 0f;
    [Tooltip("Z offset behind the midfield center — pulls camera back to reveal sky.")]
    public float battleZOffset = 240f;
    [Tooltip("Pitch angle: 35=more sky, 45=more ground. RTS sweet spot is 38-42.")]
    public float battlePitchAngle = 40f;

    private CameraView currentView = CameraView.PlayerSetup;
    private float _currentFOV;
    private Camera _cam;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }

        CalculateViews();

        // Force snap immediately in Awake — before anything else runs
        ForceApply();
    }

    public void CalculateViews()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;

        // Try to find the generated sub-grids
        GameObject playerGrid = GameObject.Find("PlayerGrid");
        GameObject enemyGrid = GameObject.Find("EnemyGrid");

        float camHeight = baselineCameraHeight;
        float xOffsetFromGridCenter = baselineXOffsetFromLayout;
        float zOffsetToGrid = baselineZOffsetToGrid;

        playerViewRot = new Vector3(baselineRotationX, -180f, 0f);
        enemyViewRot = playerViewRot;

        // Battle view: dedicated cinematic angle — NOT the same as setup
        battleViewRot = new Vector3(battlePitchAngle, -180f, 0f);

        GameObject layout = GameObject.Find("BattlefieldLayout");
        float layoutX = layout != null ? layout.transform.position.x : 0f;
        float layoutZ = layout != null ? layout.transform.position.z : 0f;

        if (playerGrid != null)
        {
            playerViewPos = new Vector3(playerGrid.transform.position.x + xOffsetFromGridCenter, camHeight, playerGrid.transform.position.z + zOffsetToGrid);
        }
        else
        {
            playerViewPos = new Vector3(layoutX + 360f + xOffsetFromGridCenter, camHeight, layoutZ + zOffsetToGrid);
        }

        if (enemyGrid != null)
        {
            enemyViewPos = new Vector3(enemyGrid.transform.position.x + xOffsetFromGridCenter, camHeight, enemyGrid.transform.position.z + zOffsetToGrid);
        }
        else
        {
            enemyViewPos = new Vector3(layoutX - 360f + xOffsetFromGridCenter, camHeight, layoutZ + zOffsetToGrid);
        }

        // Battle view: centered on the midpoint between both grids, pulled back further and higher
        float midX = (playerGrid != null && enemyGrid != null)
            ? (playerGrid.transform.position.x + enemyGrid.transform.position.x) * 0.5f
            : layoutX;
        float midZ = (playerGrid != null && enemyGrid != null)
            ? (playerGrid.transform.position.z + enemyGrid.transform.position.z) * 0.5f
            : layoutZ;

        battleViewPos = new Vector3(midX + battleXOffset, battleCameraHeight, midZ + battleZOffset);

        if (_cam != null)
        {
            // Setup phase always uses the setup FOV (perspective)
            _cam.orthographic = false;
            _cam.fieldOfView = fieldOfView;
            _currentFOV = fieldOfView;
        }
    }

    public void CaptureBaseline()
    {
#if UNITY_EDITOR
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        UnityEditor.Undo.RecordObject(this, "Capture Camera Baseline");
        UnityEditor.Undo.RecordObject(_cam.transform, "Capture Camera Baseline Transform");

        baselineCameraHeight = _cam.transform.position.y;
        baselineRotationX = _cam.transform.eulerAngles.x;
        
        GameObject layout = GameObject.Find("BattlefieldLayout");
        if (layout != null)
        {
            baselineXOffsetFromLayout = _cam.transform.position.x - layout.transform.position.x;
            baselineZOffsetToGrid = _cam.transform.position.z - layout.transform.position.z;
        }
        else
        {
            baselineXOffsetFromLayout = _cam.transform.position.x;
            baselineZOffsetToGrid = _cam.transform.position.z;
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"Captured camera baseline: height={baselineCameraHeight}, xOffset={baselineXOffsetFromLayout}, zOffset={baselineZOffsetToGrid}, rotationX={baselineRotationX}");
#endif
    }

    void Start()
    {
        // Double-enforce in Start in case Awake order was wrong
        ForceApply();
    }

    /// <summary>
    /// Force-apply the current view's position, rotation, and FOV immediately.
    /// Overrides serialized values to ensure correct framing.
    /// </summary>
    public void ForceApply()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;

        if (_cam != null)
        {
            _cam.transform.position = playerViewPos;
            _cam.transform.rotation = Quaternion.Euler(playerViewRot);
            _cam.orthographic = false;
            _cam.fieldOfView = fieldOfView;
            _currentFOV = fieldOfView;
        }
        else
        {
            transform.position = playerViewPos;
            transform.rotation = Quaternion.Euler(playerViewRot);
        }

        Debug.Log($"CameraController: Forced camera to pos={playerViewPos}, rot={playerViewRot}, fov={fieldOfView}");
    }

    void LateUpdate()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;

        Vector3 targetPos = playerViewPos;
        Vector3 targetRot = playerViewRot;
        float targetFOV = fieldOfView;

        switch (currentView)
        {
            case CameraView.PlayerSetup:
                targetPos = playerViewPos;
                targetRot = playerViewRot;
                targetFOV = fieldOfView;
                break;
            case CameraView.EnemySetup:
                targetPos = enemyViewPos;
                targetRot = enemyViewRot;
                targetFOV = fieldOfView;
                break;
            case CameraView.Battle:
                targetPos = battleViewPos;
                targetRot = battleViewRot;
                targetFOV = battleFieldOfView;
                break;
        }

        float lerpT = Time.deltaTime * transitionSpeed;

        if (_cam != null)
        {
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, targetPos, lerpT);
            _cam.transform.rotation = Quaternion.Slerp(_cam.transform.rotation, Quaternion.Euler(targetRot), lerpT);
            // Smoothly interpolate FOV for cinematic transition
            _currentFOV = Mathf.Lerp(_currentFOV, targetFOV, lerpT);
            _cam.fieldOfView = _currentFOV;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, lerpT);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(targetRot), lerpT);
        }
    }

    public void SetView(CameraView view)
    {
        currentView = view;
    }
    
    public CameraView GetCurrentView()
    {
        return currentView;
    }

    public void SnapToViewInEditor(CameraView view)
    {
#if UNITY_EDITOR
        CalculateViews();
        if (_cam == null) return;

        Vector3 targetPos = playerViewPos;
        Vector3 targetRot = playerViewRot;
        switch (view)
        {
            case CameraView.PlayerSetup:
                targetPos = playerViewPos;
                targetRot = playerViewRot;
                break;
            case CameraView.EnemySetup:
                targetPos = enemyViewPos;
                targetRot = enemyViewRot;
                break;
            case CameraView.Battle:
                targetPos = battleViewPos;
                targetRot = battleViewRot;
                break;
        }
        UnityEditor.Undo.RecordObject(_cam.transform, "Preview Camera View");
        _cam.transform.position = targetPos;
        _cam.transform.rotation = Quaternion.Euler(targetRot);
        UnityEditor.EditorUtility.SetDirty(_cam.gameObject);
#endif
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(CameraController))]
public class CameraControllerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        CameraController controller = (CameraController)target;

        UnityEditor.EditorGUILayout.HelpBox(
            "1. Click '📥 Capture Baseline From Camera' when the camera is in its default, centered battlefield view to record design-time offsets.\n" +
            "2. Use the '🎬 Previews' buttons below to test grid framing in the editor.",
            UnityEditor.MessageType.Info
        );

        if (GUILayout.Button("📥  Capture Baseline From Camera", GUILayout.Height(30)))
        {
            controller.CaptureBaseline();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }

        GUILayout.Space(8);

        DrawDefaultInspector();

        GUILayout.Space(12);
        GUILayout.Label("Camera View Editor Previews", UnityEditor.EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("🎬 Player View", GUILayout.Height(30)))
        {
            controller.SnapToViewInEditor(CameraView.PlayerSetup);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }
        if (GUILayout.Button("🎬 Battle View", GUILayout.Height(30)))
        {
            controller.SnapToViewInEditor(CameraView.Battle);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }
        if (GUILayout.Button("🎬 Enemy View", GUILayout.Height(30)))
        {
            controller.SnapToViewInEditor(CameraView.EnemySetup);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }
        GUILayout.EndHorizontal();
    }
}
#endif
