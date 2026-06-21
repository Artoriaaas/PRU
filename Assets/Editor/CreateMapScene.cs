#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class MapSceneCreator
{
    [MenuItem("Tools/Map Scene/Create MapScene", false, 100)]
    public static void CreateMapScene()
    {
        string path = "Assets/Scenes/MapScene/MapScene.unity";
        string dir = Path.GetDirectoryName(path);

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject bootstrapperGO = new GameObject("MapSceneBootstrapper");
        bootstrapperGO.AddComponent<MapSceneBootstrapper>();

        EditorSceneManager.SaveScene(scene, path);
        AssetDatabase.SaveAssets();

        AddSceneToBuildSettings(path);

        Debug.Log("MapScene created at " + path);
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var existingScenes = EditorBuildSettings.scenes;

        foreach (var s in existingScenes)
        {
            if (s.path == scenePath)
                return;
        }

        var newScenes = new EditorBuildSettingsScene[existingScenes.Length + 1];
        System.Array.Copy(existingScenes, newScenes, existingScenes.Length);
        newScenes[existingScenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;

        Debug.Log("Added MapScene to Build Settings.");
    }
}
#endif
