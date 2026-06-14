using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class AlignSceneCommand
{
    [MenuItem("Tools/Regenerate and Save Battle Layout")]
    public static void AlignAndSave()
    {
        string scenePath = "Assets/Scenes/2D5_Scene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        BattlefieldGridGenerator generator = Object.FindAnyObjectByType<BattlefieldGridGenerator>();
        if (generator != null)
        {
            Debug.Log("AlignSceneCommand: Found BattlefieldGridGenerator. Finding and aligning environment...");
            
            // Link references
            generator.FindOrCreateEnvironment();
            // Position/scale everything correctly
            generator.AlignEnvironment();
            // Rebuild pads
            generator.GenerateGrid();
            
            // Mark dirty and save
            EditorUtility.SetDirty(generator);
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            Debug.Log("AlignSceneCommand: Scene saved status: " + saved);
        }
        else
        {
            Debug.LogError("AlignSceneCommand: BattlefieldGridGenerator not found in scene!");
        }
    }
}
