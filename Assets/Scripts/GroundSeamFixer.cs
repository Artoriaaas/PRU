using UnityEngine;

/// <summary>
/// Eliminates visible seams between the three side-by-side ground plane meshes
/// (Plane_Player, Plane, Plane_Enemy) by slightly overlapping their boundaries
/// and staggering their Z depth so no edge gap is visible from any camera angle.
/// </summary>
[DefaultExecutionOrder(-200)]
public class GroundSeamFixer : MonoBehaviour
{
    [Tooltip("How many units each plane extends into its neighbor to hide seam gaps.")]
    public float overlapAmount = 3f;

    [Tooltip("Tiny Y drop per seam layer to prevent Z-fighting (very small value).")]
    public float layerYOffset = 0.002f;

    void Awake()
    {
        FixSeams();
    }

    public void FixSeams()
    {
        // Center plane stays at Y=0 (highest, rendered on top)
        FixPlane("Plane",        0,            0f);

        // Player side plane: lower by one layer, extend into center overlap
        FixPlane("Plane_Player", overlapAmount, -layerYOffset);

        // Enemy side plane: lower by two layers, extend into center overlap
        FixPlane("Plane_Enemy",  overlapAmount, -layerYOffset * 2f);
    }

    private void FixPlane(string planeName, float xOverlap, float yDelta)
    {
        GameObject go = GameObject.Find(planeName);
        if (go == null) return;

        Vector3 pos   = go.transform.position;
        Vector3 scale = go.transform.localScale;

        go.transform.position = new Vector3(pos.x, pos.y + yDelta, pos.z);

        if (xOverlap > 0f && scale.x > 0f)
        {
            // Unity built-in Plane mesh is 10 units wide at scale=1
            float scaleAdd = xOverlap / 10f;
            go.transform.localScale = new Vector3(scale.x + scaleAdd, scale.y, scale.z);
        }
    }
}
