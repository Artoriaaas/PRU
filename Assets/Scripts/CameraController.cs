using UnityEngine;

public enum CameraView { PlayerSetup, EnemySetup, Battle }

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Player Setup View (Zoomed & Focused)")]
    public Vector3 playerViewPos = new Vector3(7f, 8.4f, -9f);
    public Vector3 playerViewRot = new Vector3(50f, -89f, 0f);

    [Header("Enemy Setup View (Zoomed in on enemy)")]
    public Vector3 enemyViewPos = new Vector3(7f, 8.4f, 10.5f);
    public Vector3 enemyViewRot = new Vector3(50f, -89f, 0f);

    [Header("Battle View (Side Scroller)")]
    public Vector3 battleViewPos = new Vector3(18f, 6.5f, 0.4f);
    public Vector3 battleViewRot = new Vector3(15f, -90f, 0f);

    public float transitionSpeed = 4f;
    private CameraView currentView = CameraView.PlayerSetup;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Instantly snap to Player Setup
        if (Camera.main != null)
        {
            Camera.main.transform.position = playerViewPos;
            Camera.main.transform.rotation = Quaternion.Euler(playerViewRot);
        }
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;

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

        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, targetPos, Time.deltaTime * transitionSpeed);
        Camera.main.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, Quaternion.Euler(targetRot), Time.deltaTime * transitionSpeed);
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
