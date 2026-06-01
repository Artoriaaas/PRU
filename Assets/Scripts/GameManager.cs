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

    public void SpawnUnit(bool isPlayer, Vector3 position)
    {
        GameObject unitObj = null;

#if UNITY_EDITOR
        GameObject loadedModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/medieval+knight+3d+model (1)/tripo_convert_723d231f-acab-4514-9370-c6d57d482cd7.fbx");
        if (loadedModel != null)
        {
            unitObj = Instantiate(loadedModel);
            unitObj.transform.position = position; // adjust as needed
        }
#endif

        bool isCapsule = false;
        if (unitObj == null)
        {
            unitObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unitObj.transform.position = position + Vector3.up * 1f; // Adjust height for capsule
            unitObj.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            isCapsule = true;
        }

        Rigidbody rb = unitObj.GetComponent<Rigidbody>();
        if (rb == null) rb = unitObj.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.mass = 1f;
        rb.linearDamping = 1f;
        
        Unit unit = unitObj.GetComponent<Unit>();
        if (unit == null) unit = unitObj.AddComponent<Unit>();
        unit.isPlayer = isPlayer;

        // Visuals
        if (isCapsule)
        {
            Renderer rend = unitObj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = isPlayer ? Color.blue : Color.red;
            }
        }
        else
        {
            // For custom model, add a generic collider if missing
            if (unitObj.GetComponentInChildren<Collider>() == null)
            {
                CapsuleCollider col = unitObj.AddComponent<CapsuleCollider>();
                col.height = 2f;
                col.center = new Vector3(0, 1f, 0);
            }
        }

        if (isPlayer)
        {
            playerUnits.Add(unit);
            unitObj.name = "PlayerUnit";
        }
        else
        {
            enemyUnits.Add(unit);
            unitObj.name = "EnemyUnit";
            // Rotate enemy to face opposite direction
            if (!isCapsule) unitObj.transform.rotation = Quaternion.Euler(0, 180, 0);
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
