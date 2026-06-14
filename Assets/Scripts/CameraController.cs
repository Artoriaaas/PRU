using UnityEngine;

public enum CameraView { PlayerSetup, EnemySetup, Battle }

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Player Setup View (Looking down at grid)")]
    public Vector3 playerViewPos = new Vector3(-34.4f, 273.51f, 485.3f);
    public Vector3 playerViewRot = new Vector3(31.6f, -180f, 0f);

    [Header("Enemy Setup View")]
    public Vector3 enemyViewPos = new Vector3(-34.4f, 273.51f, 505.3f);
    public Vector3 enemyViewRot = new Vector3(31.6f, -180f, 0f);

    [Header("Battle View (Side Scroller)")]
    public Vector3 battleViewPos = new Vector3(-34.4f, 273.51f, 485.3f);
    public Vector3 battleViewRot = new Vector3(31.6f, -180f, 0f);

    [Header("Camera Settings")]
    public bool useOrthographic = true;
    public float orthographicSize = 135f;
    public float fieldOfView = 50f;
    public float transitionSpeed = 4f;

    [Header("Dynamic View Calculation Settings")]
    public float baselineCameraHeight = 187.31f;
    public float baselineXOffsetFromLayout = 0f;
    public float baselineZOffsetToGrid = 316.79f;

    private CameraView currentView = CameraView.PlayerSetup;
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

        playerViewRot = new Vector3(31.6f, -180f, 0f);
        enemyViewRot = playerViewRot;
        battleViewRot = playerViewRot; // Keep exact same angle as setup view, just sliding on X axis

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

        if (layout != null)
        {
            // Center the battle view camera relative to BattlefieldLayout parent pivot (X = 0 local) with the same height and Z offset as setup views
            battleViewPos = new Vector3(layout.transform.position.x + xOffsetFromGridCenter, camHeight, layout.transform.position.z + zOffsetToGrid);
        }
        else
        {
            battleViewPos = new Vector3(layoutX + xOffsetFromGridCenter, camHeight, layoutZ + zOffsetToGrid);
        }

        if (_cam != null)
        {
            _cam.orthographic = useOrthographic;
            if (useOrthographic)
            {
                _cam.orthographicSize = orthographicSize;
            }
            else
            {
                _cam.fieldOfView = fieldOfView;
            }
        }
    }

    public void CaptureBaseline()
    {
#if UNITY_EDITOR
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;

        UnityEditor.Undo.RecordObject(this, "Capture Camera Baseline");

        baselineCameraHeight = transform.position.y;
        
        GameObject layout = GameObject.Find("BattlefieldLayout");
        if (layout != null)
        {
            baselineXOffsetFromLayout = transform.position.x - layout.transform.position.x;
            baselineZOffsetToGrid = transform.position.z - layout.transform.position.z;
        }
        else
        {
            baselineXOffsetFromLayout = transform.position.x;
            baselineZOffsetToGrid = transform.position.z;
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"Captured camera baseline: height={baselineCameraHeight}, xOffset={baselineXOffsetFromLayout}, zOffset={baselineZOffsetToGrid}");
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
        transform.position = playerViewPos;
        transform.rotation = Quaternion.Euler(playerViewRot);

        if (_cam != null)
        {
            _cam.orthographic = useOrthographic;
            if (useOrthographic)
            {
                _cam.orthographicSize = orthographicSize;
            }
            else
            {
                _cam.fieldOfView = fieldOfView;
            }
        }

        Debug.Log($"CameraController: Forced camera to pos={playerViewPos}, rot={playerViewRot}, fov={fieldOfView}, orthographic={useOrthographic}, orthoSize={orthographicSize}");
    }

    void LateUpdate()
    {
        Vector3 targetPos = playerViewPos;
        Vector3 targetRot = playerViewRot;

        switch (currentView)
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

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * transitionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(targetRot), Time.deltaTime * transitionSpeed);
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
        UnityEditor.Undo.RecordObject(this.transform, "Preview Camera View");
        this.transform.position = targetPos;
        this.transform.rotation = Quaternion.Euler(targetRot);
        UnityEditor.EditorUtility.SetDirty(this.gameObject);
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
