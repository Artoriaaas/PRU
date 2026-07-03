using UnityEngine;

/// <summary>
/// Attaches a weapon bone to a hand bone by parenting.
///
/// Strategy:
///   Parent the weapon "Bone" to the hand bone. Since AnimatorSetupTool already
///   stripped all "Bone" animation curves, the Animator won't fight the hierarchy —
///   the sword follows the hand perfectly through ALL animations (idle, walk,
///   attack, death) with zero drift.
///
///   The weapon's local origin is forced to the hand bone's origin (localPos=0),
///   and positionOffset/rotationOffset fine-tune the grip alignment.
///
/// Usage: Added automatically by GameManager.SpawnUnit() for the King/General unit.
/// </summary>
public class WeaponAttacher : MonoBehaviour
{
    [Tooltip("The weapon bone Transform to snap to the hand (typically the 'Bone' parent of the sword mesh).")]
    public Transform weaponTransform;

    [Tooltip("The hand bone Transform to follow (typically 'mixamorig:RightHand').")]
    public Transform handBone;

    [Tooltip("(Reserved for future use — grip-pivot offset mode.)")]
    public Transform gripPivot;

    [Tooltip("Additional position offset relative to the hand bone (in hand-bone local space).")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("Additional rotation offset (euler angles) applied on top of the weapon's local orientation. " +
             "Use this to fine-tune the sword grip angle in the Inspector.")]
    public Vector3 rotationOffset = Vector3.zero;

    [Header("Debug")]
    [Tooltip("Log transform data every N seconds (0 = disabled).")]
    public float debugLogInterval = 0f;

    // Base local transform captured after parenting
    private Quaternion _baseLocalRot;
    private bool       _initialized = false;
    private float      _nextDebugLog = 0f;

    void LateUpdate()
    {
        if (weaponTransform == null || handBone == null) return;

        if (!_initialized)
        {
            // Capture the weapon's world rotation before parenting so we can
            // compute the correct local rotation that preserves its visual orientation.
            Quaternion worldRot = weaponTransform.rotation;

            // Parent the weapon to the hand bone.
            // Since AnimatorSetupTool stripped all "Bone" animation curves,
            // the Animator will never override this hierarchy.
            weaponTransform.SetParent(handBone);

            // Force the weapon to the hand bone's origin (localPos = zero).
            // This matches the old behavior where position was always at the hand.
            // positionOffset then fine-tunes the grip position from there.
            weaponTransform.localPosition = Vector3.zero;

            // Compute the correct local rotation: the weapon's visual orientation
            // relative to the hand's current rotation. This preserves the FBX-designed
            // sword angle while making it follow the hand's rotation changes.
            _baseLocalRot = Quaternion.Inverse(handBone.rotation) * worldRot;
            weaponTransform.localRotation = _baseLocalRot * Quaternion.Euler(rotationOffset);

            _initialized = true;

            Debug.Log($"[WeaponAttacher] Parented '{weaponTransform.name}' to '{handBone.name}'. " +
                      $"baseLocalRot={_baseLocalRot.eulerAngles}, offset={positionOffset}, rotOffset={rotationOffset}");
        }

        // Keep position at hand origin + user offset every frame
        // (in case something else moves the weapon)
        weaponTransform.localPosition = positionOffset;
        weaponTransform.localRotation = _baseLocalRot * Quaternion.Euler(rotationOffset);

        // Optional debug logging
        if (debugLogInterval > 0f && Time.time >= _nextDebugLog)
        {
            _nextDebugLog = Time.time + debugLogInterval;
            Debug.Log($"[WeaponAttacher] '{weaponTransform.name}': " +
                      $"localPos={weaponTransform.localPosition}, localRot={weaponTransform.localRotation.eulerAngles}, " +
                      $"worldPos={weaponTransform.position}, handWorldPos={handBone.position}, " +
                      $"dist={Vector3.Distance(weaponTransform.position, handBone.position):F3}");
        }
    }

    /// <summary>
    /// Forces re-parenting and re-capture on the next LateUpdate frame.
    /// </summary>
    public void ResetReference()
    {
        _initialized = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (handBone != null)
        {
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.SphereHandleCap(0, handBone.position, handBone.rotation, 0.05f, EventType.Repaint);
            UnityEditor.Handles.Label(handBone.position + Vector3.up * 0.06f, $"Hand: {handBone.name}");
        }
        if (weaponTransform != null)
        {
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.SphereHandleCap(0, weaponTransform.position, weaponTransform.rotation, 0.04f, EventType.Repaint);
            UnityEditor.Handles.Label(weaponTransform.position + Vector3.up * 0.04f, $"Bone: {weaponTransform.name}");
        }
    }
#endif
}
