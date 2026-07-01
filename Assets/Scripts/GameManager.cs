using UnityEngine;
using System.Collections.Generic;

public enum GameState { Setup, Placement, Battle, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static string levelToLoadName = "";
    public static string activeCastleName = "";

    public GameState currentState = GameState.Setup;
    [HideInInspector] public bool isLevelEditorMode = false;
    
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

        // Force general model scale (+20%) and position offset corrections to center it
        if (kingScale == 60.0f)
        {
            kingScale = 72.0f;
        }
        if (kingPositionOffset == Vector3.zero)
        {
            kingPositionOffset = new Vector3(-0.5f, 0f, 0f);
        }

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
        BGMManager.Instance.PlayMusic("Audio/PrepareTheme", true);

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
    [Tooltip("Drag your Ho Bon Quan FBX/Prefab here. If left empty, it will auto-load from Assets in Editor.")]
    public GameObject hoBonQuanPrefab;
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

    [Header("Enemy Model Settings")]
    [Tooltip("Drag your Enemy ModelQuanLinh FBX/Prefab here. If left empty, it will auto-load from Assets/Models/NewModel/Model quân địch/Trang_thai_cho_quan_dich.fbx in Editor.")]
    public GameObject enemyUnitModelPrefab;
    [Tooltip("Drag your Enemy Archer FBX/Prefab here. If left empty, it will auto-load from Assets/Models/NewModel/Model quân địch/model_quan_cung/animation_ban_cung_quan_dich.fbx in Editor.")]
    public GameObject enemyArcherModelPrefab;
    [Tooltip("Drag your Arrow prefab here. If left empty, it will auto-load from Assets/Cartoon_Weapon_Pack/Prefab/Arrow.prefab in Editor.")]
    public GameObject arrowPrefab;
    public float arrowSpeed = 400f;
    public float arrowArcHeight = 15f;

    [Header("King Model Settings")]
    [Tooltip("Drag your King FBX/Prefab here. If left empty, it will auto-load from Assets/Models/NewModel/Model quân ta/Model_vua/model_vua.fbx in Editor.")]
    public GameObject kingModelPrefab;
    public Vector3 kingRotationOffset = new Vector3(0f, 90f, 0f);
    public Vector3 kingPositionOffset = new Vector3(-0.5f, 0f, 0f);
    public float kingScale = 72.0f;
    [Tooltip("Assign your Animator Controller for the King here.")]
    public RuntimeAnimatorController kingAnimatorController;

    [Header("Enemy King/General Model Settings")]
    [Tooltip("Drag your Enemy General FBX/Prefab here. If left empty, it will auto-load from Assets/Models/NewModel/Model quân địch/model_tuong_quan_dich/animation_tuong_quan_dich.fbx in Editor.")]
    public GameObject enemyKingModelPrefab;
    public Vector3 enemyKingRotationOffset = new Vector3(0f, -90f, 0f); // Symmetrical to player king (facing opposite direction)
    public Vector3 enemyKingPositionOffset = new Vector3(0.5f, 0f, 0f); // Symmetrical to player king
    public float enemyKingScale = 72.0f;
    [Tooltip("Assign your Animator Controller for the Enemy General here.")]
    public RuntimeAnimatorController enemyKingAnimatorController;

    [Header("Unit Templates")]
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
            if (pad == null && padName == "EnemyPad_3_2")
            {
                pad = enemyGrid.transform.Find("EnemyPad_3_2 (1)");
            }
            if (pad != null)
            {
                SpawnUnit(false, pad.position, placement.unitTypeIndex);
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

        bool isCapsule = forceCapsuleForTesting || (isPlayer && unitTypeIndex > 1 && unitTypeIndex != 4);

        GameObject loadedModel = null;
#if UNITY_EDITOR
        if (!isCapsule)
        {
            if (isPlayer)
            {
                if (unitTypeIndex == 0)
                {
                    if (SkillManager.Instance != null && SkillManager.Instance.troopLevel >= 2)
                    {
                        loadedModel = hoBonQuanPrefab != null ? hoBonQuanPrefab : UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân ta/model_ho_bon_quan/animation_ho_bon_quan.fbx");
                    }
                    else
                    {
                        loadedModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân ta/Model_quan_ta.fbx");
                    }
                }
                else if (unitTypeIndex == 1)
                {
                    loadedModel = archerModelPrefab != null ? archerModelPrefab : UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/animation_ban_cung_quan_ta.fbx");
                }
                else if (unitTypeIndex == 4)
                {
                    loadedModel = kingModelPrefab != null ? kingModelPrefab : UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân ta/Model_vua/model_vua_after_update.fbx");
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
                    loadedModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân địch/model_quan_cung/animation_ban_cung_quan_dich.fbx");
                }
                else if (unitTypeIndex == 4)
                {
                    loadedModel = enemyKingModelPrefab != null ? enemyKingModelPrefab : UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân địch/model_tuong_quan_dich/animation_tuong_quan_dich.fbx");
                }
            }
        }
#else
        // Build path: use serialized prefab references directly
        if (!isCapsule)
        {
            if (isPlayer)
            {
                if (unitTypeIndex == 0)
                {
                    if (SkillManager.Instance != null && SkillManager.Instance.troopLevel >= 2)
                        loadedModel = hoBonQuanPrefab;
                    else
                        loadedModel = unitModelPrefab;
                }
                else if (unitTypeIndex == 1)
                    loadedModel = archerModelPrefab;
                else if (unitTypeIndex == 4)
                    loadedModel = kingModelPrefab;
            }
            else
            {
                if (unitTypeIndex == 0)
                    loadedModel = enemyUnitModelPrefab;
                else if (unitTypeIndex == 1)
                    loadedModel = enemyArcherModelPrefab;
                else if (unitTypeIndex == 4)
                    loadedModel = enemyKingModelPrefab;
            }
        }
#endif
        if (loadedModel == null)
        {
            if (unitTypeIndex == 1) loadedModel = isPlayer ? archerModelPrefab : enemyArcherModelPrefab;
            else if (unitTypeIndex == 4) loadedModel = isPlayer ? kingModelPrefab : enemyKingModelPrefab;
            else loadedModel = isPlayer ? unitModelPrefab : enemyUnitModelPrefab;
        }

        GameObject graphicsObj = null;

        if (!isCapsule && loadedModel != null)
        {
            GameObject graphics = Instantiate(loadedModel, rootObj.transform);
            graphicsObj = graphics;
            
            // Choose offsets and scale based on unit type
            Vector3 rotationOffset = modelRotationOffset;
            Vector3 positionOffset = modelPositionOffset;
            float scaleVal = modelScale;
            RuntimeAnimatorController animController = unitAnimatorController;

            if (unitTypeIndex == 1)
            {
                rotationOffset = archerRotationOffset;
                positionOffset = archerPositionOffset;
                scaleVal = archerScale;
                animController = archerAnimatorController;
            }
            else if (unitTypeIndex == 4)
            {
                if (isPlayer)
                {
                    rotationOffset = kingRotationOffset;
                    positionOffset = kingPositionOffset;
                    scaleVal = kingScale;
                    animController = kingAnimatorController;
                }
                else
                {
                    rotationOffset = enemyKingRotationOffset;
                    positionOffset = enemyKingPositionOffset;
                    scaleVal = enemyKingScale;
                    animController = enemyKingAnimatorController;
#if UNITY_EDITOR
                    if (animController == null)
                    {
                        animController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Animations/QuanTuongDichAnimatorController.controller");
                    }
#endif
                    if (animController == null)
                    {
                        animController = kingAnimatorController;
                    }
                }
            }

            // Override prefab's local rotation with our offset to fix orientation
            graphics.transform.localPosition = positionOffset;
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

            if ((unitTypeIndex == 1 || unitTypeIndex == 4) && animator.runtimeAnimatorController != null)
            {
                animator.ResetTrigger("Attack");
                animator.ResetTrigger("Die");
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsAttacking", false);
                animator.SetBool("IsDead", false);
                animator.Play("Idle", 0, 0f);
                animator.Update(0f);
            }

            // The new archer model has the bow and arrow fully integrated into the FBX's skeletal hierarchy (under mixamorig:LeftHand/Bow_root and mixamorig:RightHand/Bone).
            // The bow itself is a SkinnedMeshRenderer, so runtime manual parenting is no longer necessary.
            /*
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
            */

            // Setup textures dynamically to fix white character model issue
            Texture2D textureToApply = null;
#if UNITY_EDITOR
            if (unitTypeIndex == 1)
            {
                // For archer, do not fallback to unrelated folders like Model quân ta (animation_chay_dibo_doi).
                // Let it use its embedded/standard materials if no override texture is specified.
                textureToApply = archerBaseColorTexture;
            }
            else if (unitTypeIndex == 4 && isPlayer)
            {
                // King model has its own dedicated texture
                textureToApply = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/NewModel/Model quân ta/Model_vua/Textures/model_vua_basecolor.jpg");
            }
            else if (unitTypeIndex == 4 && !isPlayer)
            {
                // Enemy general uses automatically mapped extracted textures, do not overwrite!
                textureToApply = null;
            }
            else if (isPlayer)
            {
                if (unitTypeIndex == 0 && SkillManager.Instance != null && SkillManager.Instance.troopLevel >= 2)
                {
                    textureToApply = null; // Use embedded materials for Ho Bon Quan
                }
                else
                {
                    textureToApply = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/NewModel/Model quân ta/model_quan_ta/tripo_convert_74080320-2742-4915-ab54-fe52dd1aaaa6.fbm/model_quan_ta_basecolor.JPEG");
                }
            }
            else
            {
                textureToApply = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/NewModel/Model quân địch/medieval+knight+3d+model/tripo_convert_0e217662-40c2-483b-9f6e-5f6498668c72.fbm/medieval_knight_3d_model_basecolor.JPEG");
            }
#endif
            if (textureToApply == null && unitTypeIndex != 1 && unitTypeIndex != 4)
            {
                textureToApply = unitBaseColorTexture;
            }
            
            if (textureToApply != null)
            {
                var rends = graphics.GetComponentsInChildren<Renderer>();
                foreach (var r in rends)
                {
                    // Skip weapon renderers (like the King's sword meshes[0].001) so they don't get overwritten with the body texture
                    if (r.name.Contains("meshes[0].001") || r.name.ToLower().Contains("sword") || r.name.ToLower().Contains("weapon") || r.name.ToLower().Contains("bow") || r.name.ToLower().Contains("shield") || r.name.ToLower().Contains("arrow"))
                    {
                        continue;
                    }
                    if (r.material != null)
                    {
                        r.material.SetTexture("_BaseMap", textureToApply);
                        r.material.SetTexture("_MainTex", textureToApply);
                    }
                }
            }

            // For the Player King: parent sword bone (Armature/Bone) to RightHand at runtime.
            // All animation curves for Armature/Bone are stripped in AnimatorSetupTool,
            // so the sword won't fight between animation and parenting.
            // For the Enemy General: the axe (meshes[0].001) is already parented to
            // mixamorig:RightHand in the FBX hierarchy, so no extra parenting is needed.
            if (unitTypeIndex == 4 && isPlayer)
            {
                Transform bone = graphics.transform.Find("Armature/Bone");
                Transform hand = null;
                foreach (var t in graphics.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "mixamorig:RightHand")
                    {
                        hand = t;
                        break;
                    }
                }
                if (bone != null && hand != null)
                {
                    bone.SetParent(hand, true);
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
                var allRenderers = graphics.GetComponentsInChildren<Renderer>();
                List<Renderer> validRenderers = new List<Renderer>();
                foreach (var r in allRenderers)
                {
                    if (r.name.ToLower().Contains("sword") || 
                        r.name.ToLower().Contains("bow") || 
                        r.name.ToLower().Contains("shield") || 
                        r.name.ToLower().Contains("arrow")) continue;
                    validRenderers.Add(r);
                }

                if (validRenderers.Count == 0)
                {
                    validRenderers.AddRange(allRenderers);
                }

                if (validRenderers.Count > 0)
                {
                    Bounds b = validRenderers[0].bounds;
                    for (int i = 1; i < validRenderers.Count; i++) b.Encapsulate(validRenderers[i].bounds);

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
            var allRenderers = rootObj.GetComponentsInChildren<Renderer>();
            List<Renderer> validRenderers = new List<Renderer>();
            foreach (var r in allRenderers)
            {
                if (r.name.ToLower().Contains("sword") || 
                    r.name.ToLower().Contains("bow") || 
                    r.name.ToLower().Contains("shield") || 
                    r.name.ToLower().Contains("arrow")) continue;
                validRenderers.Add(r);
            }

            if (validRenderers.Count > 0)
            {
                Bounds bounds = validRenderers[0].bounds;
                for (int i = 1; i < validRenderers.Count; i++) bounds.Encapsulate(validRenderers[i].bounds);

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

        // Enforce base stats for each unit type as specified by the user
        if (unitTypeIndex == 0) // Bộ
        {
            unit.speed = 64f;
            if (isPlayer && SkillManager.Instance != null && SkillManager.Instance.troopLevel >= 2)
            {
                unit.hp = 120f;
                unit.maxHp = 120f;
                unit.atk = 15f;
                unit.def = 8f;
            }
            else
            {
                unit.hp = 100f;
                unit.maxHp = 100f;
                unit.atk = 10f;
                unit.def = 5f;
            }
        }
        else if (unitTypeIndex == 1) // Cung
        {
            unit.speed = 64f;
            unit.hp = 80f;
            unit.maxHp = 80f;
            unit.atk = 15f;
            unit.def = 2f;
        }
        else if (unitTypeIndex == 4) // Tướng
        {
            unit.speed = 64f;
            unit.hp = 200f;
            unit.maxHp = 200f;
            unit.atk = 13f;
            unit.def = 6f;
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

        // Scale speed and attack range dynamically to match the grid generator's spacing
        BattlefieldGridGenerator gridGen = Object.FindAnyObjectByType<BattlefieldGridGenerator>();
        float scale = 1f;
        if (gridGen != null)
        {
            scale = gridGen.rowSpacing / 70f;
        }
        else
        {
            scale = colRadius / 0.4f;
        }

        unit.speed *= scale;

        // Scale range but ensure it exceeds physical contact distance (colRadius * 2.2f)
        float baseRange = Mathf.Max(unit.attackRange * scale, colRadius * 2.2f);

        if (unitTypeIndex == 1) // Cung (scales from 133.3757 -> 210 final range under normal scale)
        {
            unit.attackRange = 210f * scale;
        }
        else if (unitTypeIndex == 4) // Tướng (give slightly more range so he can easily reach and hit enemies)
        {
            unit.attackRange = baseRange * 1.25f;
        }
        else // Bộ
        {
            unit.attackRange = baseRange;
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
        BGMManager.Instance.PlayMusic("Audio/BattleTheme", true);

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
            BGMManager.Instance.PlayMusic("Audio/Defeat", false);
            if (UIManager.Instance != null) UIManager.Instance.ShowGameOver(false);
        }
        else if (enemyUnits.Count == 0)
        {
            currentState = GameState.GameOver;
            BGMManager.Instance.PlayMusic("Audio/Victory", false);
            
            // Handle level completion step and award skill points
            if (activeCastleName == "Hoan Châu")
            {

                int tutStep = PlayerPrefs.GetInt("TutorialStep", 0);
                if (tutStep < 2)
                {
                    PlayerPrefs.SetInt("TutorialStep", 2);
                    PlayerPrefs.Save();
                    SkillManager.SkillPointsStatic += 1;
                    Debug.Log($"[GameManager] Hoan Châu (Tutorial) won! Awarded 1 skill point. Current points={SkillManager.SkillPointsStatic}");
                }
            }
            else
            {
                int currentProg = PlayerPrefs.GetInt("MapProgression", 0);
                if (activeCastleName == "Trại Yên" && currentProg == 1)
                {
                    PlayerPrefs.SetInt("MapProgression", 2);
                    PlayerPrefs.SetInt("DialogueAfter_Trại Yên_Pending", 1);
                    SkillManager.SkillPointsStatic += 1;
                    Debug.Log($"[GameManager] Trại Yên won! Awarded 1 skill point. Current points={SkillManager.SkillPointsStatic}");
                }
                else if (activeCastleName == "Thiên Trường" && currentProg == 2)
                {
                    PlayerPrefs.SetInt("MapProgression", 3);
                    PlayerPrefs.SetInt("DialogueAfter_Thiên Trường_Pending", 1);
                    SkillManager.SkillPointsStatic += 2;
                    Debug.Log($"[GameManager] Thiên Trường won! Awarded 2 skill points. Current points={SkillManager.SkillPointsStatic}");
                }
                else if (activeCastleName == "Thăng Long" && currentProg == 3)
                {
                    PlayerPrefs.SetInt("MapProgression", 4);
                    PlayerPrefs.SetInt("DialogueAfter_Thăng Long_Pending", 1);
                }
                PlayerPrefs.Save();

            }

            int mapProgress = PlayerPrefs.GetInt("MapProgress", 0);
            if (activeCastleName == "Hoan Châu" && mapProgress == 0)
            {
                PlayerPrefs.SetInt("MapProgress", 1);
            }
            else if (activeCastleName == "Trại Yên" && mapProgress == 1)
            {
                PlayerPrefs.SetInt("MapProgress", 2);
            }
            else if (activeCastleName == "Thiên Trường" && mapProgress == 2)
            {
                PlayerPrefs.SetInt("MapProgress", 3);
            }
            else if (activeCastleName == "Thăng Long" && mapProgress == 3)
            {
                PlayerPrefs.SetInt("MapProgress", 4);
            }
            PlayerPrefs.Save();

            if (UIManager.Instance != null) UIManager.Instance.ShowGameOver(true);
        }
    }
}
