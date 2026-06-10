using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SetupBattlefieldLayout
{
    [MenuItem("Tools/Setup Battlefield Layout")]
    public static void SetupLayout()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            camera = Object.FindAnyObjectByType<Camera>();
        }

        if (camera == null)
        {
            EditorUtility.DisplayDialog("Error", "No Camera found in the scene!", "OK");
            return;
        }

        // 1. Rebuild target parent GameObject
        string parentName = "BattlefieldLayout";
        GameObject existingParent = GameObject.Find(parentName);
        if (existingParent != null)
        {
            Undo.DestroyObjectImmediate(existingParent);
        }

        // Also clean up the temporary CameraSetupGrid if it exists
        GameObject oldGrid = GameObject.Find("CameraSetupGrid");
        if (oldGrid != null)
        {
            Undo.DestroyObjectImmediate(oldGrid);
        }

        GameObject layoutParent = new GameObject(parentName);
        Undo.RegisterCreatedObjectUndo(layoutParent, "Create Battlefield Layout");

        // 2. Define Plane y = 0 for projection
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        // 3. Calculate scale factor based on camera size to keep everything proportional
        float scaleFactor = 1.0f;
        if (camera.orthographic)
        {
            scaleFactor = camera.orthographicSize * 0.1f;
        }
        else
        {
            // For perspective, estimate based on viewport center projection
            Vector3 centerProj = ProjectViewportPoint(camera, new Vector3(0.5f, 0.4f, 0), groundPlane);
            float distance = Vector3.Distance(camera.transform.position, centerProj);
            scaleFactor = distance * 0.05f;
        }
        scaleFactor = Mathf.Max(scaleFactor, 1.0f);

        // 4. Create materials for player and enemy pads
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Battlefield"))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Battlefield");
        }

        Material playerPadMat = CreatePadMaterial("Assets/Materials/Battlefield/PlayerPadMat.mat", new Color(0f, 0.7f, 1f, 0.5f)); // Semi-transparent Cyan
        Material enemyPadMat = CreatePadMaterial("Assets/Materials/Battlefield/EnemyPadMat.mat", new Color(1f, 0.2f, 0.2f, 0.5f)); // Semi-transparent Red

        // 5. Generate Player Grid (Left side, 5 rows x 3 columns)
        GameObject playerGroup = new GameObject("PlayerGrid");
        playerGroup.transform.parent = layoutParent.transform;
        
        int rows = 5;
        int cols = 3;

        // Viewport boundaries for Player Grid
        float playerUMin = 0.12f;
        float playerUMax = 0.38f;
        float vMin = 0.18f;
        float vMax = 0.66f;

        float padRadius = scaleFactor * 0.4f; // 12.0 units diameter at scaleFactor=15
        Vector3 padScale = new Vector3(padRadius * 2f, 0.05f, padRadius * 2f);

        for (int r = 0; r < rows; r++)
        {
            float v = Mathf.Lerp(vMin, vMax, r / (float)(rows - 1));
            for (int c = 0; c < cols; c++)
            {
                float u = Mathf.Lerp(playerUMin, playerUMax, c / (float)(cols - 1));
                Vector3 pos = ProjectViewportPoint(camera, new Vector3(u, v, 0), groundPlane);
                
                // Offset Y slightly to avoid Z-fighting with the ground plane
                pos.y = 0.02f;

                GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = string.Format("PlayerPad_Row{0}_Col{1}", r, c);
                pad.transform.position = pos;
                pad.transform.localScale = padScale;
                pad.transform.parent = playerGroup.transform;

                // Remove cylinder collider so it doesn't block raycasts in game
                Object.DestroyImmediate(pad.GetComponent<Collider>());
                pad.GetComponent<Renderer>().sharedMaterial = playerPadMat;
            }
        }

        // 6. Generate Enemy Grid (Right side, 5 rows x 3 columns)
        GameObject enemyGroup = new GameObject("EnemyGrid");
        enemyGroup.transform.parent = layoutParent.transform;

        // Viewport boundaries for Enemy Grid
        float enemyUMin = 0.62f;
        float enemyUMax = 0.88f;

        for (int r = 0; r < rows; r++)
        {
            float v = Mathf.Lerp(vMin, vMax, r / (float)(rows - 1));
            for (int c = 0; c < cols; c++)
            {
                float u = Mathf.Lerp(enemyUMin, enemyUMax, c / (float)(cols - 1));
                Vector3 pos = ProjectViewportPoint(camera, new Vector3(u, v, 0), groundPlane);
                
                pos.y = 0.02f;

                GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = string.Format("EnemyPad_Row{0}_Col{1}", r, c);
                pad.transform.position = pos;
                pad.transform.localScale = padScale;
                pad.transform.parent = enemyGroup.transform;

                Object.DestroyImmediate(pad.GetComponent<Collider>());
                pad.GetComponent<Renderer>().sharedMaterial = enemyPadMat;
            }
        }

        // 7. Adjust parent position if needed (shifting to stay in front of background Quad if necessary)
        GameObject backgroundQuad = GameObject.Find("Quad");
        float wallZ = -40f;
        if (backgroundQuad != null)
        {
            wallZ = backgroundQuad.transform.position.z;
        }

        // Find the furthest back pad position (minimum Z)
        float minZ = float.MaxValue;
        foreach (Transform group in layoutParent.transform)
        {
            foreach (Transform pad in group)
            {
                if (pad.position.z < minZ)
                {
                    minZ = pad.position.z;
                }
            }
        }

        // Shift forward if furthest back pad is too close to or behind the wall
        float targetMinZ = wallZ + (padRadius * 1.5f);
        if (minZ < targetMinZ)
        {
            float shiftAmount = targetMinZ - minZ;
            layoutParent.transform.position += Vector3.forward * shiftAmount;
            Debug.Log(string.Format("Shifted battlefield layout forward by {0:F2} units to stay in front of the background wall.", shiftAmount));
        }

        Selection.activeGameObject = layoutParent;
        Debug.Log("2.5D Battlefield Layout initialized successfully (5x3 Player Grid + 5x3 Enemy Grid)!");
    }

    private static Vector3 ProjectViewportPoint(Camera cam, Vector3 viewportPos, Plane plane)
    {
        Ray ray = cam.ViewportPointToRay(viewportPos);
        float enter;
        if (plane.Raycast(ray, out enter))
        {
            return ray.GetPoint(enter);
        }
        return ray.GetPoint(50.0f);
    }

    private static Material CreatePadMaterial(string path, Color color)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Alpha blend
            mat.renderQueue = 3000;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.color = color;
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }
}
