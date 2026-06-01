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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Wait a bit for the scene to build, then initialize enemies
        Invoke("InitializeEnemies", 0.5f);
    }

    void InitializeEnemies()
    {
        // Find all enemy tiles
        GameObject gridRoot = GameObject.Find("EnemyGrid");
        if (gridRoot == null) return;

        List<Transform> enemyTiles = new List<Transform>();
        
        foreach (Transform child in gridRoot.transform)
        {
            if (child.name.StartsWith("EnemyTile_"))
            {
                enemyTiles.Add(child);
            }
        }

        // Spawn 5 to 10 enemies
        int numEnemies = Random.Range(5, 11);
        for (int i = 0; i < numEnemies; i++)
        {
            if (enemyTiles.Count == 0) break;
            
            int randIndex = Random.Range(0, enemyTiles.Count);
            Transform tile = enemyTiles[randIndex];
            enemyTiles.RemoveAt(randIndex);

            SpawnUnit(false, tile.position);
        }

        currentState = GameState.Placement;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlacementUI();
        }
    }

    [Header("Model Settings")]
    public Vector3 modelRotationOffset = new Vector3(-90f, 0f, 0f);
    public Vector3 modelPositionOffset = new Vector3(0f, 0f, 0f);
    public float modelScale = 1.0f;
    public bool autoAlignBottom = true;

    public void SpawnUnit(bool isPlayer, Vector3 position)
    {
        GameObject rootObj = new GameObject(isPlayer ? "PlayerUnit" : "EnemyUnit");
        rootObj.transform.position = position;

        bool isCapsule = false;

#if UNITY_EDITOR
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
#else
        isCapsule = true;
#endif

        if (isCapsule)
        {
            GameObject graphics = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            graphics.transform.SetParent(rootObj.transform);
            graphics.transform.localPosition = Vector3.up * 1f;
            graphics.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            
            Renderer rend = graphics.GetComponent<Renderer>();
            if (rend != null) rend.material.color = isPlayer ? Color.blue : Color.red;
        }

        Rigidbody rb = rootObj.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.mass = 1f;
        rb.linearDamping = 1f;
        
        CapsuleCollider col = rootObj.AddComponent<CapsuleCollider>();
        col.height = 2f;
        col.center = new Vector3(0, 1f, 0);

        Unit unit = rootObj.AddComponent<Unit>();
        unit.isPlayer = isPlayer;

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
        currentState = GameState.Battle;
        
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
