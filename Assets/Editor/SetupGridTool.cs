using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SetupGridTool
{
    [MenuItem("Tools/Create 2.5D Camera Setup Grid")]
    public static void CreateGrid()
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
        string parentName = "CameraSetupGrid";
        GameObject existingParent = GameObject.Find(parentName);
        if (existingParent != null)
        {
            Undo.DestroyObjectImmediate(existingParent);
        }

        GameObject gridParent = new GameObject(parentName);
        Undo.RegisterCreatedObjectUndo(gridParent, "Create Camera Setup Grid");

        // 2. Project viewport corners to Plane y = 0
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        // Compress viewport bounds to make the grid tighter and centered (0.25 to 0.75 u, 0.2 to 0.7 v)
        float uMin = 0.25f;
        float uMax = 0.75f;
        float vMin = 0.2f;
        float vMax = 0.7f;

        Vector3 pBL = ProjectViewportPoint(camera, new Vector3(uMin, vMin, 0), groundPlane);
        Vector3 pBR = ProjectViewportPoint(camera, new Vector3(uMax, vMin, 0), groundPlane);
        Vector3 pTL = ProjectViewportPoint(camera, new Vector3(uMin, vMax, 0), groundPlane);
        Vector3 pTR = ProjectViewportPoint(camera, new Vector3(uMax, vMax, 0), groundPlane);

        // Calculate automatic scale factor based on camera zoom/size
        float scaleFactor = 1.0f;
        if (camera.orthographic)
        {
            scaleFactor = camera.orthographicSize * 0.1f;
        }
        else
        {
            // For perspective, estimate based on average distance to grid points
            float avgDistance = Vector3.Distance(camera.transform.position, (pBL + pBR + pTL + pTR) * 0.25f);
            scaleFactor = avgDistance * 0.05f;
        }
        scaleFactor = Mathf.Max(scaleFactor, 1.0f);

        // 3. Draw Grid Lines (Cubes as line segments)
        int cols = 4;
        int rows = 3;

        // Create Materials folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Materials/GridSetup"))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "GridSetup");
        }

        // Create a grid line material (semi-transparent cyan/blue or subtle gray)
        Material lineMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GridSetup/GridLine.mat");
        if (lineMat == null)
        {
            lineMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            lineMat.color = new Color(0.2f, 0.6f, 1.0f, 0.8f); // Cyanish blue
            lineMat.SetFloat("_Surface", 1); // Transparent
            lineMat.SetFloat("_Blend", 0); // Alpha blend
            lineMat.renderQueue = 3000;
            AssetDatabase.CreateAsset(lineMat, "Assets/Materials/GridSetup/GridLine.mat");
        }

        float thickness = scaleFactor * 0.12f;
        float height = scaleFactor * 0.02f;

        // Draw horizontal grid lines
        for (int i = 0; i <= rows; i++)
        {
            float t = (float)i / rows;
            Vector3 left = Vector3.Lerp(pBL, pTL, t);
            Vector3 right = Vector3.Lerp(pBR, pTR, t);
            CreateLineSegment(left, right, "Row_" + i, gridParent, lineMat, thickness, height);
        }

        // Draw vertical grid lines
        for (int j = 0; j <= cols; j++)
        {
            float t = (float)j / cols;
            Vector3 bottom = Vector3.Lerp(pBL, pBR, t);
            Vector3 top = Vector3.Lerp(pTL, pTR, t);
            CreateLineSegment(bottom, top, "Col_" + j, gridParent, lineMat, thickness, height);
        }

        // 4. Create 4 capsules in the first row (closest to camera, i.e., index 0 to 1)
        string[] colorNames = { "ElectricPink", "NeonCyan", "VibrantOrange", "AcidLime" };
        Color[] colors = {
            new Color(1f, 0f, 0.5f),      // #FF007F
            new Color(0f, 0.95f, 1f),     // #00F3FF
            new Color(1f, 0.37f, 0f),     // #FF5E00
            new Color(0.63f, 1f, 0f)      // #A2FF00
        };

        // Enlarge capsules even more as requested (use 3.0x scale factor multiplier)
        float capsuleScaleMultiplier = 3.0f;
        float finalCapsuleScale = scaleFactor * capsuleScaleMultiplier;

        for (int j = 0; j < cols; j++)
        {
            // Cell center coordinates
            float u = (j + 0.5f) / cols;
            float v = 0.5f / rows; // Centered in the first row

            Vector3 capPos = ProjectViewportPoint(camera, new Vector3(u, v, 0), groundPlane);
            
            // Adjust capsule position so it stands on the plane (half-height offset)
            capPos.y = finalCapsuleScale;

            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Capsule_" + j + "_" + colorNames[j];
            capsule.transform.position = capPos;
            capsule.transform.localScale = Vector3.one * finalCapsuleScale;
            capsule.transform.parent = gridParent.transform;

            // Load or create material
            string matPath = "Assets/Materials/GridSetup/" + colorNames[j] + ".mat";
            Material capMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (capMat == null)
            {
                capMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                capMat.color = colors[j];
                capMat.SetFloat("_Metallic", 0.1f);
                capMat.SetFloat("_Smoothness", 0.7f); // Glossy
                AssetDatabase.CreateAsset(capMat, matPath);
            }
            capsule.GetComponent<Renderer>().sharedMaterial = capMat;
        }

        // Find the background Quad to get its Z coordinate
        GameObject backgroundQuad = GameObject.Find("Quad");
        float wallZ = -40f; // Default fallback
        if (backgroundQuad != null)
        {
            wallZ = backgroundQuad.transform.position.z;
        }

        // 5. Rotate the entire grid 90 degrees around its center to align capsules in depth
        Vector3 center = (pBL + pBR + pTL + pTR) * 0.25f;
        gridParent.transform.RotateAround(center, Vector3.up, 90f);

        // 6. Stagger the capsules horizontally (X-axis) so they partially overlap on the sides
        float staggerSpacing = finalCapsuleScale * 0.25f; // Slight stagger to see left/right occlusion edges
        int capIndex = 0;
        foreach (Transform child in gridParent.transform)
        {
            if (child.name.StartsWith("Capsule_"))
            {
                float horizontalOffset = (capIndex - 1.5f) * staggerSpacing;
                child.position += Vector3.right * horizontalOffset;
                capIndex++;
            }
        }

        // Calculate the furthest capsule along the camera's view depth (minimum Z)
        float minZ = float.MaxValue;
        foreach (Transform child in gridParent.transform)
        {
            if (child.name.StartsWith("Capsule_"))
            {
                if (child.position.z < minZ)
                {
                    minZ = child.position.z;
                }
            }
        }

        // Shift the entire grid forward (+Z) if the furthest capsule is behind the wall
        float targetMinZ = wallZ + (finalCapsuleScale * 1.2f); // Keep it safely in front of the wall
        if (minZ < targetMinZ)
        {
            float shiftAmount = targetMinZ - minZ;
            gridParent.transform.position += Vector3.forward * shiftAmount;
        }

        Selection.activeGameObject = gridParent;
        Debug.Log("2.5D Camera Setup Grid generated, scaled up, staggered, and rotated!");
    }

    private static Vector3 ProjectViewportPoint(Camera cam, Vector3 viewportPos, Plane plane)
    {
        Ray ray = cam.ViewportPointToRay(viewportPos);
        float enter;
        if (plane.Raycast(ray, out enter))
        {
            return ray.GetPoint(enter);
        }
        // Fallback along ray if no intersection
        return ray.GetPoint(50.0f);
    }

    private static void CreateLineSegment(Vector3 start, Vector3 end, string name, GameObject parent, Material mat, float thickness, float height)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = name;
        line.transform.parent = parent.transform;

        Vector3 dir = end - start;
        float length = dir.magnitude;

        line.transform.position = start + dir * 0.5f;
        line.transform.LookAt(end);

        // Grid lines are thin strips
        line.transform.localScale = new Vector3(thickness, height, length);

        // Remove the collider since it's just a helper line
        Object.DestroyImmediate(line.GetComponent<Collider>());

        line.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
