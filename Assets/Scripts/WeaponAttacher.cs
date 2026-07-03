using UnityEngine;

/// <summary>
/// Attaches a weapon bone to a hand bone every LateUpdate.
///
/// Problem context:
///   - The sword mesh (meshes[0].001) is parented to a standalone "Bone" in the FBX.
///   - "Bone" is NOT part of the Mixamo skeleton, so the Animator never moves it.
///   - WeaponAttacher snaps "Bone" to mixamorig:RightHand every LateUpdate.
///
/// Rotation strategy — DELTA approach:
///   At the first LateUpdate frame, we record the world-rotations of both Bone (weapon)
///   and RightHand. Each subsequent frame, we compute how much the hand has rotated since
///   that reference frame and apply the same delta to the sword's original orientation.
///   This preserves the original sword orientation (as modelled in the FBX) while making
///   it correctly rotate WITH the hand during all animations.
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

    [Tooltip("Additional rotation offset (euler angles) applied on top of the delta-rotated sword orientation. " +
             "Use this to fine-tune the sword grip angle in the Inspector.")]
    public Vector3 rotationOffset = Vector3.zero;

    // Captured at the first LateUpdate frame (after Animator has run)
    private Quaternion _initialBoneRot;
    private Quaternion _initialHandRot;
    private bool       _initialized = false;

    void LateUpdate()
    {
        if (weaponTransform == null || handBone == null) return;

        // Capture reference rotations on the very first frame so we know the
        // "natural" orientation of the sword relative to the hand at rest pose.
        if (!_initialized)
        {
            _initialBoneRot = weaponTransform.rotation;
            _initialHandRot = handBone.rotation;
            _initialized    = true;
        }

        // Compute how much the hand has rotated since the reference frame.
        Quaternion handDelta = handBone.rotation * Quaternion.Inverse(_initialHandRot);

        // Apply the same delta to the sword's original world rotation,
        // then add the user-tunable fine-tune offset on top.
        Quaternion targetRotation = handDelta * _initialBoneRot * Quaternion.Euler(rotationOffset);

        // Snap Bone's position to the hand bone (with optional local offset).
        Vector3 targetPosition = handBone.TransformPoint(positionOffset);

        weaponTransform.position = targetPosition;
        weaponTransform.rotation = targetRotation;
    }

    /// <summary>
    /// Forces re-capture of reference rotations on the next LateUpdate frame.
    /// Call this after changing animation state if the sword drifts.
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
