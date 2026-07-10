using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

/// <summary>
/// Editor tool to inspect the King/General model's bone hierarchy and
/// identify the exact bone names for the right hand and sword mesh.
/// Run via: Tools/PRU/Inspect King Weapon Bones
/// Output saved to: inspect_king_weapon_output.txt (project root)
/// </summary>
public class InspectKingWeaponBones
{
    [MenuItem("Tools/PRU/Inspect King Weapon Bones")]
    public static void Inspect()
    {
        string[] pathsToCheck = new[]
        {
            "Assets/Models/NewModel/Model quân ta/Model_vua/model_vua_after_update.fbx",
            "Assets/Models/NewModel/Model quân ta/Model_vua/animation_idle_vua.fbx",
            "Assets/Models/NewModel/Model quân ta/Model_tuong_quan_ta/animation_tuong_quan_ta.fbx",
        };

        StringBuilder sb = new StringBuilder();

        foreach (string path in pathsToCheck)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                sb.AppendLine($"[NOT FOUND] {path}");
                continue;
            }

            sb.AppendLine($"\n=== Hierarchy: {path} ===");
            DumpHierarchy(prefab.transform, sb, "");

            // Print all Renderer names to spot the sword/weapon mesh
            sb.AppendLine($"\n--- Renderers in {System.IO.Path.GetFileName(path)} ---");
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
            {
                sb.AppendLine($"  Renderer: '{r.name}'  Type={r.GetType().Name}  Parent='{r.transform.parent?.name}'");
            }

            // Print all AnimationClips inside the FBX
            sb.AppendLine($"\n--- AnimationClips in {System.IO.Path.GetFileName(path)} ---");
            foreach (var sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            {
                if (sub is AnimationClip)
                {
                    sb.AppendLine($"  AnimationClip: '{sub.name}'");
                }
            }

            // Search for hand-related bones
            sb.AppendLine($"\n--- Bones with 'hand' or 'wrist' in name ---");
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            {
                string lower = t.name.ToLower();
                if (lower.Contains("hand") || lower.Contains("wrist") || lower.Contains("palm"))
                {
                    sb.AppendLine($"  '{t.name}'  worldPos={t.position}  parent='{t.parent?.name}'");
                }
            }
        }

        string outputPath = "inspect_king_weapon_output.txt";
        File.WriteAllText(outputPath, sb.ToString());
        Debug.Log($"[InspectKingWeaponBones] Output written to: {outputPath}\n\n{sb}");
        EditorUtility.RevealInFinder(outputPath);
    }

    private static void DumpHierarchy(Transform t, StringBuilder sb, string indent)
    {
        // Mark special nodes
        string tag = "";
        string lower = t.name.ToLower();
        if (lower.Contains("hand") || lower.Contains("wrist")) tag = " << HAND BONE";
        if (lower.Contains("sword") || lower.Contains("weapon") || lower.Contains("blade") || t.name.Contains("meshes[0].001")) tag = " << WEAPON MESH";

        sb.AppendLine($"{indent}{t.name}{tag}");

        for (int i = 0; i < t.childCount; i++)
        {
            DumpHierarchy(t.GetChild(i), sb, indent + "  ");
        }
    }
}
