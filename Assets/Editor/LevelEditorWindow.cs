using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class LevelEditorWindow : EditorWindow
{
    private LevelData _activeLevel;
    private float _capsuleScale = 15f;
    private Color _previewColor = new Color(1f, 0.2f, 0.2f, 0.6f);

    [MenuItem("Tools/PRU Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<LevelEditorWindow>("PRU Level Editor");
    }

    private void OnGUI()
    {
        GUILayout.Label("PRU Grid Level Editor", EditorStyles.boldLabel);
        
        GUILayout.Space(8);
        _activeLevel = (LevelData)EditorGUILayout.ObjectField("Active Level Data", _activeLevel, typeof(LevelData), false);

        if (_activeLevel == null)
        {
            EditorGUILayout.HelpBox("Please select or create a LevelData ScriptableObject asset to edit.", MessageType.Warning);
            if (GUILayout.Button("➕ Create New Level Data Asset"))
            {
                CreateNewLevelData();
            }
            return;
        }

        GUILayout.Space(12);
        GUILayout.Label("Level Editing Controls", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("📥 Load Level to Scene", GUILayout.Height(30)))
        {
            LoadLevelToScene();
        }
        if (GUILayout.Button("💾 Save Placement from Scene", GUILayout.Height(30)))
        {
            SavePlacementFromScene();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        if (GUILayout.Button("➕ Add New Preview Unit to Grid", GUILayout.Height(30)))
        {
            AddNewPreviewUnit();
        }

        GUILayout.Space(8);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("✖ Clear Previews", GUILayout.Height(24)))
        {
            ClearPreviews();
        }
        if (GUILayout.Button("📐 Snap Previews to Pads", GUILayout.Height(24)))
        {
            SnapPreviewsToPads();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(12);
        EditorGUILayout.HelpBox(
            "Instructions:\n" +
            "1. Click 'Load Level to Scene' to spawn temporary preview enemy units on the pads.\n" +
            "2. Duplicate, delete, or move these units in the Scene view to design your level layout.\n" +
            "3. Click 'Snap Previews to Pads' to snap any manually moved units onto their nearest pad center.\n" +
            "4. Click 'Save Placement from Scene' to write the design back into the LevelData asset.\n" +
            "5. Click 'Clear Previews' before running or exporting.",
            MessageType.Info
        );
    }

    private void CreateNewLevelData()
    {
        string dir = "Assets/Levels";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/NewLevelData.asset");
        LevelData asset = ScriptableObject.CreateInstance<LevelData>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        _activeLevel = asset;
        
        Debug.Log($"Created new LevelData asset at: {path}");
        Selection.activeObject = asset;
    }

    private void ClearPreviews()
    {
        GameObject enemyGrid = GameObject.Find("EnemyGrid");
        if (enemyGrid == null)
        {
            Debug.LogError("EnemyGrid not found in the scene! Cannot clear previews.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        // Destroy only child objects that represent enemy units
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in enemyGrid.transform)
        {
            if (child.name.StartsWith("EnemyUnit_Preview") || child.name.StartsWith("EnemyUnit"))
            {
                toDestroy.Add(child.gameObject);
            }
        }

        foreach (var obj in toDestroy)
        {
            Undo.DestroyObjectImmediate(obj);
        }

        Debug.Log($"Cleared {toDestroy.Count} preview enemy units from EnemyGrid.");
    }

    private void LoadLevelToScene()
    {
        if (_activeLevel == null) return;

        ClearPreviews();

        GameObject enemyGrid = GameObject.Find("EnemyGrid");
        if (enemyGrid == null)
        {
            Debug.LogError("EnemyGrid not found in the scene! Make sure the battlefield grid exists.");
            return;
        }

        int spawnedCount = 0;
        foreach (var placement in _activeLevel.enemyPlacements)
        {
            string padName = $"EnemyPad_{placement.row}_{placement.column}";
            Transform pad = enemyGrid.transform.Find(padName);
            if (pad != null)
            {
                // Instantiate a preview wrapper object matching standard Unit wrapper
                GameObject rootObj = new GameObject("EnemyUnit_Preview");
                Undo.RegisterCreatedObjectUndo(rootObj, "Load Enemy Preview");
                
                rootObj.transform.position = pad.position;
                rootObj.transform.rotation = Quaternion.Euler(0, 180, 0); // facing player
                rootObj.transform.SetParent(enemyGrid.transform);

                // Add visual capsule
                GameObject graphics = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                graphics.name = "Graphics";
                graphics.transform.SetParent(rootObj.transform);
                graphics.transform.localPosition = Vector3.up * _capsuleScale;
                graphics.transform.localScale = new Vector3(_capsuleScale * 0.8f, _capsuleScale * 0.8f, _capsuleScale * 0.8f);

                // Disable collider on the graphics child to prevent interference in editor
                var childCol = graphics.GetComponent<Collider>();
                if (childCol != null) DestroyImmediate(childCol);

                // Color red transparency for previews
                Renderer rend = graphics.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material previewMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    previewMat.color = _previewColor;
                    previewMat.SetFloat("_Surface", 1); // Transparent
                    previewMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    previewMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    previewMat.SetInt("_ZWrite", 0);
                    previewMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    rend.sharedMaterial = previewMat;
                }

                spawnedCount++;
            }
            else
            {
                Debug.LogWarning($"Could not find pad '{padName}' under EnemyGrid to place preview unit.");
            }
        }

        Debug.Log($"Loaded level: Spawned {spawnedCount} preview enemy units in EnemyGrid.");
    }

    private void SavePlacementFromScene()
    {
        if (_activeLevel == null) return;

        GameObject enemyGrid = GameObject.Find("EnemyGrid");
        if (enemyGrid == null)
        {
            Debug.LogError("EnemyGrid not found in the scene! Cannot save placement.");
            return;
        }

        // Collect all pad components for distance snapping
        List<Transform> pads = new List<Transform>();
        foreach (Transform child in enemyGrid.transform)
        {
            if (child.name.StartsWith("EnemyPad_"))
            {
                pads.Add(child);
            }
        }

        if (pads.Count == 0)
        {
            Debug.LogError("No EnemyPads found under EnemyGrid. Please generate the grid first!");
            return;
        }

        Undo.RecordObject(_activeLevel, "Save Level Placements");

        List<EnemyPlacement> newPlacements = new List<EnemyPlacement>();

        // Find all enemy units currently in the grid
        foreach (Transform child in enemyGrid.transform)
        {
            if (child.name.StartsWith("EnemyUnit_Preview") || child.name.StartsWith("EnemyUnit"))
            {
                // Find closest pad
                Transform closestPad = null;
                float minDist = float.MaxValue;
                foreach (var pad in pads)
                {
                    float dist = Vector2.Distance(new Vector2(child.position.x, child.position.z), new Vector2(pad.position.x, pad.position.z));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestPad = pad;
                    }
                }

                if (closestPad != null && minDist < 35f) // snap threshold (approx columnSpacing * 0.4)
                {
                    // Parse row/col from pad name "EnemyPad_r_c"
                    string[] parts = closestPad.name.Split('_');
                    if (parts.Length >= 3)
                    {
                        if (int.TryParse(parts[1], out int r) && int.TryParse(parts[2], out int c))
                        {
                            // Avoid duplicates on the same pad
                            bool duplicate = false;
                            foreach (var placement in newPlacements)
                            {
                                if (placement.row == r && placement.column == c)
                                {
                                    duplicate = true;
                                    break;
                                }
                            }

                            if (!duplicate)
                            {
                                newPlacements.Add(new EnemyPlacement { row = r, column = c });
                            }
                        }
                    }
                }
            }
        }

        _activeLevel.enemyPlacements = newPlacements;
        EditorUtility.SetDirty(_activeLevel);
        AssetDatabase.SaveAssets();

        Debug.Log($"Saved layout: {newPlacements.Count} enemy placements written to active LevelData asset.");
    }

    private void SnapPreviewsToPads()
    {
        GameObject enemyGrid = GameObject.Find("EnemyGrid");
        if (enemyGrid == null) return;

        List<Transform> pads = new List<Transform>();
        foreach (Transform child in enemyGrid.transform)
        {
            if (child.name.StartsWith("EnemyPad_"))
            {
                pads.Add(child);
            }
        }

        if (pads.Count == 0) return;

        int snapCount = 0;
        foreach (Transform child in enemyGrid.transform)
        {
            if (child.name.StartsWith("EnemyUnit_Preview") || child.name.StartsWith("EnemyUnit"))
            {
                Transform closestPad = null;
                float minDist = float.MaxValue;
                foreach (var pad in pads)
                {
                    float dist = Vector2.Distance(new Vector2(child.position.x, child.position.z), new Vector2(pad.position.x, pad.position.z));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestPad = pad;
                    }
                }

                if (closestPad != null)
                {
                    Undo.RecordObject(child, "Snap Unit to Pad");
                    child.position = closestPad.position;
                    snapCount++;
                }
            }
        }

        Debug.Log($"Snapped {snapCount} enemy preview units onto their closest pads.");
    }

    private void AddNewPreviewUnit()
    {
        GameObject enemyGrid = GameObject.Find("EnemyGrid");
        if (enemyGrid == null)
        {
            Debug.LogError("EnemyGrid not found in the scene! Cannot add preview unit.");
            return;
        }

        List<Transform> pads = new List<Transform>();
        foreach (Transform child in enemyGrid.transform)
        {
            if (child.name.StartsWith("EnemyPad_"))
            {
                pads.Add(child);
            }
        }

        if (pads.Count == 0)
        {
            Debug.LogError("No EnemyPads found. Please generate the grid first!");
            return;
        }

        // Find the first unoccupied pad
        Transform targetPad = null;
        foreach (var pad in pads)
        {
            bool occupied = false;
            foreach (Transform child in enemyGrid.transform)
            {
                if (child.name.StartsWith("EnemyUnit_Preview") || child.name.StartsWith("EnemyUnit"))
                {
                    if (Vector2.Distance(new Vector2(child.position.x, child.position.z), new Vector2(pad.position.x, pad.position.z)) < 5f)
                    {
                        occupied = true;
                        break;
                    }
                }
            }

            if (!occupied)
            {
                targetPad = pad;
                break;
            }
        }

        if (targetPad == null)
        {
            targetPad = pads[0]; // fallback
        }

        // Spawn visual preview wrapper
        GameObject rootObj = new GameObject("EnemyUnit_Preview");
        Undo.RegisterCreatedObjectUndo(rootObj, "Add Enemy Preview");
        
        rootObj.transform.position = targetPad.position;
        rootObj.transform.rotation = Quaternion.Euler(0, 180, 0);
        rootObj.transform.SetParent(enemyGrid.transform);

        // Add graphics
        GameObject graphics = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        graphics.name = "Graphics";
        graphics.transform.SetParent(rootObj.transform);
        graphics.transform.localPosition = Vector3.up * _capsuleScale;
        graphics.transform.localScale = new Vector3(_capsuleScale * 0.8f, _capsuleScale * 0.8f, _capsuleScale * 0.8f);

        var childCol = graphics.GetComponent<Collider>();
        if (childCol != null) DestroyImmediate(childCol);

        Renderer rend = graphics.GetComponent<Renderer>();
        if (rend != null)
        {
            Material previewMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            previewMat.color = _previewColor;
            previewMat.SetFloat("_Surface", 1);
            previewMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMat.SetInt("_ZWrite", 0);
            previewMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            rend.sharedMaterial = previewMat;
        }

        Selection.activeGameObject = rootObj;
        Debug.Log($"Added preview enemy unit at {targetPad.name}");
    }
}
