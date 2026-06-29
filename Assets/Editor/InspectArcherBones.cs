using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

public class InspectArcherBones
{
    [MenuItem("Tools/PRU/Inspect Archer Bones")]
    public static void Inspect()
    {
        string path = "Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/animation_ban_cung_quan_ta.fbx";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError("Prefab not found at " + path);
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Hierarchy of prefab: " + path);
        DumpHierarchy(prefab.transform, sb, "");
        File.WriteAllText("inspect_archer_bones_output.txt", sb.ToString());
        Debug.Log("Dumped hierarchy to inspect_archer_bones_output.txt");
    }

    private static void DumpHierarchy(Transform t, StringBuilder sb, string indent)
    {
        sb.AppendLine($"{indent}- {t.name} (Type: {t.GetType().Name})");
        for (int i = 0; i < t.childCount; i++)
        {
            DumpHierarchy(t.GetChild(i), sb, indent + "  ");
        }
    }
}
