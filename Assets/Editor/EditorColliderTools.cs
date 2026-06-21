#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class EditorColliderTools
{
    [MenuItem("Tools/PRU/Toggle Hitboxes")]
    public static void ToggleHitboxes()
    {
        ColliderVisualizer.ShowColliders = !ColliderVisualizer.ShowColliders;
        Debug.Log("PRU Editor: Hitbox visibility toggled: " + (ColliderVisualizer.ShowColliders ? "ON" : "OFF"));
        
        // Force redraw of scene view
        UnityEditor.SceneView.RepaintAll();
    }
}
#endif
