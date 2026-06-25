using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;

public class SetupBattleScene : EditorWindow
{
    [MenuItem("Tools/Setup Battle Scene Models")]
    public static void SetupModels()
    {
        string scenePath = "Assets/Scenes/2D5_Scene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Define models to instantiate
        string[] modelsToSpawn = new string[]
        {
            "Assets/Models/BattleScene/medieval tent 3d model.fbx",
            "Assets/Models/BattleScene/rock pile 3d model.fbx",
            "Assets/Models/BattleScene/wooden fence 3d model.fbx",
            "Assets/Models/BattleScene/rocky terrain 3d model.fbx"
        };

        GameObject envRoot = GameObject.Find("Environment_Models");
        if (envRoot == null)
        {
            envRoot = new GameObject("Environment_Models");
        }

        int count = 0;
        foreach (string path in modelsToSpawn)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                // Spawn a few instances of each to scatter them
                for (int i = 0; i < 3; i++)
                {
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    go.transform.SetParent(envRoot.transform);
                    
                    // Random position within a reasonable range (assuming 0,0 is center)
                    float x = Random.Range(-20f, 20f);
                    float z = Random.Range(-20f, 20f);
                    go.transform.position = new Vector3(x, 0, z);

                    // Random rotation
                    go.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

                    // Add colliders and NavMeshObstacle
                    AddPhysicsAndNavMesh(go);
                    
                    count++;
                }
            }
            else
            {
                Debug.LogWarning("Could not find model at path: " + path);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Setup completed. Spawned {count} models with NavMeshObstacles into {scene.name}.");
    }

    private static void AddPhysicsAndNavMesh(GameObject go)
    {
        // Try to add a BoxCollider covering the visual bounds
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            BoxCollider col = go.AddComponent<BoxCollider>();
            // Localize bounds relative to object
            col.center = go.transform.InverseTransformPoint(bounds.center);
            col.size = new Vector3(
                bounds.size.x / go.transform.lossyScale.x,
                bounds.size.y / go.transform.lossyScale.y,
                bounds.size.z / go.transform.lossyScale.z
            );

            // Add NavMeshObstacle
            NavMeshObstacle obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = col.center;
            obstacle.size = col.size;
        }
    }
}
