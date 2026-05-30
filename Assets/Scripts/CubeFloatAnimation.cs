using UnityEngine;

/// <summary>
/// Animates the attached GameObject to float up and down while rotating 360°
/// horizontally over a configurable cycle duration.
/// 
/// Timeline (cycleDuration = 2s default):
///   0s  → default position, rotation = 0°
///   1s  → peak height, rotation = 180°
///   2s  → back to default, rotation = 360° (loops)
/// </summary>
public class CubeFloatAnimation : MonoBehaviour
{
    [Header("Float Settings")]
    [Tooltip("How many units the object rises at its peak.")]
    public float floatHeight = 2f;

    [Header("Timing")]
    [Tooltip("Duration of one full animation cycle in seconds.")]
    public float cycleDuration = 2f;

    private Vector3 startPosition;
    private float elapsed;

    void Start()
    {
        startPosition = transform.localPosition;
        elapsed = 0f;
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        // Normalized time within current cycle: 0.0 → 1.0
        float normalizedT = (elapsed % cycleDuration) / cycleDuration;

        // --- Position (Float up and down) ---
        // sin(π * t) gives 0 → 1 → 0 over one full cycle (smooth arc)
        float yOffset = Mathf.Sin(normalizedT * Mathf.PI) * floatHeight;
        transform.localPosition = new Vector3(
            startPosition.x,
            startPosition.y + yOffset,
            startPosition.z
        );

        // --- Rotation (Horizontal spin 360° per cycle around Y axis) ---
        float rotY = normalizedT * 360f;
        transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
    }
}
