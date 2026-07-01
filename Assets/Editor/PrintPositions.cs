using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class PrintPositions
{
    [MenuItem("Tools/Print Pad and Unit Positions")]
    public static void Print()
    {
        Debug.Log("--- Printing Positions ---");
        
        // Find all player pads
        GameObject playerGrid = GameObject.Find("PlayerGrid");
        if (playerGrid != null)
        {
            Debug.Log($"PlayerGrid position: {playerGrid.transform.position}");
            foreach (Transform pad in playerGrid.transform)
            {
                if (pad.name.StartsWith("PlayerPad_"))
                {
                    Debug.Log($"Pad {pad.name}: WorldPos={pad.transform.position}, LocalPos={pad.transform.localPosition}");
                }
            }
        }
        else
        {
            Debug.Log("PlayerGrid not found in scene!");
        }

        // Find all enemy pads
        GameObject enemyGrid = GameObject.Find("EnemyGrid");
        if (enemyGrid != null)
        {
            Debug.Log($"EnemyGrid position: {enemyGrid.transform.position}");
            foreach (Transform pad in enemyGrid.transform)
            {
                if (pad.name.StartsWith("EnemyPad_"))
                {
                    Debug.Log($"Pad {pad.name}: WorldPos={pad.transform.position}, LocalPos={pad.transform.localPosition}");
                }
            }
        }
        else
        {
            Debug.Log("EnemyGrid not found in scene!");
        }

        // Find all units
        Unit[] units = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (var unit in units)
        {
            Transform root = unit.transform;
            Transform graphics = root.Find("ClickPlacementPreviewCapsule");
            if (graphics == null)
            {
                // check children for graphics
                if (root.childCount > 0) graphics = root.GetChild(0);
            }

            Debug.Log($"Unit {root.name}: WorldPos={root.transform.position}, Rotation={root.transform.rotation.eulerAngles}");
            if (graphics != null)
            {
                Debug.Log($"  Graphics {graphics.name}: LocalPos={graphics.transform.localPosition}, WorldPos={graphics.transform.position}");
            }
        }
        Debug.Log("--- Inspecting Animator Controller ---");
        RuntimeAnimatorController rac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Animations/QuanTuongDichAnimatorController.controller");
        if (rac is AnimatorController ac)
        {
            foreach (var layer in ac.layers)
            {
                Debug.Log($"Layer: {layer.name}");
                foreach (var state in layer.stateMachine.states)
                {
                    Debug.Log($"  State: {state.state.name}, Motion: {(state.state.motion != null ? state.state.motion.name : "NULL")} (Type: {(state.state.motion != null ? state.state.motion.GetType().Name : "N/A")})");
                }
            }
        }
        else
        {
            Debug.Log("Animator Controller is NULL or not AnimatorController!");
        }
        Debug.Log("---------------------------");
    }
}
