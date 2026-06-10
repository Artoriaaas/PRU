using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BattlefieldGridGenerator : MonoBehaviour
{
    [Header("Grid Dimensions")]
    [Range(1, 10)] public int rows = 4;
    [Range(1, 10)] public int columns = 4;

    [Header("Spacing (World Units)")]
    public float rowSpacing = 4.0f;
    public float columnSpacing = 4.0f;

    [Header("Pad Settings")]
    public float padRadius = 3.0f;
    public Color padColor = new Color(1.0f, 0.95f, 0.6f, 0.85f);
    public Texture2D padTexture;

    [Header("Material Settings")]
    public string materialPath = "Assets/Materials/Battlefield/PlayerPadMat.mat";

    private void Reset()
    {
#if UNITY_EDITOR
        padTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/MagicCircle.png");
#endif
    }

    public void ClearGrid()
    {
        // Destroy all children GameObjects (in editor, we must use DestroyImmediate)
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    public void GenerateGrid()
    {
        ClearGrid();

        // 1. Create/Update Material
        Material padMat = CreatePadMaterial(materialPath, padColor, padTexture);

        // 2. Calculate local start position to center the grid on the parent's pivot
        float halfWidth = ((columns - 1) * columnSpacing) / 2f;
        float halfDepth = ((rows - 1) * rowSpacing) / 2f;
        Vector3 localStart = new Vector3(-halfWidth, 0.03f, -halfDepth);

        // 3. Generate the Quad pads
        for (int r = 0; r < rows; r++)
        {
            float localZ = localStart.z + r * rowSpacing;
            for (int c = 0; c < columns; c++)
            {
                float localX = localStart.x + c * columnSpacing;
                Vector3 localPos = new Vector3(localX, 0.03f, localZ);

                GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                pad.name = string.Format("PlayerPad_{0}_{1}", r, c);
                pad.transform.SetParent(this.transform);
                pad.transform.localPosition = localPos;
                pad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                pad.transform.localScale = new Vector3(padRadius * 2f, padRadius * 2f, 1f);

                // Replace the thin single-sided MeshCollider with a BoxCollider
                // MeshCollider on Quad is single-sided and misses rays from the camera
                var meshCol = pad.GetComponent<MeshCollider>();
                if (meshCol != null)
                {
                    DestroyImmediate(meshCol);
                }
                BoxCollider boxCol = pad.AddComponent<BoxCollider>();
                boxCol.size = new Vector3(1f, 1f, 0.1f); // thin box in local space
                boxCol.isTrigger = true;

                var rend = pad.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.sharedMaterial = padMat;
                }
            }
        }

        Debug.Log(string.Format("Generated {0}x{1} battlefield grid successfully!", rows, columns));
    }

    private Material CreatePadMaterial(string path, Color color, Texture2D tex)
    {
#if UNITY_EDITOR
        // Ensure folder structure exists
        string dir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = false;
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            isNew = true;
        }
        else
        {
            if (mat.shader.name != "Universal Render Pipeline/Unlit")
            {
                mat.shader = Shader.Find("Universal Render Pipeline/Unlit");
            }
        }

        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0); // Alpha blend
        mat.renderQueue = 3000;

        // Setup URP transparent keywords and blend options
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType", "Transparent");

        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
        }

        if (isNew)
        {
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            EditorUtility.SetDirty(mat);
        }
        return mat;
#else
        Material runtimeMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        runtimeMat.color = color;
        if (tex != null) runtimeMat.SetTexture("_BaseMap", tex);
        return runtimeMat;
#endif
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BattlefieldGridGenerator))]
public class BattlefieldGridGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BattlefieldGridGenerator generator = (BattlefieldGridGenerator)target;

        GUILayout.Space(12);
        
        if (GUILayout.Button("▶  Generate Grid Now", GUILayout.Height(36)))
        {
            Undo.RecordObject(generator, "Generate Grid");
            generator.GenerateGrid();
            EditorUtility.SetDirty(generator);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }

        if (GUILayout.Button("✖  Clear Grid", GUILayout.Height(24)))
        {
            Undo.RecordObject(generator, "Clear Grid");
            generator.ClearGrid();
            EditorUtility.SetDirty(generator);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }
    }
}
#endif
