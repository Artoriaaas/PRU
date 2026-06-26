using UnityEngine;
using System.Collections.Generic;

public enum GameState { Setup, Placement, Battle, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static string levelToLoadName = "";

    public GameState currentState = GameState.Setup;
    
    public List<Unit> playerUnits = new List<Unit>();
    public List<Unit> enemyUnits = new List<Unit>();

    public int maxPlayerUnits = 6;
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

        // Enforce minimum values for arrow flight in case they were serialized as 0 or extremely small in the scene
        if (arrowSpeed < 300f) arrowSpeed = 400f;
        if (arrowArcHeight < 1f) arrowArcHeight = 15f;

#if UNITY_EDITOR
        if (arrowPrefab == null)
        {
            arrowPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Cartoon_Weapon_Pack/Prefab/Arrow.prefab");
        }
#endif

        if (arrowPrefab != null)
        {
            // Convert prefab materials to URP once on startup to prevent runtime delay/magenta/green flashing
            Renderer[] prefabRends = arrowPrefab.GetComponentsInChildren<Renderer>(true);
            foreach (var r in prefabRends)
            {
                if (r.sharedMaterial != null)
                {
                    Shader urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
                    if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpShader == null) urpShader = Shader.Find("Standard");
                    
                    if (urpShader != null)
                    {
                        Texture mainTex = r.sharedMaterial.mainTexture;
                        r.sharedMaterial.shader = urpShader;
                        if (mainTex != null)
                        {
                            r.sharedMaterial.SetTexture("_BaseMap", mainTex);
                            r.sharedMaterial.SetTexture("_MainTex", mainTex);
                        }
                    }
                }
            }
        }
    }

    void Start()
    {
        if (SkillManager.Instance != null)
        {
            maxPlayerUnits = 6 + SkillManager.Instance.barracksLevel;
        }
        else
        {
            maxPlayerUnits = 6;
        }

        if (!string.IsNullOrEmpty(levelToLoadName))
        {
            activeLevel = Resources.Load<LevelData>("Levels/" + levelToLoadName);
            if (activeLevel == null)
            {
                Debug.LogError($"GameManager: Could not load LevelData '{levelToLoadName}' from Resources/Levels/!");
            }
        }

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
    public float modelScale = 15.0f;
    public float capsuleScale = 15f; // Scale up the capsules to be clearly visible
    public bool autoAlignBottom = true;
    [Tooltip("Drag the texture JPEG/PNG for ModelQuanLinh here. If left empty, it will auto-detect from .fbm folders in Editor.")]
    public Texture2D unitBaseColorTexture;

    [Header("Archer Model Settings")]
    [Tooltip("Drag your Archer FBX/Prefab here. If left empty, it will auto-load from Assets/Models/NewModel/animation_cung_quan_ta.fbx in Editor.")]
    public GameObject archerModelPrefab;
    public Vector3 archerRotationOffset = new Vector3(0f, 0f, 0f); // default to 0 for animation_ban_cung_quan_ta to face forward
    public Vector3 archerPositionOffset = new Vector3(0f, 0f, 0f);
    public float archerScale = 60.0f;
    [Tooltip("Drag the texture JPEG/PNG for Archer here. If left empty, it will auto-detect from .fbm folders in Editor.")]
    public Texture2D archerBaseColorTexture;
    [Tooltip("Assign your Animator Controller for the Archer here.")]
    public RuntimeAnimatorController archerAnimatorController;
    [Tooltip("Drag your Arrow prefab here. If left empty, it will auto-load from Assets/Cartoon_Weapon_Pack/Prefab/Arrow.prefab in Editor.")]
    public GameObject arrowPrefab;
    public float arrowSpeed = 400f;
    public float arrowArcHeight = 15f;
    public float archerAttackCooldown = 2.5f;

    [Header("Unit Templates")]
    [Tooltip("Drag the Unit GameObject from the scene or a prefab here to use as a template for player stats.")]
    public Unit playerUnitTemplate;
    [Tooltip("Drag the Unit GameObject from the scene or a prefab here to use as a template for enemy stats.")]
    public Unit enemyUnitTemplate;

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

    public void SpawnUnit(bool isPlayer, Vector3 position, int unitTypeIndex = 0)
    {
        GameObject rootObj = new GameObject(isPlayer ? ($"PlayerUnit_Type{unitTypeIndex}") : "EnemyUnit");
        rootObj.transform.position = position;

        bool isCapsule = forceCapsuleForTesting || (isPlayer && unitTypeIndex > 1);

        GameObject loadedModel = null;
#if UNITY_EDITOR
        if (!isCapsule)
        {
            if (isPlayer)
            {
                if (unitTypeIndex == 0)
                {
                    loadedModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân ta/Model_quan_ta.fbx");
                }
                else if (unitTypeIndex == 1)
                {
                    loadedModel = archerModelPrefab != null ? archerModelPrefab : UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/animation_ban_cung_quan_ta.fbx");
                }
            }
            else
            {
                if (unitTypeIndex == 0)
                {
                    loadedModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân địch/Trang_thai_cho_quan_dich.fbx");
                }
                else if (unitTypeIndex == 1)
                {
                    loadedModel = archerModelPrefab != null ? archerModelPrefab : UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/animation_ban_cung_quan_ta.fbx");
                }
            }
        }
#endif
        if (loadedModel == null)
        {
            loadedModel = (unitTypeIndex == 1) ? archerModelPrefab : unitModelPrefab;
        }

        GameObject graphicsObj = null;

        if (!isCapsule && loadedModel != null)
        {
            GameObject graphics = Instantiate(loadedModel, rootObj.transform);
            graphicsObj = graphics;
            graphics.transform.localPosition = Vector3.zero;
            
            // Choose offsets and scale based on unit type
            Vector3 rotationOffset = (unitTypeIndex == 1) ? archerRotationOffset : modelRotationOffset;
            Vector3 positionOffset = (unitTypeIndex == 1) ? archerPositionOffset : modelPositionOffset;
            float scaleVal = (unitTypeIndex == 1) ? archerScale : modelScale;
            RuntimeAnimatorController animController = (unitTypeIndex == 1) ? archerAnimatorController : unitAnimatorController;

            // Override prefab's local rotation with our offset to fix orientation
            graphics.transform.localRotation = Quaternion.Euler(rotationOffset);
            graphics.transform.localScale = new Vector3(scaleVal, scaleVal, scaleVal);

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
            if (animController != null)
            {
                animator.runtimeAnimatorController = animController;
            }

            if (unitTypeIndex == 1 && animator.runtimeAnimatorController != null)
            {
                animator.ResetTrigger("Attack");
                animator.ResetTrigger("Die");
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsAttacking", false);
                animator.SetBool("IsDead", false);
                animator.Play("Idle", 0, 0f);
                animator.Update(0f);
            }

            if (unitTypeIndex == 1)
            {
                Transform bowArmature = null;
                Transform bowBone = null;
                Transform leftHand = null;
                Transform[] childTransforms = graphics.GetComponentsInChildren<Transform>(true);
                foreach (var t in childTransforms)
                {
                    if (t.name == "Armature")
                    {
                        bowArmature = t;
                    }
                    else if (t.name == "mixamorig:LeftHand")
                    {
                        leftHand = t;
                    }
                }
                
                if (bowArmature != null)
                {
                    foreach (Transform child in bowArmature)
                    {
                        if (child.name == "Bone")
                        {
                            bowBone = child;
                            break;
                        }
                    }
                }
                
                if (bowArmature != null && leftHand != null)
                {
                    bowArmature.SetParent(leftHand, false);
                    Quaternion targetRot = Quaternion.Euler(333.1258f, 345.7652f, 5.40046f) * Quaternion.Euler(0f, 180f, 0f);
                    bowArmature.localRotation = targetRot;
                    
                    Vector3 boneLocalPos = Vector3.zero;
                    if (bowBone != null)
                    {
                        boneLocalPos = bowBone.localPosition;
                    }
                    bowArmature.localPosition = -(targetRot * boneLocalPos);
                    bowArmature.localScale = Vector3.one;
                }
            }

            // Setup textures dynamically to fix white character model issue
            Texture2D textureToApply = null;
#if UNITY_EDITOR
            if (unitTypeIndex == 1)
            {
                // For archer, do not fallback to unrelated folders like Model quân ta (animation_chay_dibo_doi).
                // Let it use its embedded/standard materials if no override texture is specified.
                textureToApply = archerBaseColorTexture;
            }
            else if (isPlayer)
            {
                textureToApply = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/NewModel/Model quân ta/model_quan_ta/tripo_convert_74080320-2742-4915-ab54-fe52dd1aaaa6.fbm/model_quan_ta_basecolor.JPEG");
            }
            else
            {
                textureToApply = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/NewModel/Model quân địch/medieval+knight+3d+model/tripo_convert_0e217662-40c2-483b-9f6e-5f6498668c72.fbm/medieval_knight_3d_model_basecolor.JPEG");
            }
#endif
            if (textureToApply == null && unitTypeIndex != 1)
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

            // Tint the enemy archer red-ish to differentiate from player archers
            if (!isPlayer && unitTypeIndex == 1)
            {
                var rends = graphics.GetComponentsInChildren<Renderer>();
                foreach (var r in rends)
                {
                    if (r.material != null)
                    {
                        Color tintColor = new Color(1f, 0.4f, 0.4f);
                        r.material.color = tintColor;
                        if (r.material.HasProperty("_BaseColor"))
                        {
                            r.material.SetColor("_BaseColor", tintColor);
                        }
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
            graphics.transform.localPosition += positionOffset;
        }
        else
        {
            isCapsule = true;
        }

        if (isCapsule)
        {
            GameObject graphics = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            graphicsObj = graphics;
            graphics.transform.SetParent(rootObj.transform);
            graphics.transform.localPosition = Vector3.up * capsuleScale;
            graphics.transform.localScale = new Vector3(capsuleScale * 0.8f, capsuleScale * 0.8f, capsuleScale * 0.8f);
            
            Renderer rend = graphics.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (rend.material.shader.name == "Hidden/InternalErrorShader")
                    rend.material = new Material(Shader.Find("Standard"));

                if (isPlayer)
                {
                    if (unitTypeIndex == 0) rend.material.color = Color.blue;
                    else if (unitTypeIndex == 1) rend.material.color = Color.green; // Archer
                    else if (unitTypeIndex == 2) rend.material.color = new Color(0.5f, 0f, 0.5f); // Cavalry
                    else if (unitTypeIndex == 3) rend.material.color = new Color(1f, 0.5f, 0f); // Elite
                }
                else
                {
                    rend.material.color = Color.red;
                }
            }

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

                // Shift the model's graphics transform to align its visual center with the parent's pivot (on the XZ plane)
                if (graphicsObj != null)
                {
                    graphicsObj.transform.localPosition -= new Vector3(localCenter.x, 0f, localCenter.z);
                }

                colHeight = localSize.y;
                // Center the collider on the XZ plane (0, 0) relative to the parent's pivot
                colCenter = new Vector3(0f, localCenter.y, 0f);
                
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
        unit.unitTypeIndex = unitTypeIndex;
        rootObj.AddComponent<ColliderVisualizer>();

        // Find fallback templates in the scene if not explicitly assigned
        Unit activeTemplate = isPlayer ? playerUnitTemplate : enemyUnitTemplate;
        if (activeTemplate == null)
        {
            if (isPlayer)
            {
                GameObject go = GameObject.Find("Unit");
                if (go != null)
                {
                    playerUnitTemplate = go.GetComponent<Unit>();
                    activeTemplate = playerUnitTemplate;
                }
            }
            else
            {
                GameObject go = GameObject.Find("EnemyUnitTemplate");
                if (go == null) go = GameObject.Find("EnemyUnit");
                if (go != null)
                {
                    enemyUnitTemplate = go.GetComponent<Unit>();
                    activeTemplate = enemyUnitTemplate;
                }
            }
        }

        // Copy stats from the active template or prefab if available
        Unit prefabUnit = activeTemplate;
        if (prefabUnit == null)
        {
            prefabUnit = loadedModel != null ? loadedModel.GetComponent<Unit>() : null;
            if (prefabUnit == null && loadedModel != null)
            {
                prefabUnit = loadedModel.GetComponentInChildren<Unit>();
            }
        }

        if (prefabUnit != null)
        {
            unit.hp = prefabUnit.hp;
            unit.maxHp = prefabUnit.maxHp;
            unit.atk = prefabUnit.atk;
            unit.def = prefabUnit.def;
            unit.speed = prefabUnit.speed;
            unit.attackRange = prefabUnit.attackRange;
            unit.attackCooldown = prefabUnit.attackCooldown;
            unit.animSpeedMultiplier = prefabUnit.animSpeedMultiplier;
        }

        if (unitTypeIndex == 1) // Archer
        {
            unit.attackCooldown = archerAttackCooldown;
        }

        if (isPlayer && SkillManager.Instance != null)
        {
            float buffMultiplier = 1f;
            int logLvl = SkillManager.Instance.logisticsLevel;
            if (logLvl == 1) buffMultiplier = 1.10f;
            else if (logLvl == 2) buffMultiplier = 1.20f;
            else if (logLvl == 3) buffMultiplier = 1.30f;

            unit.hp *= buffMultiplier;
            unit.maxHp *= buffMultiplier;
            unit.atk *= buffMultiplier;
            unit.def *= buffMultiplier;
        }

        // Scale speed and attack range dynamically to match the grid generator's spacing (70f is base spacing for scale 1.0)
        BattlefieldGridGenerator gridGen = Object.FindAnyObjectByType<BattlefieldGridGenerator>();
        if (gridGen != null)
        {
            float scale = gridGen.rowSpacing / 70f;
            unit.speed *= scale;
            // Scale range but ensure it exceeds physical contact distance
            float baseRange = Mathf.Max(unit.attackRange * scale, colRadius * 2.2f);
            unit.attackRange = (unitTypeIndex == 1) ? (baseRange * 5f) : baseRange;
        }
        else
        {
            // Fallback for runtime: scale speed and range relative to collider radius
            float scaleFactor = colRadius / 0.4f;
            unit.speed *= scaleFactor;
            float baseRange = Mathf.Max(unit.attackRange * scaleFactor, colRadius * 2.2f);
            unit.attackRange = (unitTypeIndex == 1) ? (baseRange * 5f) : baseRange;
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

        // Initialize RVO simulation
        if (RVOSimulatorManager.Instance != null)
        {
            RVOSimulatorManager.Instance.InitializeSimulation(playerUnits, enemyUnits);
        }

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

        if (RVOSimulatorManager.Instance != null)
        {
            RVOSimulatorManager.Instance.RemoveAgent(unit);
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
