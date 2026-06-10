using UnityEngine;

/// <summary>
/// DEPRECATED — This script has been replaced by CameraController.
/// Kept as an empty stub to avoid missing-script errors on existing scene objects.
/// </summary>
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class LockedCamera : MonoBehaviour
{
    [HideInInspector] public Vector3 position;
    [HideInInspector] public Vector3 rotation;
    [HideInInspector] public float fieldOfView = 60f;
    [HideInInspector] public Color skyColor = new Color(0.44f, 0.67f, 0.94f, 1f);
    [HideInInspector] public CameraClearFlags clearMode = CameraClearFlags.SolidColor;
    [HideInInspector] public bool lockPosition = false;
    [HideInInspector] public bool lockRotation = false;

    // Does nothing — CameraController handles everything now.
    void Awake()
    {
        // Self-destruct: remove this component at runtime
        Debug.Log("LockedCamera is deprecated. Removing self. Use CameraController instead.");
        Destroy(this);
    }
}
