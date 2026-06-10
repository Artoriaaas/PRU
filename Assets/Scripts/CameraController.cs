using UnityEngine;

public enum CameraView { PlayerSetup, EnemySetup, Battle }

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Player Setup View (Looking down at grid)")]
    public Vector3 playerViewPos = new Vector3(-34.4f, 273.51f, 485.3f);
    public Vector3 playerViewRot = new Vector3(31.6f, -181f, 0f);

    [Header("Enemy Setup View")]
    public Vector3 enemyViewPos = new Vector3(-34.4f, 273.51f, 505.3f);
    public Vector3 enemyViewRot = new Vector3(31.6f, -181f, 0f);

    [Header("Battle View (Side Scroller)")]
    public Vector3 battleViewPos = new Vector3(18f, 6.5f, 23.745f);
    public Vector3 battleViewRot = new Vector3(15f, -90f, 0f);

    [Header("Camera Settings")]
    public float fieldOfView = 60f;
    public float transitionSpeed = 4f;

    private CameraView currentView = CameraView.PlayerSetup;
    private Camera _cam;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }

        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;

        // Capture initial transform from editor scene right before Play
        playerViewPos = transform.position;
        playerViewRot = transform.eulerAngles;
        if (_cam != null)
        {
            fieldOfView = _cam.fieldOfView;
        }

        // Keep enemyViewPos aligned to player setup view with the Z offset
        enemyViewPos = playerViewPos + new Vector3(0f, 0f, 20f);
        enemyViewRot = playerViewRot;

        // Force snap immediately in Awake — before anything else runs
        ForceApply();
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
            _cam.orthographic = false;
            _cam.fieldOfView = fieldOfView;
        }

        Debug.Log($"CameraController: Forced camera to pos={playerViewPos}, rot={playerViewRot}, fov={fieldOfView}, orthographic=false");
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
}
