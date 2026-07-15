using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class FixMissingReferences : EditorWindow
{
    [MenuItem("Tools/PRU/Fix Missing References in Scenes")]
    public static void FixReferencesInAllScenes()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new string[] { "Assets/Scenes" });
        if (sceneGuids.Length == 0)
        {
            Debug.LogWarning("[FixReferences] No scenes found in Assets/Scenes.");
            return;
        }

        Debug.Log($"[FixReferences] Found {sceneGuids.Length} scenes to process.");

        // Paths to the required assets
        string pathUnitModel = "Assets/Models/NewModel/Model quân ta/Model_quan_ta.fbx";
        string pathHoBon = "Assets/Models/NewModel/Model quân ta/model_ho_bon_quan/animation_ho_bon_quan.fbx";
        string pathArrow = "Assets/Cartoon_Weapon_Pack/Prefab/Arrow.prefab";
        string pathArcher = "Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/animation_ban_cung_quan_ta.fbx";
        string pathKing = "Assets/Models/NewModel/Model quân ta/Model_tuong_quan_ta/animation_tuong_quan_ta.fbx";
        
        string pathEnemyUnit = "Assets/Models/NewModel/Model quân địch/Trang_thai_cho_quan_dich.fbx";
        string pathEnemyArcher = "Assets/Models/NewModel/Model quân địch/model_quan_cung/animation_ban_cung_quan_dich.fbx";
        string pathEnemyKing = "Assets/Models/NewModel/Model quân địch/model_tuong_quan_dich/animation_tuong_quan_dich.fbx";

        string pathHoverTex = "Assets/Materials/SelectedNodeRe.png";
        string pathPanelSprite = "Assets/Materials/output.png";

        // Load the assets to verify they exist and have them ready
        GameObject assetUnitModel = AssetDatabase.LoadAssetAtPath<GameObject>(pathUnitModel);
        GameObject assetHoBon = AssetDatabase.LoadAssetAtPath<GameObject>(pathHoBon);
        GameObject assetArrow = AssetDatabase.LoadAssetAtPath<GameObject>(pathArrow);
        GameObject assetArcher = AssetDatabase.LoadAssetAtPath<GameObject>(pathArcher);
        GameObject assetKing = AssetDatabase.LoadAssetAtPath<GameObject>(pathKing);
        
        GameObject assetEnemyUnit = AssetDatabase.LoadAssetAtPath<GameObject>(pathEnemyUnit);
        GameObject assetEnemyArcher = AssetDatabase.LoadAssetAtPath<GameObject>(pathEnemyArcher);
        GameObject assetEnemyKing = AssetDatabase.LoadAssetAtPath<GameObject>(pathEnemyKing);

        Texture2D assetHoverTex = AssetDatabase.LoadAssetAtPath<Texture2D>(pathHoverTex);
        Sprite assetPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pathPanelSprite);

        // Sanity checks
        if (assetUnitModel == null) Debug.LogWarning($"[FixReferences] Asset not found: {pathUnitModel}");
        if (assetHoBon == null) Debug.LogWarning($"[FixReferences] Asset not found: {pathHoBon}");
        if (assetArrow == null) Debug.LogWarning($"[FixReferences] Asset not found: {pathArrow}");
        if (assetHoverTex == null) Debug.LogWarning($"[FixReferences] Asset not found: {pathHoverTex}");

        // Save currently open scene to avoid losing user work (no dialogs)
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            Debug.Log("[FixReferences] Saving current scene before modifications...");
            EditorSceneManager.SaveOpenScenes();
        }

        string originalScenePath = EditorSceneManager.GetActiveScene().path;

        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"[FixReferences] Processing scene: {scenePath}");

            var scene = EditorSceneManager.OpenScene(scenePath);
            bool isSceneDirty = false;

            // 1. Process GameManager
            var gameManagers = Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var gm in gameManagers)
            {
                bool gmDirty = false;

                if (gm.unitModelPrefab == null && assetUnitModel != null)
                {
                    gm.unitModelPrefab = assetUnitModel;
                    gmDirty = true;
                    Debug.Log($"[FixReferences]   Assigned unitModelPrefab on {gm.name}");
                }
                if (gm.hoBonQuanPrefab == null && assetHoBon != null)
                {
                    gm.hoBonQuanPrefab = assetHoBon;
                    gmDirty = true;
                    Debug.Log($"[FixReferences]   Assigned hoBonQuanPrefab on {gm.name}");
                }
                if (gm.arrowPrefab == null && assetArrow != null)
                {
                    gm.arrowPrefab = assetArrow;
                    gmDirty = true;
                    Debug.Log($"[FixReferences]   Assigned arrowPrefab on {gm.name}");
                }
                if (gm.archerModelPrefab == null && assetArcher != null)
                {
                    gm.archerModelPrefab = assetArcher;
                    gmDirty = true;
                    Debug.Log($"[FixReferences]   Assigned archerModelPrefab on {gm.name}");
                }
                if (gm.kingModelPrefab == null && assetKing != null)
                {
                    gm.kingModelPrefab = assetKing;
                    gmDirty = true;
                    Debug.Log($"[FixReferences]   Assigned kingModelPrefab on {gm.name}");
                }
                if (gm.enemyUnitModelPrefab == null && assetEnemyUnit != null)
                {
                    gm.enemyUnitModelPrefab = assetEnemyUnit;
                    gmDirty = true;
                    Debug.Log($"[FixReferences]   Assigned enemyUnitModelPrefab on {gm.name}");
                }
                if (gm.enemyArcherModelPrefab == null && assetEnemyArcher != null)
                {
                    gm.enemyArcherModelPrefab = assetEnemyArcher;
                    gmDirty = true;
                    Debug.Log($"[FixReferences]   Assigned enemyArcherModelPrefab on {gm.name}");
                }
                if (gm.enemyKingModelPrefab == null && assetEnemyKing != null)
                {
                    gm.enemyKingModelPrefab = assetEnemyKing;
                    gmDirty = true;
                    Debug.Log($"[FixReferences]   Assigned enemyKingModelPrefab on {gm.name}");
                }

                if (gmDirty)
                {
                    EditorUtility.SetDirty(gm);
                    isSceneDirty = true;
                }
            }

            // 2. Process PadHoverManager
            var hoverManagers = Object.FindObjectsByType<PadHoverManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var hm in hoverManagers)
            {
                if (hm.hoverTexture == null && assetHoverTex != null)
                {
                    hm.hoverTexture = assetHoverTex;
                    EditorUtility.SetDirty(hm);
                    isSceneDirty = true;
                    Debug.Log($"[FixReferences]   Assigned hoverTexture on {hm.name}");
                }
            }

            // 3. Process UIManager
            var uiManagers = Object.FindObjectsByType<UIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ui in uiManagers)
            {
                if (ui.panelSprite == null && assetPanelSprite != null)
                {
                    ui.panelSprite = assetPanelSprite;
                    EditorUtility.SetDirty(ui);
                    isSceneDirty = true;
                    Debug.Log($"[FixReferences]   Assigned panelSprite on {ui.name}");
                }
            }

            // Save scene if modified
            if (isSceneDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[FixReferences] Scene saved: {scenePath}");
            }
        }

        // Restore original scene if it was changed
        if (!string.IsNullOrEmpty(originalScenePath) && originalScenePath != EditorSceneManager.GetActiveScene().path)
        {
            EditorSceneManager.OpenScene(originalScenePath);
        }

        Debug.Log("[FixReferences] Successfully completed fixing missing model and texture references in all scenes!");
    }
}
