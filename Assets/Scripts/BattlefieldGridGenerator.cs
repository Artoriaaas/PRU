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

    [System.Serializable]
    public class GridZoneSettings
    {
        public string zoneName;
        
        [Header("References")]
        public GameObject gridParent;
        public GameObject planeObject;
        public GameObject quadObject;

        [Header("Positions (Local to BattlefieldLayout)")]
        public Vector3 gridLocalPosition;
        public Vector3 planeLocalPosition;
        public Vector3 quadLocalPosition;

        [Header("Scales")]
        public Vector3 planeScale = new Vector3(36f, 2.5f, 42.4f);
        public Vector3 quadScale = new Vector3(360f, 241.7f, 29.6f);

        [Header("Rotations")]
        public Vector3 planeRotation = Vector3.zero;
        public Vector3 quadRotation = new Vector3(0f, -180f, 0f);

        [Header("Material Overrides")]
        public Material planeMaterial;
        public Material quadMaterial;
    }

    [Header("Zone Configuration Settings")]
    public GridZoneSettings playerZone = new GridZoneSettings
    {
        zoneName = "Player Setup Zone",
        gridLocalPosition = new Vector3(710f, 0f, 0f),
        planeLocalPosition = new Vector3(710f, 0f, 43.5f),
        quadLocalPosition = new Vector3(720f, 118f, -157.5f),
        planeScale = new Vector3(85f, 2.5f, 42.4f),
        quadScale = new Vector3(850f, 241.7f, 100f)
    };

    public GridZoneSettings combatZone = new GridZoneSettings
    {
        zoneName = "Combat Screen Zone",
        gridLocalPosition = Vector3.zero,
        planeLocalPosition = new Vector3(0f, 0f, 43.5f),
        quadLocalPosition = new Vector3(0f, 118f, -157.5f),
        planeScale = new Vector3(56.7728f, 2.5f, 42.4f),
        quadScale = new Vector3(622.38043f, 241.7f, 29.6f)
    };

    public GridZoneSettings enemyZone = new GridZoneSettings
    {
        zoneName = "Enemy Setup Zone",
        gridLocalPosition = new Vector3(-710f, 0f, 0f),
        planeLocalPosition = new Vector3(-710f, 0f, 43.5f),
        quadLocalPosition = new Vector3(-720f, 118f, -157.5f),
        planeScale = new Vector3(85f, 2.5f, 42.4f),
        quadScale = new Vector3(850f, 241.7f, 29.6f)
    };

    [Header("Legacy References (Kept for compatibility)")]
    [HideInInspector] public GameObject planeCenter;
    [HideInInspector] public GameObject planePlayer;
    [HideInInspector] public GameObject planeEnemy;
    [HideInInspector] public GameObject quadCenter;
    [HideInInspector] public GameObject quadPlayer;
    [HideInInspector] public GameObject quadEnemy;

    [Header("Legacy Offsets & Sizes (Kept for compatibility)")]
    [HideInInspector] public float playerEnvXOffset = 360f;
    [HideInInspector] public float enemyEnvXOffset = -360f;
    [HideInInspector] public float centerEnvXOffset = 0f;
    [HideInInspector] public Vector3 planeScale = new Vector3(36f, 2.5f, 42.4f);
    [HideInInspector] public Vector3 quadScale = new Vector3(360f, 241.7f, 29.6f);
    [HideInInspector] public Material planeMaterial;
    [HideInInspector] public Material quadMaterial;

    private void Reset()
    {
#if UNITY_EDITOR
        padTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/MagicCircle.png");
#endif
    }

    public void ClearGrid()
    {
        // Clear children of PlayerGrid and EnemyGrid
        GameObject playerGrid = GameObject.Find("PlayerGrid");
        if (playerGrid != null)
        {
            while (playerGrid.transform.childCount > 0)
            {
                DestroyImmediate(playerGrid.transform.GetChild(0).gameObject);
            }
        }

        GameObject enemyGrid = GameObject.Find("EnemyGrid");
        if (enemyGrid != null)
        {
            while (enemyGrid.transform.childCount > 0)
            {
                DestroyImmediate(enemyGrid.transform.GetChild(0).gameObject);
            }
        }

        // Clean up any extra dynamic clones under the generator itself
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "PlayerGrid" || child.name == "EnemyGrid")
            {
                continue;
            }

            // Do not delete active planes/quads
            if (child.gameObject == playerZone.planeObject || child.gameObject == playerZone.quadObject ||
                child.gameObject == combatZone.planeObject || child.gameObject == combatZone.quadObject ||
                child.gameObject == enemyZone.planeObject || child.gameObject == enemyZone.quadObject)
            {
                continue;
            }

            // Also legacy reference check as fallback
            if (child.gameObject == planeCenter || child.gameObject == planePlayer || child.gameObject == planeEnemy ||
                child.gameObject == quadCenter || child.gameObject == quadPlayer || child.gameObject == quadEnemy)
            {
                continue;
            }

            DestroyImmediate(child.gameObject);
        }
    }

    public void FindOrCreateEnvironment()
    {
#if UNITY_EDITOR
        // 1. Auto-find objects if null
        if (combatZone.planeObject == null) combatZone.planeObject = GameObject.Find("Plane");
        if (playerZone.planeObject == null) playerZone.planeObject = GameObject.Find("Plane_Player");
        if (enemyZone.planeObject == null) enemyZone.planeObject = GameObject.Find("Plane_Enemy");
        
        if (combatZone.quadObject == null) combatZone.quadObject = GameObject.Find("Quad");
        if (playerZone.quadObject == null) playerZone.quadObject = GameObject.Find("Quad_Player");
        if (enemyZone.quadObject == null) enemyZone.quadObject = GameObject.Find("Quad_Enemy");

        playerZone.zoneName = "Player Setup Zone";
        combatZone.zoneName = "Combat Screen Zone";
        enemyZone.zoneName = "Enemy Setup Zone";

        // 2. Instantiate missing environment duplicates
        if (playerZone.planeObject == null && combatZone.planeObject != null)
        {
            playerZone.planeObject = Instantiate(combatZone.planeObject);
            playerZone.planeObject.name = "Plane_Player";
            Undo.RegisterCreatedObjectUndo(playerZone.planeObject, "Create Plane_Player");
        }
        if (enemyZone.planeObject == null && combatZone.planeObject != null)
        {
            enemyZone.planeObject = Instantiate(combatZone.planeObject);
            enemyZone.planeObject.name = "Plane_Enemy";
            Undo.RegisterCreatedObjectUndo(enemyZone.planeObject, "Create Plane_Enemy");
        }

        if (playerZone.quadObject == null && combatZone.quadObject != null)
        {
            playerZone.quadObject = Instantiate(combatZone.quadObject);
            playerZone.quadObject.name = "Quad_Player";
            Undo.RegisterCreatedObjectUndo(playerZone.quadObject, "Create Quad_Player");
        }
        if (enemyZone.quadObject == null && combatZone.quadObject != null)
        {
            enemyZone.quadObject = Instantiate(combatZone.quadObject);
            enemyZone.quadObject.name = "Quad_Enemy";
            Undo.RegisterCreatedObjectUndo(enemyZone.quadObject, "Create Quad_Enemy");
        }

        // Parent them to transform if they are not already
        ParentToLayout(playerZone.planeObject);
        ParentToLayout(playerZone.quadObject);
        ParentToLayout(combatZone.planeObject);
        ParentToLayout(combatZone.quadObject);
        ParentToLayout(enemyZone.planeObject);
        ParentToLayout(enemyZone.quadObject);

        // Keep legacy references in sync
        planeCenter = combatZone.planeObject;
        planePlayer = playerZone.planeObject;
        planeEnemy = enemyZone.planeObject;
        quadCenter = combatZone.quadObject;
        quadPlayer = playerZone.quadObject;
        quadEnemy = enemyZone.quadObject;
#endif
    }

    private void ParentToLayout(GameObject go)
    {
#if UNITY_EDITOR
        if (go != null && go.transform.parent != this.transform)
        {
            Undo.SetTransformParent(go.transform, this.transform, "Parent to Layout");
        }
#endif
    }

    public void CaptureFromScene()
    {
#if UNITY_EDITOR
        Undo.RecordObject(this, "Capture Layout From Scene");

        // Auto-find references if empty
        FindOrCreateEnvironment();

        // Capture settings for each zone
        CaptureZoneSettings(playerZone, "Player Setup Zone", "PlayerGrid", playerZone.planeObject, playerZone.quadObject, new Vector3(360f, 0f, 0f));
        CaptureZoneSettings(combatZone, "Combat Screen Zone", null, combatZone.planeObject, combatZone.quadObject, Vector3.zero);
        CaptureZoneSettings(enemyZone, "Enemy Setup Zone", "EnemyGrid", enemyZone.planeObject, enemyZone.quadObject, new Vector3(-360f, 0f, 0f));

        // Sync legacy fields
        playerEnvXOffset = playerZone.gridLocalPosition.x;
        enemyEnvXOffset = enemyZone.gridLocalPosition.x;
        centerEnvXOffset = combatZone.gridLocalPosition.x;

        EditorUtility.SetDirty(this);
        Debug.Log("Captured current environment layout and settings from the scene successfully!");
#endif
    }

    private void CaptureZoneSettings(GridZoneSettings zone, string name, string gridParentName, GameObject planeObj, GameObject quadObj, Vector3 defaultGridPos)
    {
#if UNITY_EDITOR
        zone.zoneName = name;
        
        if (gridParentName != null)
        {
            zone.gridParent = GameObject.Find(gridParentName);
        }
        else
        {
            zone.gridParent = null;
        }
        zone.planeObject = planeObj;
        zone.quadObject = quadObj;

        if (zone.gridParent != null)
        {
            zone.gridLocalPosition = zone.gridParent.transform.parent == this.transform 
                ? zone.gridParent.transform.localPosition 
                : this.transform.InverseTransformPoint(zone.gridParent.transform.position);
        }
        else
        {
            zone.gridLocalPosition = defaultGridPos;
        }

        if (zone.planeObject != null)
        {
            zone.planeLocalPosition = zone.planeObject.transform.parent == this.transform
                ? zone.planeObject.transform.localPosition
                : this.transform.InverseTransformPoint(zone.planeObject.transform.position);
            zone.planeScale = zone.planeObject.transform.localScale;
            zone.planeRotation = zone.planeObject.transform.localEulerAngles;
            var r = zone.planeObject.GetComponent<Renderer>();
            if (r != null) zone.planeMaterial = r.sharedMaterial;
        }
        else
        {
            zone.planeLocalPosition = defaultGridPos;
            zone.planeScale = new Vector3(36f, 2.5f, 42.4f);
            zone.planeRotation = Vector3.zero;
        }

        if (zone.quadObject != null)
        {
            zone.quadLocalPosition = zone.quadObject.transform.parent == this.transform
                ? zone.quadObject.transform.localPosition
                : this.transform.InverseTransformPoint(zone.quadObject.transform.position);
            zone.quadScale = zone.quadObject.transform.localScale;
            zone.quadRotation = zone.quadObject.transform.localEulerAngles;
            var r = zone.quadObject.GetComponent<Renderer>();
            if (r != null) zone.quadMaterial = r.sharedMaterial;
        }
        else
        {
            zone.quadLocalPosition = defaultGridPos + new Vector3(0f, 180f, -157.5f);
            zone.quadScale = new Vector3(360f, 241.7f, 29.6f);
            zone.quadRotation = new Vector3(0f, -180f, 0f);
        }
#endif
    }

    public void AlignEnvironment()
    {
#if UNITY_EDITOR
        FindOrCreateEnvironment();

        AlignZoneEnvironment(playerZone);
        AlignZoneEnvironment(combatZone);
        AlignZoneEnvironment(enemyZone);
#endif
    }

    private void AlignZoneEnvironment(GridZoneSettings zone)
    {
#if UNITY_EDITOR
        if (zone == null) return;

        if (zone.planeObject != null)
        {
            ConfigureTransform(
                zone.planeObject, 
                zone.planeLocalPosition, 
                zone.planeScale, 
                zone.planeRotation, 
                zone.planeMaterial
            );
        }

        if (zone.quadObject != null)
        {
            ConfigureTransform(
                zone.quadObject, 
                zone.quadLocalPosition, 
                zone.quadScale, 
                zone.quadRotation, 
                zone.quadMaterial
            );
        }
#endif
    }

    private void ConfigureTransform(GameObject go, Vector3 localPos, Vector3 scale, Vector3 rotation, Material mat)
    {
#if UNITY_EDITOR
        if (go == null) return;

        if (go.transform.parent != this.transform)
        {
            Undo.SetTransformParent(go.transform, this.transform, "Parent to Layout");
        }

        if (mat != null)
        {
            SetMaterial(go, mat);
        }

        Undo.RecordObject(go.transform, "Set Transform Properties");
        go.transform.localScale = scale;
        go.transform.localRotation = Quaternion.Euler(rotation);

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            Undo.RecordObject(rt, "Set RectTransform Positions");
            rt.anchoredPosition3D = localPos;
            rt.localPosition = localPos;
        }
        else
        {
            go.transform.localPosition = localPos;
        }
#endif
    }

    private void SetMaterial(GameObject go, Material mat)
    {
#if UNITY_EDITOR
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (mat == null) return; // Do not overwrite with null if slot is empty

            Undo.RecordObject(renderer, "Change Material");
            renderer.sharedMaterial = mat;
        }
#endif
    }

    private void ConfigureGridParent(GameObject go, Vector3 localPosition)
    {
#if UNITY_EDITOR
        Undo.RecordObject(go.transform, "Configure Grid Parent");
#endif
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
    }

    public void GenerateGrid()
    {
        ClearGrid();

        FindOrCreateEnvironment();

        // 1. Generate Player Setup Grid (rows x columns)
        if (playerZone.gridParent == null)
        {
            playerZone.gridParent = GameObject.Find("PlayerGrid");
            if (playerZone.gridParent == null)
            {
                playerZone.gridParent = new GameObject("PlayerGrid");
            }
        }
        if (playerZone.gridParent.transform.parent != this.transform)
        {
#if UNITY_EDITOR
            Undo.SetTransformParent(playerZone.gridParent.transform, this.transform, "Parent PlayerGrid");
#else
            playerZone.gridParent.transform.SetParent(this.transform, true);
#endif
        }
        ConfigureGridParent(playerZone.gridParent, playerZone.gridLocalPosition);
        GenerateSubGrid(playerZone.gridParent, rows, columns, "PlayerPad", materialPath, padColor, padTexture);

        // 2. Generate Enemy Setup Grid (rows x columns)
        if (enemyZone.gridParent == null)
        {
            enemyZone.gridParent = GameObject.Find("EnemyGrid");
            if (enemyZone.gridParent == null)
            {
                enemyZone.gridParent = new GameObject("EnemyGrid");
            }
        }
        if (enemyZone.gridParent.transform.parent != this.transform)
        {
#if UNITY_EDITOR
            Undo.SetTransformParent(enemyZone.gridParent.transform, this.transform, "Parent EnemyGrid");
#else
            enemyZone.gridParent.transform.SetParent(this.transform, true);
#endif
        }
        ConfigureGridParent(enemyZone.gridParent, enemyZone.gridLocalPosition);
        Color enemyColor = new Color(1f, 0.3f, 0.3f, 0.85f); // Semi-transparent Red
        string enemyMatPath = "Assets/Materials/Battlefield/EnemyPadMat.mat";
        GenerateSubGrid(enemyZone.gridParent, rows, columns, "EnemyPad", enemyMatPath, enemyColor, padTexture);

        // 3. Align environment elements to the configured offsets
        AlignEnvironment();

        // Keep legacy properties in sync for safety
        playerEnvXOffset = playerZone.gridLocalPosition.x;
        enemyEnvXOffset = enemyZone.gridLocalPosition.x;
        centerEnvXOffset = combatZone.gridLocalPosition.x;

        Debug.Log(string.Format("Generated Player Setup ({0}x{1}) and Enemy Setup ({0}x{1}) grids successfully!", rows, columns));
    }

    private void GenerateSubGrid(GameObject parent, int subRows, int subCols, string prefix, string matPath, Color color, Texture2D tex)
    {
        Material padMat = CreatePadMaterial(matPath, color, tex);

        float halfWidth = ((subCols - 1) * columnSpacing) / 2f;
        float halfDepth = ((subRows - 1) * rowSpacing) / 2f;
        Vector3 localStart = new Vector3(-halfWidth, 0.03f, -halfDepth);

        for (int r = 0; r < subRows; r++)
        {
            float localZ = localStart.z + r * rowSpacing;
            for (int c = 0; c < subCols; c++)
            {
                float localX = localStart.x + c * columnSpacing;
                Vector3 localPos = new Vector3(localX, 0.03f, localZ);

                GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                pad.name = string.Format("{0}_{1}_{2}", prefix, r, c);
                pad.transform.SetParent(parent.transform);
                pad.transform.localPosition = localPos;
                pad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                pad.transform.localScale = new Vector3(padRadius * 2f, padRadius * 2f, 1f);

                // Replace the thin single-sided MeshCollider with a BoxCollider
                var meshCol = pad.GetComponent<MeshCollider>();
                if (meshCol != null)
                {
                    DestroyImmediate(meshCol);
                }
                BoxCollider boxCol = pad.AddComponent<BoxCollider>();
                boxCol.size = new Vector3(1f, 1f, 0.1f);
                boxCol.isTrigger = true;

                var rend = pad.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.sharedMaterial = padMat;
                }
            }
        }
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
        BattlefieldGridGenerator generator = (BattlefieldGridGenerator)target;

        serializedObject.Update();

        // 1. Grid Parameters
        GUILayout.Label("Grid Dimension & Spacing Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rows"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("columns"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rowSpacing"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("columnSpacing"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("padRadius"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("padColor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("padTexture"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("materialPath"));

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Configure Planes, Quads, and Grids individually for each of the 3 zones below.\n" +
            "Tip: Click 'Capture Settings From Current Scene' to load positions directly from your scene before editing.",
            MessageType.Info
        );

        GUILayout.Space(8);

        if (GUILayout.Button("📥  Capture Settings From Current Scene", GUILayout.Height(30)))
        {
            Undo.RecordObject(generator, "Capture Settings From Current Scene");
            generator.CaptureFromScene();
            EditorUtility.SetDirty(generator);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }

        GUILayout.Space(12);

        // 2. Compact drawing of the zones
        DrawZoneProperties(serializedObject.FindProperty("playerZone"), false);
        DrawZoneProperties(serializedObject.FindProperty("combatZone"), true);
        DrawZoneProperties(serializedObject.FindProperty("enemyZone"), false);

        GUILayout.Space(12);

        GUILayout.Label("Action Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("▶  Generate Grid Now", GUILayout.Height(36)))
        {
            Undo.RecordObject(generator, "Generate Grid");
            generator.GenerateGrid();
            EditorUtility.SetDirty(generator);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }

        if (GUILayout.Button("✖  Clear Grid (Pads Only)", GUILayout.Height(24)))
        {
            Undo.RecordObject(generator, "Clear Grid");
            generator.ClearGrid();
            EditorUtility.SetDirty(generator);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }

        GUILayout.Space(6);

        if (GUILayout.Button("🔍  Find/Link Environment Objects", GUILayout.Height(24)))
        {
            Undo.RecordObject(generator, "Find Environment Objects");
            generator.FindOrCreateEnvironment();
            EditorUtility.SetDirty(generator);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }

        if (GUILayout.Button("📐  Align Environment Planes & Quads", GUILayout.Height(24)))
        {
            Undo.RecordObject(generator, "Align Environment");
            generator.AlignEnvironment();
            EditorUtility.SetDirty(generator);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawZoneProperties(SerializedProperty zoneProp, bool isCombat)
    {
        SerializedProperty nameProp = zoneProp.FindPropertyRelative("zoneName");
        SerializedProperty gridParentProp = zoneProp.FindPropertyRelative("gridParent");
        SerializedProperty planeObjectProp = zoneProp.FindPropertyRelative("planeObject");
        SerializedProperty quadObjectProp = zoneProp.FindPropertyRelative("quadObject");
        
        SerializedProperty gridPosProp = zoneProp.FindPropertyRelative("gridLocalPosition");
        
        SerializedProperty planePosProp = zoneProp.FindPropertyRelative("planeLocalPosition");
        SerializedProperty planeScaleProp = zoneProp.FindPropertyRelative("planeScale");
        SerializedProperty planeRotProp = zoneProp.FindPropertyRelative("planeRotation");
        SerializedProperty planeMatProp = zoneProp.FindPropertyRelative("planeMaterial");

        SerializedProperty quadPosProp = zoneProp.FindPropertyRelative("quadLocalPosition");
        SerializedProperty quadScaleProp = zoneProp.FindPropertyRelative("quadScale");
        SerializedProperty quadRotProp = zoneProp.FindPropertyRelative("quadRotation");
        SerializedProperty quadMatProp = zoneProp.FindPropertyRelative("quadMaterial");

        string title = nameProp.stringValue;
        if (string.IsNullOrEmpty(title)) title = zoneProp.displayName;

        // Visual divider/header for each zone
        GUILayout.Space(8);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        
        // Use a nice background box style for clean grouping
        GUILayout.BeginVertical(EditorStyles.helpBox);
        
        // References (Side-by-side or standard)
        EditorGUILayout.LabelField("References", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(planeObjectProp, new GUIContent("Plane Object"));
        EditorGUILayout.PropertyField(quadObjectProp, new GUIContent("Quad Object"));
        if (!isCombat)
        {
            EditorGUILayout.PropertyField(gridParentProp, new GUIContent("Grid Parent"));
        }

        GUILayout.Space(4);

        // Grid Local Position
        if (!isCombat)
        {
            EditorGUILayout.LabelField("Grid Setup Position", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(gridPosProp, new GUIContent("Position"));
            GUILayout.Space(4);
        }

        // Plane properties
        EditorGUILayout.LabelField("Plane Properties", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(planePosProp, new GUIContent("Position"));
        EditorGUILayout.PropertyField(planeRotProp, new GUIContent("Rotation"));
        EditorGUILayout.PropertyField(planeScaleProp, new GUIContent("Scale"));
        EditorGUILayout.PropertyField(planeMatProp, new GUIContent("Material Override"));

        GUILayout.Space(4);

        // Quad properties
        EditorGUILayout.LabelField("Quad Properties", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(quadPosProp, new GUIContent("Position"));
        EditorGUILayout.PropertyField(quadRotProp, new GUIContent("Rotation"));
        EditorGUILayout.PropertyField(quadScaleProp, new GUIContent("Scale"));
        EditorGUILayout.PropertyField(quadMatProp, new GUIContent("Material Override"));

        GUILayout.EndVertical();
    }
}
#endif
