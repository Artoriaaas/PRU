using UnityEngine;
using System.Collections.Generic;

public enum GameState { Setup, Placement, Battle, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState currentState = GameState.Setup;
    
    public List<Unit> playerUnits = new List<Unit>();
    public List<Unit> enemyUnits = new List<Unit>();

    public int maxPlayerUnits = 10;
    public int placedPlayerUnits = 0;
    public int maxEnemyUnits = 10;
    public int placedEnemyUnits = 0;

    [Header("Level Configuration")]
    public LevelData activeLevel;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }

        // Force forceCapsuleForTesting to false to override Unity Inspector's serialized value
        forceCapsuleForTesting = false;
    }

    void Start()
    {
        // Clean up any editor preview objects at runtime to avoid clutter
        CleanUpEditorPreviews();

        // Set initial visibility of the sub-grids
        GameObject playerGrid = GameObject.Find("PlayerGrid");
        GameObject enemyGrid = GameObject.Find("EnemyGrid");

        if (playerGrid != null) playerGrid.SetActive(true);
        if (enemyGrid != null) enemyGrid.SetActive(true);

        currentState = GameState.Placement;

        if (activeLevel != null)
        {
            LoadLevelEnemies();
        }
        else
        {
            Debug.LogWarning("GameManager: Active Level is not assigned! The game will fallback to generating random enemies when starting the battle. Please assign your LevelData asset to the 'Active Level' field of the GameManager component in the Inspector.");
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlacementUI();
        }
    }

    void CleanUpEditorPreviews()
    {
        GameObject enemyGrid = GameObject.Find("EnemyGrid");
        if (enemyGrid != null)
        {
            List<GameObject> previews = new List<GameObject>();
            foreach (Transform child in enemyGrid.transform)
            {
                if (child.name.StartsWith("EnemyUnit_Preview"))
                {
                    previews.Add(child.gameObject);
                }
            }
            foreach (var preview in previews)
            {
                Destroy(preview);
            }
            if (previews.Count > 0)
            {
                Debug.Log($"Cleaned up {previews.Count} leftover editor preview unit(s) at runtime.");
            }
        }
    }

    void InitializeRandomEnemies()
    {
        GameObject gridRoot = GameObject.Find("EnemyGrid");
        if (gridRoot != null)
        {
            List<Transform> enemyTiles = new List<Transform>();
            foreach (Transform child in gridRoot.transform)
            {
                if (child.name.StartsWith("EnemyPad_"))
                {
                    enemyTiles.Add(child);
                }
            }

            int numEnemies = Random.Range(5, 11);
            for (int i = 0; i < numEnemies; i++)
            {
                if (enemyTiles.Count == 0) break;
                
                int randIndex = Random.Range(0, enemyTiles.Count);
                Transform tile = enemyTiles[randIndex];
                enemyTiles.RemoveAt(randIndex);

                SpawnUnit(false, tile.position);
                placedEnemyUnits++;
            }
        }
        else
        {
            Debug.Log("EnemyGrid not found, skipping random enemy unit generation.");
        }
    }

    [Header("Model Settings")]
    [Tooltip("Drag your ModelQuanLinh FBX/Prefab here. If left empty, it will auto-load from Assets/Models/ModelQuanLinh.fbx in Editor.")]
    public GameObject unitModelPrefab;
    public Vector3 modelRotationOffset = new Vector3(0f, 0f, 0f); // default to 0 for ModelQuanLinh, user can adjust
    public Vector3 modelPositionOffset = new Vector3(0f, 0f, 0f);
    public float modelScale = 1.0f;
    public float capsuleScale = 15f; // Scale up the capsules to be clearly visible
    public bool autoAlignBottom = true;
    [Tooltip("Drag the texture JPEG/PNG for ModelQuanLinh here. If left empty, it will auto-detect from .fbm folders in Editor.")]
    public Texture2D unitBaseColorTexture;

    [Header("Animation Settings")]
    [Tooltip("Assign your Animator Controller for the ModelQuanLinh here.")]
    public RuntimeAnimatorController unitAnimatorController;

    [Header("Testing")]
    public bool forceCapsuleForTesting = false;

    public void LoadLevelEnemies()
    {
        if (activeLevel == null) return;

        GameObject enemyGrid = GameObject.Find("EnemyGrid");
        if (enemyGrid == null)
        {
            Debug.LogError("EnemyGrid not found in scene! Cannot load level enemies.");
            return;
        }

        // Clean up any existing enemy units in the scene
        foreach (var unit in new List<Unit>(enemyUnits))
        {
            if (unit != null) Destroy(unit.gameObject);
        }
        enemyUnits.Clear();
        placedEnemyUnits = 0;

        foreach (var placement in activeLevel.enemyPlacements)
        {
            string padName = $"EnemyPad_{placement.row}_{placement.column}";
            Transform pad = enemyGrid.transform.Find(padName);
            if (pad != null)
            {
                SpawnUnit(false, pad.position);
                placedEnemyUnits++;
            }
            else
            {
                Debug.LogWarning($"Level Load: Could not find pad '{padName}' under EnemyGrid to spawn enemy.");
            }
        }
        
        Debug.Log($"Loaded {placedEnemyUnits} enemy units from LevelData '{activeLevel.name}'");
    }

    public void SpawnUnit(bool isPlayer, Vector3 position)
    {
        GameObject rootObj = new GameObject(isPlayer ? "PlayerUnit" : "EnemyUnit");
        rootObj.transform.position = position;

        bool isCapsule = forceCapsuleForTesting;

        GameObject loadedModel = null;
#if UNITY_EDITOR
        if (!isCapsule)
        {
            if (isPlayer)
            {
                loadedModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân ta-20260616T082614Z-3-001/Model quân ta/Model_quan_ta.fbx");
            }
            else
            {
                loadedModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân địch-20260616T082614Z-3-001/Model quân địch/Trang_thai_cho_quan_dich.fbx");
            }
        }
#endif
        if (loadedModel == null)
        {
            loadedModel = unitModelPrefab;
        }

        if (!isCapsule && loadedModel != null)
        {
            GameObject graphics = Instantiate(loadedModel, rootObj.transform);
            graphics.transform.localPosition = Vector3.zero;
            // Override prefab's local rotation with our offset to fix face-planting
            graphics.transform.localRotation = Quaternion.Euler(modelRotationOffset);
            graphics.transform.localScale = new Vector3(modelScale, modelScale, modelScale);

            // Destroy all built-in child colliders on the imported model to prevent collision conflicts
            Collider[] modelColliders = graphics.GetComponentsInChildren<Collider>(true);
            foreach (var c in modelColliders)
            {
                Destroy(c);
            }

            // Setup animator controller
            Animator animator = graphics.GetComponent<Animator>();
            if (animator == null)
            {
                animator = graphics.AddComponent<Animator>();
            }
            animator.applyRootMotion = false; // Disable root motion to prevent tilting forward
            if (unitAnimatorController != null)
            {
                animator.runtimeAnimatorController = unitAnimatorController;
            }

            // Setup textures dynamically to fix white character model issue
            Texture2D textureToApply = null;
#if UNITY_EDITOR
            if (isPlayer)
            {
                textureToApply = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/NewModel/Model quân ta-20260616T082614Z-3-001/Model quân ta/model_quan_ta/tripo_convert_74080320-2742-4915-ab54-fe52dd1aaaa6.fbm/model_quan_ta_basecolor.JPEG");
            }
            else
            {
                textureToApply = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/NewModel/Model quân địch-20260616T082614Z-3-001/Model quân địch/medieval+knight+3d+model/tripo_convert_0e217662-40c2-483b-9f6e-5f6498668c72.fbm/medieval_knight_3d_model_basecolor.JPEG");
            }
#endif
            if (textureToApply == null)
            {
                textureToApply = unitBaseColorTexture;
            }
            if (textureToApply != null)
            {
                var rends = graphics.GetComponentsInChildren<Renderer>();
                foreach (var r in rends)
                {
                    if (r.material != null)
                    {
                        r.material.SetTexture("_BaseMap", textureToApply);
                        r.material.SetTexture("_MainTex", textureToApply);
                    }
                }
            }

            if (autoAlignBottom)
            {
                var renderers = graphics.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

                    float lowestY = b.min.y;
                    float offsetY = rootObj.transform.position.y - lowestY;
                    graphics.transform.position += new Vector3(0, offsetY, 0);
                }
            }
            
            // Apply manual offset for fine-tuning
            graphics.transform.localPosition += modelPositionOffset;
        }
        else
        {
            isCapsule = true;
        }

        if (isCapsule)
        {
            GameObject graphics = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            graphics.transform.SetParent(rootObj.transform);
            graphics.transform.localPosition = Vector3.up * capsuleScale;
            graphics.transform.localScale = new Vector3(capsuleScale * 0.8f, capsuleScale * 0.8f, capsuleScale * 0.8f);
            
            Renderer rend = graphics.GetComponent<Renderer>();
            if (rend != null) rend.material.color = isPlayer ? Color.blue : Color.red;

            Debug.Log($"[Diagnostic] Spawned Capsule Unit: rootPos={rootObj.transform.position}, graphicsLocalPos={graphics.transform.localPosition}, graphicsWorldPos={graphics.transform.position}");
        }

        Rigidbody rb = rootObj.AddComponent<Rigidbody>();
        // Freeze all rotations and Y position to prevent capsule climbing/floating
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.mass = 1f;
        rb.linearDamping = 1f;
        rb.isKinematic = true; // Kinematic during placement phase to prevent sliding/offsetting
        
        CapsuleCollider col = rootObj.AddComponent<CapsuleCollider>();
        float colHeight = 2f * capsuleScale;
        float colRadius = capsuleScale * 0.4f;
        Vector3 colCenter = new Vector3(0, capsuleScale, 0);

        if (!isCapsule)
        {
            var renderers = rootObj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                Vector3 localCenter = rootObj.transform.InverseTransformPoint(bounds.center);
                Vector3 localSize = rootObj.transform.InverseTransformVector(bounds.size);

                colHeight = localSize.y;
                colCenter = localCenter;
                // Avoid zero radius or extremely wide/thin capsules.
                // Avoid zero radius or extremely thin capsules, letting it scale naturally with bounds.
                float calculatedRadius = Mathf.Max(localSize.x, localSize.z) * 0.25f;
                colRadius = Mathf.Max(calculatedRadius, 0.35f);
            }
            else
            {
                colHeight = 2f;
                colCenter = new Vector3(0, 1f, 0);
                colRadius = 0.4f;
            }
        }

        col.height = colHeight;
        col.center = colCenter;
        col.radius = colRadius;
        col.isTrigger = true; // Use triggers to prevent physics stutters and allow smooth bypassing

        Unit unit = rootObj.AddComponent<Unit>();
        unit.isPlayer = isPlayer;

        // Scale speed and attack range dynamically to match the grid generator's spacing
        BattlefieldGridGenerator gridGen = Object.FindAnyObjectByType<BattlefieldGridGenerator>();
        if (gridGen != null)
        {
            unit.speed = gridGen.rowSpacing * 0.5f;
            // Ensure attackRange is larger than physical contact distance (colRadius * 2)
            unit.attackRange = Mathf.Max(gridGen.rowSpacing * 0.35f, colRadius * 2.2f);
        }
        else
        {
            // Fallback for runtime: scale speed and range relative to collider radius
            float scaleFactor = colRadius / 0.4f;
            unit.speed = 3f * scaleFactor;
            unit.attackRange = 1.5f * scaleFactor;
        }

        if (isPlayer)
        {
            playerUnits.Add(unit);
            // Rotate towards the enemy on the left (-X direction)
            rootObj.transform.rotation = Quaternion.Euler(0, 270, 0);
        }
        else
        {
            enemyUnits.Add(unit);
            // Rotate towards the player on the right (+X direction)
            rootObj.transform.rotation = Quaternion.Euler(0, 90, 0);
        }
    }

    public void StartBattle()
    {
        if (currentState != GameState.Placement) return;

        // If no enemy units were placed on the setup grid, generate random ones as fallback
        if (enemyUnits.Count == 0)
        {
            InitializeRandomEnemies();
        }

        // Manage grid visibilities for battle phase
        GameObject playerGrid = GameObject.Find("PlayerGrid");
        GameObject enemyGrid = GameObject.Find("EnemyGrid");

        if (playerGrid != null) playerGrid.SetActive(false);
        if (enemyGrid != null) enemyGrid.SetActive(false);

        currentState = GameState.Battle;

        // Make rigidbodies dynamic when battle starts
        foreach (var unit in playerUnits)
        {
            Rigidbody rb = unit.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
        }
        foreach (var unit in enemyUnits)
        {
            Rigidbody rb = unit.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
        }
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HidePlacementUI();
        }

        if (CameraController.Instance != null)
        {
            CameraController.Instance.SetView(CameraView.Battle);
        }
    }


    public void ReportDeath(Unit unit)
    {
        if (unit.isPlayer)
        {
            playerUnits.Remove(unit);
        }
        else
        {
            enemyUnits.Remove(unit);
        }

        CheckWinCondition();
    }

    void CheckWinCondition()
    {
        if (currentState != GameState.Battle) return;

        if (playerUnits.Count == 0)
        {
            currentState = GameState.GameOver;
            if (UIManager.Instance != null) UIManager.Instance.ShowGameOver(false);
        }
        else if (enemyUnits.Count == 0)
        {
            currentState = GameState.GameOver;
            if (UIManager.Instance != null) UIManager.Instance.ShowGameOver(true);
        }
    }
}
