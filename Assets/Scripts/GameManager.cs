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
        }
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
    public Vector3 modelRotationOffset = new Vector3(-90f, 0f, 0f);
    public Vector3 modelPositionOffset = new Vector3(0f, 0f, 0f);
    public float modelScale = 1.0f;
    public float capsuleScale = 15f; // Scale up the capsules to be clearly visible
    public bool autoAlignBottom = true;

    [Header("Testing")]
    public bool forceCapsuleForTesting = true;

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

#if UNITY_EDITOR
        if (!isCapsule)
        {
            GameObject loadedModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/medieval+knight+3d+model (1)/tripo_convert_723d231f-acab-4514-9370-c6d57d482cd7.fbx");
            if (loadedModel != null)
            {
                GameObject graphics = Instantiate(loadedModel, rootObj.transform);
                graphics.transform.localPosition = Vector3.zero;
                // Override prefab's local rotation with our offset to fix face-planting
                graphics.transform.localRotation = Quaternion.Euler(modelRotationOffset);
                graphics.transform.localScale = new Vector3(modelScale, modelScale, modelScale);

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
        }
#else
        isCapsule = true;
#endif

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
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.mass = 1f;
        rb.linearDamping = 1f;
        rb.isKinematic = true; // Kinematic during placement phase to prevent sliding/offsetting
        
        CapsuleCollider col = rootObj.AddComponent<CapsuleCollider>();
        col.height = 2f * capsuleScale;
        col.center = new Vector3(0, capsuleScale, 0);

        Unit unit = rootObj.AddComponent<Unit>();
        unit.isPlayer = isPlayer;

        // Scale speed and attack range dynamically to match the grid generator's spacing
        BattlefieldGridGenerator gridGen = Object.FindAnyObjectByType<BattlefieldGridGenerator>();
        if (gridGen != null)
        {
            unit.speed = gridGen.rowSpacing * 0.5f;
            unit.attackRange = gridGen.rowSpacing * 0.25f;
        }

        if (isPlayer)
        {
            playerUnits.Add(unit);
            rootObj.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            enemyUnits.Add(unit);
            // Rotate the wrapper (root) so the unit faces the player
            rootObj.transform.rotation = Quaternion.Euler(0, 180, 0);
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
