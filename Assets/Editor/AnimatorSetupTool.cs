using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public class AnimatorSetupTool
{
    static AnimatorSetupTool()
    {
        // Auto-run on project compilation to keep the animator controller synced
        EditorApplication.delayCall += () => {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SetupAnimator();
            }
        };
    }

    [MenuItem("Tools/PRU/Setup Animator Controller")]
    public static void SetupAnimator()
    {
        // Force King models to Generic so their animations can be imported properly
        string kingModelPath = "Assets/Models/NewModel/Model quân ta/Model_tuong_quan_ta/animation_tuong_quan_ta.fbx";
        ModelImporter kingImporter = AssetImporter.GetAtPath(kingModelPath) as ModelImporter;
        if (kingImporter != null && kingImporter.animationType != ModelImporterAnimationType.Generic)
        {
            kingImporter.animationType = ModelImporterAnimationType.Generic;
            kingImporter.SaveAndReimport();
        }

        // Force Enemy King/General models to Generic so their animations can be imported properly and extract textures
        string enemyKingModelPath = "Assets/Models/NewModel/Model quân địch/model_tuong_quan_dich/animation_tuong_quan_dich.fbx";
        ModelImporter enemyKingImporter = AssetImporter.GetAtPath(enemyKingModelPath) as ModelImporter;
        if (enemyKingImporter != null)
        {
            bool needsReimport = false;
            if (enemyKingImporter.animationType != ModelImporterAnimationType.Generic)
            {
                enemyKingImporter.animationType = ModelImporterAnimationType.Generic;
                needsReimport = true;
                Debug.Log("Forced Enemy General Rig to Generic: " + enemyKingModelPath);
            }
            if (enemyKingImporter.avatarSetup != ModelImporterAvatarSetup.NoAvatar)
            {
                enemyKingImporter.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                needsReimport = true;
                Debug.Log("Forced Enemy General Avatar Setup to NoAvatar: " + enemyKingModelPath);
            }

            // Extract embedded textures to avoid white untextured meshes (like the axe)
            string extractPath = "Assets/Models/NewModel/Model quân địch/model_tuong_quan_dich";
            if (enemyKingImporter.ExtractTextures(extractPath))
            {
                needsReimport = true;
                Debug.Log("Extracted textures for Enemy General to: " + extractPath);
            }

            // Always reimport the enemy general model to ensure animation data is fresh.
            // Generic rig animations can become stale/corrupted on initial import, requiring
            // a forced reimport to properly bake the animation curves into the clip sub-assets.
            enemyKingImporter.SaveAndReimport();
            AssetDatabase.ImportAsset(enemyKingModelPath, ImportAssetOptions.ForceUpdate);
        }

        // 0. Ensure rigs are Humanoid
        ConvertRigsToHumanoid();

        // Set per-clip keepOriginalPositionY: idle FBX files use 1 (bind pose locked),
        // Run FBX uses 0 (root Y lifts the run cycle naturally).
        FixInfantryAnimationImportSettings();

        string folderPath = "Assets/Art/Animations";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        // Setup Infantry Controller
        string infantryControllerPath = folderPath + "/QuanLinhAnimatorController.controller";
        AnimatorController infantryController = SetupController(infantryControllerPath, 0);

        // Setup Archer Controller
        string archerControllerPath = folderPath + "/QuanCungAnimatorController.controller";
        AnimatorController archerController = SetupController(archerControllerPath, 1);

        // Setup King Controller
        string kingControllerPath = folderPath + "/QuanVuaAnimatorController.controller";
        AnimatorController kingController = SetupController(kingControllerPath, 2);

        // Assign to GameManager in the current active scene
        GameManager[] managers = Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (managers.Length > 0)
        {
            foreach (GameManager manager in managers)
            {
                Undo.RecordObject(manager, "Assign Animator Controllers");
                manager.unitAnimatorController = infantryController;
                manager.archerAnimatorController = archerController;
                manager.kingAnimatorController = kingController;

                // Set King defaults in Inspector
                manager.kingModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân ta/Model_tuong_quan_ta/animation_tuong_quan_ta.fbx");
                manager.kingScale = 72f;
                manager.kingRotationOffset = new Vector3(0f, 0f, 0f);
                manager.kingPositionOffset = new Vector3(0f, 0.03f, 0f);

                // Set Enemy King defaults in Inspector
                manager.enemyKingModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân địch/model_tuong_quan_dich/animation_tuong_quan_dich.fbx");
                manager.enemyKingScale = 72f;
                manager.enemyKingRotationOffset = new Vector3(0f, 0f, 0f);
                manager.enemyKingPositionOffset = new Vector3(0f, 0.03f, 0f);
                manager.enemyKingAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Animations/QuanTuongDichAnimatorController.controller");

                // Set Enemy Infantry & Archer prefabs
                manager.enemyUnitModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân địch/Trang_thai_cho_quan_dich.fbx");
                manager.enemyArcherModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân địch/model_quan_cung/animation_ban_cung_quan_dich.fbx");

                manager.archerModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/animation_ban_cung_quan_ta.fbx");
                manager.archerScale = 60f;
                manager.archerRotationOffset = Vector3.zero;
                manager.forceCapsuleForTesting = false;
                manager.unitBaseColorTexture = null;
                EditorUtility.SetDirty(manager);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }

            Debug.Log("Assigned Animator Controllers and archer/king model settings to all GameManagers in scene!");
        }
        else
        {
            Debug.LogWarning("GameManager not found in current scene. Please open the correct scene and run this tool again to auto-assign it.");
        }

        Debug.Log("Animator Controller Setup completed successfully!");
    }

    private static AnimatorController SetupController(string controllerPath, int unitType)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            Debug.Log("Created Animator Controller at: " + controllerPath);
        }

        AddParameterIfNotExists(controller, "IsMoving", AnimatorControllerParameterType.Bool);
        AddParameterIfNotExists(controller, "IsAttacking", AnimatorControllerParameterType.Bool);
        AddParameterIfNotExists(controller, "IsDead", AnimatorControllerParameterType.Bool);
        AddParameterIfNotExists(controller, "Attack", AnimatorControllerParameterType.Trigger);
        AddParameterIfNotExists(controller, "Die", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        AnimatorState idleState = GetOrCreateState(rootStateMachine, "Idle");
        AnimatorState runState = GetOrCreateState(rootStateMachine, "Run");
        AnimatorState attackState = GetOrCreateState(rootStateMachine, "Attack");
        AnimatorState dieState = GetOrCreateState(rootStateMachine, "Die");

        rootStateMachine.defaultState = idleState;

        if (unitType == 1)
        {
            AssignArcherClipsToStates(idleState, runState, attackState, dieState);
            attackState.speed = 1.5f; // Played at 1.5x speed matches natural bow draw and release (~2.88s duration)
        }
        else if (unitType == 2)
        {
            AssignKingClipsToStates(idleState, runState, attackState, dieState);
            attackState.speed = 1.0f;
        }
        else
        {
            AssignClipsToStates(idleState, runState, attackState, dieState);
            attackState.speed = 1.0f;
        }

        AddTransitionIfNotExists(idleState, runState, new AnimatorCondition[] {
            new AnimatorCondition { mode = AnimatorConditionMode.If, parameter = "IsMoving", threshold = 0 }
        }, false);

        AddTransitionIfNotExists(runState, idleState, new AnimatorCondition[] {
            new AnimatorCondition { mode = AnimatorConditionMode.IfNot, parameter = "IsMoving", threshold = 0 }
        }, false);

        AddAnyStateTransitionIfNotExists(rootStateMachine, attackState, new AnimatorCondition[] {
            new AnimatorCondition { mode = AnimatorConditionMode.If, parameter = "Attack", threshold = 0 }
        });

        AddTransitionWithExitTimeIfNotExists(attackState, idleState);

        AddAnyStateTransitionIfNotExists(rootStateMachine, dieState, new AnimatorCondition[] {
            new AnimatorCondition { mode = AnimatorConditionMode.If, parameter = "IsDead", threshold = 0 }
        });
        AddAnyStateTransitionIfNotExists(rootStateMachine, dieState, new AnimatorCondition[] {
            new AnimatorCondition { mode = AnimatorConditionMode.If, parameter = "Die", threshold = 0 }
        });

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        return controller;
    }

    private static void AssignClipsToStates(AnimatorState idleState, AnimatorState runState, AnimatorState attackState, AnimatorState dieState)
    {
        Debug.Log("--- START ASSIGNING CLIPS FROM NEWMODEL FOLDER ---");
        
        idleState.motion = null;
        runState.motion = null;
        attackState.motion = null;
        dieState.motion = null;

        // Priority FBX files: scan in this order so ho_bon_quan's own clips win.
        // Model_quan_ta.fbx is a static mesh (no clips), so ho_bon_quan provides
        // the complete animation set for all infantry via humanoid retargeting.
        // ho_bon_quan has: animation_idle, animation_run, animation_attack_1/2, animation_dying.
        string[] priorityFbxPaths = new string[]
        {
            "Assets/Models/NewModel/Model quân ta/model_ho_bon_quan/animation_ho_bon_quan.fbx",
            "Assets/Models/NewModel/medieval knight 3d model@Sword And Shield Run.fbx",
            "Assets/Models/NewModel/medieval knight 3d model@Sword And Shield Slash.fbx",
            "Assets/Models/NewModel/medieval knight 3d model@Sword And Shield Death.fbx",
        };

        void ScanFbx(string fbxPath, bool skipIfTaken)
        {
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (Object asset in allAssets)
            {
                AnimationClip clip = asset as AnimationClip;
                if (clip == null) continue;
                if (clip.name.StartsWith("__preview__")) continue;

                string clipName = clip.name.ToLower();
                string pathName = fbxPath.ToLower().Replace('\\', '/');

                Debug.Log($"Found Clip: '{clip.name}' in path: '{fbxPath}'");

                // Include "dying" for animation_dying from ho_bon_quan.fbx
                bool isIdle = pathName.Contains("trang_thai_cho") || clipName.Contains("idle") || pathName.Contains("doi") || pathName.Contains("cho");
                bool isRun = pathName.Contains("run") || clipName.Contains("run") || clipName.Contains("walk") || pathName.Contains("chay") || pathName.Contains("dibo");
                bool isAttack = pathName.Contains("slash") || clipName.Contains("attack") || clipName.Contains("hit") || clipName.Contains("slash");
                bool isDie = pathName.Contains("death") || clipName.Contains("die") || clipName.Contains("dead") || clipName.Contains("dying");

                if (isIdle && (idleState.motion == null || !skipIfTaken))
                {
                    idleState.motion = clip;
                    EnableLoopTime(clip);
                    Debug.Log(">> Assigned Idle clip: " + clip.name + " (" + fbxPath + ")");
                }
                else if (isRun && (runState.motion == null || !skipIfTaken))
                {
                    runState.motion = clip;
                    EnableLoopTime(clip);
                    Debug.Log(">> Assigned Run clip: " + clip.name + " (" + fbxPath + ")");
                }
                else if (isAttack && (attackState.motion == null || !skipIfTaken))
                {
                    attackState.motion = clip;
                    Debug.Log(">> Assigned Attack clip: " + clip.name + " (" + fbxPath + ")");
                }
                else if (isDie && (dieState.motion == null || !skipIfTaken))
                {
                    dieState.motion = clip;
                    Debug.Log(">> Assigned Die clip: " + clip.name + " (" + fbxPath + ")");
                }
            }
        }

        // Pass 1: priority FBX files — skipIfTaken=false so later priority
        // paths can override earlier ones (unused here since first match wins).
        foreach (string path in priorityFbxPaths)
        {
            ScanFbx(path, true);
        }

        // Pass 2: scan remaining NewModel FBX files for any clips still missing
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Replace('\\', '/').Contains("Assets/Models/NewModel")) continue;
            if (path.Replace('\\', '/').Contains("Model_cung_quan_ta")) continue;
            if (path.Replace('\\', '/').Contains("animation_cung_quan_ta")) continue;
            if (path.Replace('\\', '/').Contains("Model_vua")) continue;

            // Skip if already processed in priority pass
            bool alreadyProcessed = false;
            foreach (string pp in priorityFbxPaths)
            {
                if (string.Equals(path.Replace('\\', '/'), pp.Replace('\\', '/'), System.StringComparison.OrdinalIgnoreCase))
                {
                    alreadyProcessed = true;
                    break;
                }
            }
            if (alreadyProcessed) continue;

            ScanFbx(path, true);
        }

        Debug.Log("--- END ASSIGNING CLIPS FROM NEWMODEL FOLDER ---");
    }

    private static void ConvertArcherRigsToGeneric()
    {
        Debug.Log("--- START CONVERTING ARCHER RIGS TO GENERIC ---");
        string[] paths = new string[] {
            "Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/animation_ban_cung_quan_ta.fbx",
            "Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/model_quan_cung@Standing Idle.fbx",
            "Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/model_quan_cung@Standing Run Forward.fbx",
            "Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/Standing Death Forward.fbx",
            "Assets/Models/NewModel/Model quân địch/model_quan_cung/animation_ban_cung_quan_dich.fbx"
        };
        foreach (string path in paths)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                bool modified = false;
                if (importer.animationType != ModelImporterAnimationType.Generic)
                {
                    importer.animationType = ModelImporterAnimationType.Generic;
                    modified = true;
                }

                // Specifically for animation_ban_cung_quan_ta.fbx, correct the clip animations definition
                if (path.EndsWith("animation_ban_cung_quan_ta.fbx"))
                {
                    if (importer.clipAnimations == null || 
                        importer.clipAnimations.Length != 1 || 
                        importer.clipAnimations[0].name != "animation_ban_cung" || 
                        importer.clipAnimations[0].takeName != "Armature.001|Armature.001|Armature.001|animation_ban_cung")
                    {
                        var clips = new ModelImporterClipAnimation[1];
                        clips[0] = new ModelImporterClipAnimation();
                        clips[0].name = "animation_ban_cung";
                        clips[0].takeName = "Armature.001|Armature.001|Armature.001|animation_ban_cung";
                        clips[0].firstFrame = 130;
                        clips[0].lastFrame = 260;
                        clips[0].loopTime = false;
                        importer.clipAnimations = clips;
                        modified = true;
                    }
                }
                else if (path.EndsWith("animation_ban_cung_quan_dich.fbx"))
                {
                    if (importer.clipAnimations == null || 
                        importer.clipAnimations.Length != 1 || 
                        importer.clipAnimations[0].name != "animation_ban_cung" || 
                        importer.clipAnimations[0].takeName != "Armature.001|Armature.001|Armature.001|Armature.001|animation_ban_cung")
                    {
                        var clips = new ModelImporterClipAnimation[1];
                        clips[0] = new ModelImporterClipAnimation();
                        clips[0].name = "animation_ban_cung";
                        clips[0].takeName = "Armature.001|Armature.001|Armature.001|Armature.001|animation_ban_cung";
                        clips[0].firstFrame = 130;
                        clips[0].lastFrame = 260;
                        clips[0].loopTime = false;
                        importer.clipAnimations = clips;
                        modified = true;
                    }
                }

                if (modified)
                {
                    importer.SaveAndReimport();
                    Debug.Log("Converted Archer Rig / Clip to Generic: " + path);
                }
            }
        }
        Debug.Log("--- END CONVERTING ARCHER RIGS TO GENERIC ---");
    }

    private static void AssignArcherClipsToStates(AnimatorState idleState, AnimatorState runState, AnimatorState attackState, AnimatorState dieState)
    {
        Debug.Log("--- START ASSIGNING ARCHER CLIPS (GENERIC) ---");
        idleState.motion = null;
        runState.motion = null;
        attackState.motion = null;
        dieState.motion = null;

        // 1. Force Archer models to Generic
        ConvertArcherRigsToGeneric();

        string[] fbxPaths = new string[] {
            "Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/animation_ban_cung_quan_ta.fbx",
            "Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/model_quan_cung@Standing Idle.fbx",
            "Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/model_quan_cung@Standing Run Forward.fbx",
            "Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/Standing Death Forward.fbx"
        };
        
        string outputDir = "Assets/Art/Animations/ArcherClips";
        if (System.IO.Directory.Exists(outputDir))
        {
            System.IO.Directory.Delete(outputDir, true);
        }
        System.IO.Directory.CreateDirectory(outputDir);
        AssetDatabase.Refresh();

        foreach (string fbxPath in fbxPaths)
        {
            Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(fbxPath);
            if (subAssets != null && subAssets.Length > 0)
            {
                foreach (Object asset in subAssets)
                {
                    AnimationClip sourceClip = asset as AnimationClip;
                    if (sourceClip == null || sourceClip.name.StartsWith("__preview__")) continue;

                    AnimationClip destClip = new AnimationClip();

                    // Copy float curves and rewrite paths targeting correct Armature.001
                    var floatBindings = AnimationUtility.GetCurveBindings(sourceClip);
                    foreach (var binding in floatBindings)
                    {
                        var newBinding = binding;
                        
                        // Rewrite path
                        if (newBinding.path.StartsWith("mixamorig:Hips"))
                        {
                            newBinding.path = "Armature.001/" + newBinding.path;
                        }
                        else if (newBinding.path.StartsWith("Armature/"))
                        {
                            newBinding.path = "Armature.001/" + newBinding.path.Substring(9);
                        }
                        else if (newBinding.path == "Armature")
                        {
                            newBinding.path = "Armature.001";
                        }
                        else if (newBinding.path.StartsWith("Armature.001"))
                        {
                            // Already starts with Armature.001, keep it
                        }
                        else if (newBinding.path != "")
                        {
                            newBinding.path = "Armature.001/" + newBinding.path;
                        }

                        // Strip ALL bone scale curves to prevent animation from overriding bone scale
                        if (newBinding.propertyName.Contains("m_LocalScale"))
                        {
                            continue;
                        }

                        // Strip rotation on root Armature.001 (but NOT mixamorig:Hips) to prevent tilting/sideways rotation
                        bool isRootRotation = newBinding.path == "Armature.001";
                        if (isRootRotation && newBinding.propertyName.Contains("m_LocalRotation"))
                        {
                            continue;
                        }

                        // Strip X/Y/Z root translation to prevent root motion sliding and vertical offset issues
                        bool isRootPath = newBinding.path == "Armature.001" || newBinding.path == "Armature.001/mixamorig:Hips";
                        bool isTranslation = newBinding.propertyName.Contains("m_LocalPosition");
                        if (isRootPath && isTranslation)
                        {
                            continue;
                        }

                        AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                        AnimationUtility.SetEditorCurve(destClip, newBinding, curve);
                    }

                    // Copy object reference curves and rewrite paths
                    var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(sourceClip);
                    foreach (var binding in objectBindings)
                    {
                        var newBinding = binding;
                        
                        // Rewrite path
                        if (newBinding.path.StartsWith("mixamorig:Hips"))
                        {
                            newBinding.path = "Armature.001/" + newBinding.path;
                        }
                        else if (newBinding.path.StartsWith("Armature/"))
                        {
                            newBinding.path = "Armature.001/" + newBinding.path.Substring(9);
                        }
                        else if (newBinding.path == "Armature")
                        {
                            newBinding.path = "Armature.001";
                        }
                        else if (newBinding.path.StartsWith("Armature.001"))
                        {
                            // Already starts with Armature.001, keep it
                        }
                        else if (newBinding.path != "")
                        {
                            newBinding.path = "Armature.001/" + newBinding.path;
                        }

                        // Strip ALL bone scale reference curves
                        if (newBinding.propertyName.Contains("m_LocalScale"))
                        {
                            continue;
                        }

                        // Strip rotation on root Armature.001 (but NOT mixamorig:Hips) reference curves
                        bool isRootRotation = newBinding.path == "Armature.001";
                        if (isRootRotation && newBinding.propertyName.Contains("m_LocalRotation"))
                        {
                            continue;
                        }

                        // Strip X/Y/Z root translation reference curves to prevent root motion sliding and vertical offset issues
                        bool isRootPath = newBinding.path == "Armature.001" || newBinding.path == "Armature.001/mixamorig:Hips";
                        bool isTranslation = newBinding.propertyName.Contains("m_LocalPosition");
                        if (isRootPath && isTranslation)
                        {
                            continue;
                        }

                        var keyframes = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
                        AnimationUtility.SetObjectReferenceCurve(destClip, newBinding, keyframes);
                    }

                    var settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
                    AnimationUtility.SetAnimationClipSettings(destClip, settings);

                    string fbxPrefix = System.IO.Path.GetFileNameWithoutExtension(fbxPath);
                    string nameClean = fbxPrefix + "_" + sourceClip.name.Replace('|', '_').Replace('.', '_');
                    string outPath = outputDir + "/" + nameClean + ".anim";
                    AssetDatabase.CreateAsset(destClip, outPath);
                }
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2. Load fixed clips and assign to Animator Controller
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new string[] { outputDir });
        foreach (string guid in guids)
        {
            string clipPath = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) continue;

            string clipName = clip.name.ToLower();

            bool isIdle = clipName.Contains("idle");
            bool isRun = clipName.Contains("run");
            bool isAttack = clipName.Contains("animation_ban_cung") || clipName.Contains("ban_cung");
            bool isDie = clipName.Contains("death") || clipName.Contains("die");

            if (isIdle && idleState.motion == null)
            {
                idleState.motion = clip;
                EnableLoopTime(clip);
                Debug.Log(">> Assigned Fixed Archer Idle clip: " + clip.name);
            }
            else if (isRun && runState.motion == null)
            {
                runState.motion = clip;
                EnableLoopTime(clip);
                Debug.Log(">> Assigned Fixed Archer Run clip: " + clip.name);
            }
            else if (isAttack && attackState.motion == null)
            {
                attackState.motion = clip;
                Debug.Log(">> Assigned Fixed Archer Attack clip: " + clip.name);
            }
            else if (isDie && dieState.motion == null)
            {
                dieState.motion = clip;
                Debug.Log(">> Assigned Fixed Archer Die clip: " + clip.name);
            }
        }

        Debug.Log("--- END ASSIGNING ARCHER CLIPS (GENERIC) ---");
    }

    private static void AssignKingClipsToStates(AnimatorState idleState, AnimatorState runState, AnimatorState attackState, AnimatorState dieState)
    {
        Debug.Log("--- START ASSIGNING KING CLIPS ---");
        idleState.motion = null;
        runState.motion = null;
        attackState.motion = null;
        dieState.motion = null;

        // 1. Extract and fix curves from the fbx file
        string[] fbxPaths = new string[] {
            "Assets/Models/NewModel/Model quân ta/Model_tuong_quan_ta/animation_tuong_quan_ta.fbx"
        };
        
        string outputDir = "Assets/Art/Animations/KingClips";
        if (System.IO.Directory.Exists(outputDir))
        {
            System.IO.Directory.Delete(outputDir, true);
        }
        System.IO.Directory.CreateDirectory(outputDir);
        AssetDatabase.Refresh();

        foreach (string fbxPath in fbxPaths)
        {
            Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(fbxPath);
            if (subAssets != null && subAssets.Length > 0)
            {
                foreach (Object asset in subAssets)
                {
                    AnimationClip sourceClip = asset as AnimationClip;
                    if (sourceClip == null || sourceClip.name.StartsWith("__preview__")) continue;

                    AnimationClip destClip = new AnimationClip();

                    // Copy float curves and rewrite paths targeting correct Armature
                    var floatBindings = AnimationUtility.GetCurveBindings(sourceClip);
                    foreach (var binding in floatBindings)
                    {
                        var newBinding = binding;
                        newBinding.path = newBinding.path.Replace("Armature.001/", "Armature/")
                                                         .Replace("Armature.002/", "Armature/")
                                                         .Replace("Armature.003/", "Armature/")
                                                         .Replace("Armature.004/", "Armature/");
                        if (newBinding.path.StartsWith("Armature.001")) newBinding.path = "Armature" + newBinding.path.Substring(12);
                        if (newBinding.path.StartsWith("Armature.002")) newBinding.path = "Armature" + newBinding.path.Substring(12);
                        if (newBinding.path.StartsWith("Armature.003")) newBinding.path = "Armature" + newBinding.path.Substring(12);
                        if (newBinding.path.StartsWith("Armature.004")) newBinding.path = "Armature" + newBinding.path.Substring(12);

                        // Strip ALL sword bone curves (Armature/Bone and children)
                        // The sword will be parented to RightHand at runtime, so animation
                        // curves for Bone would fight with the parenting and cause sliding/detaching.
                        bool isSwordBone = newBinding.path == "Armature/Bone" || newBinding.path.StartsWith("Armature/Bone/");
                        if (isSwordBone)
                        {
                            continue;
                        }

                        // Strip ALL bone scale curves to prevent animation from overriding bone scale
                        if (newBinding.propertyName.Contains("m_LocalScale"))
                        {
                            continue;
                        }

                        // Strip rotation on root Armature or mixamorig:Hips to prevent tilting/sideways rotation
                        // (Commented out to preserve correct animation on new general model)
                        /*
                        bool isHipsOrRootRotation = newBinding.path == "Armature" || newBinding.path == "Armature/mixamorig:Hips";
                        if (isHipsOrRootRotation && newBinding.propertyName.Contains("m_LocalRotation"))
                        {
                            continue;
                        }
                        */

                        // Strip X/Z root translation to prevent root motion sliding
                        bool isRootPath = newBinding.path == "Armature" || newBinding.path == "Armature/mixamorig:Hips";
                        bool isXZTranslation = newBinding.propertyName.Contains("m_LocalPosition.x") || newBinding.propertyName.Contains("m_LocalPosition.z");
                        if (isRootPath && isXZTranslation)
                        {
                            continue;
                        }

                        AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                        AnimationUtility.SetEditorCurve(destClip, newBinding, curve);
                    }

                    // Copy object reference curves and rewrite paths
                    var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(sourceClip);
                    foreach (var binding in objectBindings)
                    {
                        var newBinding = binding;
                        newBinding.path = newBinding.path.Replace("Armature.001/", "Armature/")
                                                         .Replace("Armature.002/", "Armature/")
                                                         .Replace("Armature.003/", "Armature/")
                                                         .Replace("Armature.004/", "Armature/");
                        if (newBinding.path.StartsWith("Armature.001")) newBinding.path = "Armature" + newBinding.path.Substring(12);
                        if (newBinding.path.StartsWith("Armature.002")) newBinding.path = "Armature" + newBinding.path.Substring(12);
                        if (newBinding.path.StartsWith("Armature.003")) newBinding.path = "Armature" + newBinding.path.Substring(12);
                        if (newBinding.path.StartsWith("Armature.004")) newBinding.path = "Armature" + newBinding.path.Substring(12);

                        // Strip ALL sword bone reference curves
                        bool isSwordBone = newBinding.path == "Armature/Bone" || newBinding.path.StartsWith("Armature/Bone/");
                        if (isSwordBone)
                        {
                            continue;
                        }

                        // Strip ALL bone scale reference curves
                        if (newBinding.propertyName.Contains("m_LocalScale"))
                        {
                            continue;
                        }

                        // Strip rotation on root Armature or mixamorig:Hips reference curves
                        // (Commented out to preserve correct animation on new general model)
                        /*
                        bool isHipsOrRootRotation = newBinding.path == "Armature" || newBinding.path == "Armature/mixamorig:Hips";
                        if (isHipsOrRootRotation && newBinding.propertyName.Contains("m_LocalRotation"))
                        {
                            continue;
                        }
                        */

                        // Strip X/Z root translation reference curves
                        bool isRootPath = newBinding.path == "Armature" || newBinding.path == "Armature/mixamorig:Hips";
                        bool isXZTranslation = newBinding.propertyName.Contains("m_LocalPosition.x") || newBinding.propertyName.Contains("m_LocalPosition.z");
                        if (isRootPath && isXZTranslation)
                        {
                            continue;
                        }

                        var keyframes = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
                        AnimationUtility.SetObjectReferenceCurve(destClip, newBinding, keyframes);
                    }

                    var settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
                    AnimationUtility.SetAnimationClipSettings(destClip, settings);

                    string fbxPrefix = System.IO.Path.GetFileNameWithoutExtension(fbxPath);
                    string nameClean = fbxPrefix + "_" + sourceClip.name.Replace('|', '_').Replace('.', '_');
                    string outPath = outputDir + "/" + nameClean + ".anim";
                    AssetDatabase.CreateAsset(destClip, outPath);
                }
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2. Load fixed clips and assign to Animator Controller
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new string[] { outputDir });
        foreach (string guid in guids)
        {
            string clipPath = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) continue;

            string clipName = clip.name.ToLower();

            bool isIdle = clipName.Contains("idle");
            bool isRun = clipName.Contains("run");
            bool isAttack = clipName.Contains("attack") && !clipName.Contains("attack_2");
            bool isDie = clipName.Contains("dying") || clipName.Contains("death") || clipName.Contains("die");

            if (isIdle && idleState.motion == null)
            {
                idleState.motion = clip;
                EnableLoopTime(clip);
                Debug.Log(">> Assigned Fixed King Idle clip: " + clip.name);
            }
            else if (isRun && runState.motion == null)
            {
                runState.motion = clip;
                EnableLoopTime(clip);
                Debug.Log(">> Assigned Fixed King Run clip: " + clip.name);
            }
            else if (isAttack && attackState.motion == null)
            {
                attackState.motion = clip;
                Debug.Log(">> Assigned Fixed King Attack clip: " + clip.name);
            }
            else if (isDie && dieState.motion == null)
            {
                dieState.motion = clip;
                Debug.Log(">> Assigned Fixed King Die clip: " + clip.name);
            }
        }

        Debug.Log("--- END ASSIGNING KING CLIPS ---");
    }

    private static AnimationClip LoadArcherClip(string path, string clipName, bool loop, float firstFrame = 0, float lastFrame = 0, bool crop = false)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer != null)
        {
            bool needReimport = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                needReimport = true;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips != null && clips.Length > 0)
            {
                bool clipModified = false;
                foreach (var clip in clips)
                {
                    if (clip.name == clipName)
                    {
                        if (clip.loopTime != loop)
                        {
                            clip.loopTime = loop;
                            clipModified = true;
                        }
                        if (crop)
                        {
                            if (clip.firstFrame != firstFrame || clip.lastFrame != lastFrame)
                            {
                                clip.firstFrame = firstFrame;
                                clip.lastFrame = lastFrame;
                                clipModified = true;
                            }
                        }
                    }
                }

                if (clipModified)
                {
                    importer.clipAnimations = clips;
                    needReimport = true;
                }
            }

            if (needReimport)
            {
                importer.SaveAndReimport();
            }
        }

        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            AnimationClip clip = asset as AnimationClip;
            if (clip == null || clip.name.StartsWith("__preview__")) continue;
            if (clip.name == clipName)
            {
                if (loop)
                {
                    EnableLoopTime(clip);
                }
                Debug.Log(">> Assigned Archer Clip: " + clip.name + " (" + path + ")");
                return clip;
            }
        }

        Debug.LogWarning("Could not find Archer clip '" + clipName + "' in " + path);
        return null;
    }

    private static void EnableLoopTime(AnimationClip clip)
    {
        if (clip == null) return;
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
        if (settings != null)
        {
            SerializedProperty loopTime = settings.FindPropertyRelative("m_LoopTime");
            if (loopTime != null && !loopTime.boolValue)
            {
                loopTime.boolValue = true;
                serializedClip.ApplyModifiedProperties();
                EditorUtility.SetDirty(clip);
                Debug.Log(">> Enabled Loop Time on clip: " + clip.name);
            }
        }
    }

    private static void ConvertRigsToHumanoid()
    {
        Debug.Log("--- START CONVERTING RIGS TO HUMANOID & SETTING LOOPS ---");
        string[] guids = AssetDatabase.FindAssets("t:GameObject");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("Assets/Models") && !path.Contains("Assets/Art")) continue;
            if (!path.ToLower().EndsWith(".fbx")) continue;
            if (path.Replace('\\', '/').Contains("Model_cung_quan_ta")) continue;
            if (path.Contains("animation_cung_quan_ta")) continue;
            if (path.Replace('\\', '/').Contains("Model_vua")) continue;
            if (path.Contains("animation_tuong_quan_ta")) continue;
            if (path.Replace('\\', '/').Contains("model_tuong_quan_dich")) continue;
            if (path.Contains("animation_tuong_quan_dich")) continue;
            
            Debug.Log($"[Rig Check] Found FBX: {path}");

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                bool needReimport = false;

                // 1. Rig setup
                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    needReimport = true;
                    Debug.Log("Converted Rig to Humanoid: " + path);
                }

                // 2. Loop settings for clips inside FBX
                ModelImporterClipAnimation[] clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = importer.defaultClipAnimations;
                }

                if (clips != null && clips.Length > 0)
                {
                    bool loopModified = false;
                    foreach (var clip in clips)
                    {
                        string nameLower = clip.name.ToLower();
                        string pathLower = path.ToLower();
                        
                        // Check if it should loop
                        bool shouldLoop = nameLower.Contains("idle") || nameLower.Contains("run") || 
                                          nameLower.Contains("walk") || nameLower.Contains("track") ||
                                          pathLower.Contains("chay") || pathLower.Contains("dibo") || pathLower.Contains("doi") || pathLower.Contains("cho");
                        
                        if (shouldLoop && !clip.loopTime)
                        {
                            clip.loopTime = true;
                            loopModified = true;
                            Debug.Log($"Set loopTime=true for clip: {clip.name} in {path}");
                        }
                    }

                    if (loopModified)
                    {
                        importer.clipAnimations = clips;
                        needReimport = true;
                    }
                }

                if (needReimport)
                {
                    importer.SaveAndReimport();
                }
            }
        }
        Debug.Log("--- END CONVERTING RIGS TO HUMANOID & SETTING LOOPS ---");
        AssetDatabase.Refresh();
    }

    private static void AddParameterIfNotExists(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (!controller.parameters.Any(p => p.name == name))
        {
            controller.AddParameter(name, type);
            Debug.Log($"Added parameter: {name} ({type})");
        }
    }

    private static AnimatorState GetOrCreateState(AnimatorStateMachine stateMachine, string name)
    {
        foreach (var stateInMachine in stateMachine.states)
        {
            if (stateInMachine.state.name == name)
            {
                return stateInMachine.state;
            }
        }
        AnimatorState newState = stateMachine.AddState(name);
        Debug.Log($"Created state: {name}");
        return newState;
    }

    private static void AddTransitionIfNotExists(AnimatorState fromState, AnimatorState toState, AnimatorCondition[] conditions, bool hasExitTime)
    {
        foreach (var t in fromState.transitions)
        {
            if (t.destinationState == toState)
            {
                return;
            }
        }

        AnimatorStateTransition transition = fromState.AddTransition(toState);
        transition.hasExitTime = hasExitTime;
        transition.exitTime = hasExitTime ? 0.75f : 0f;
        transition.duration = 0.25f;

        if (conditions != null)
        {
            foreach (var cond in conditions)
            {
                transition.AddCondition(cond.mode, cond.threshold, cond.parameter);
            }
        }
        Debug.Log($"Added transition from {fromState.name} to {toState.name}");
    }

    private static void AddTransitionWithExitTimeIfNotExists(AnimatorState fromState, AnimatorState toState)
    {
        AddTransitionIfNotExists(fromState, toState, null, true);
    }

    private static void AddAnyStateTransitionIfNotExists(AnimatorStateMachine stateMachine, AnimatorState toState, AnimatorCondition[] conditions)
    {
        foreach (var t in stateMachine.anyStateTransitions)
        {
            if (t.destinationState == toState)
            {
                bool match = true;
                if (conditions != null)
                {
                    if (t.conditions.Length != conditions.Length)
                    {
                        match = false;
                    }
                    else
                    {
                        for (int i = 0; i < conditions.Length; i++)
                        {
                            if (t.conditions[i].parameter != conditions[i].parameter ||
                                t.conditions[i].mode != conditions[i].mode ||
                                !Mathf.Approximately(t.conditions[i].threshold, conditions[i].threshold))
                            {
                                match = false;
                                break;
                            }
                        }
                    }
                }
                if (match)
                {
                    t.canTransitionToSelf = false;
                    return;
                }
            }
        }

        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(toState);
        transition.hasExitTime = false;
        transition.duration = 0.2f;
        transition.canTransitionToSelf = false;

        if (conditions != null)
        {
            foreach (var cond in conditions)
            {
                transition.AddCondition(cond.mode, cond.threshold, cond.parameter);
            }
        }
        Debug.Log($"Added AnyState transition to {toState.name}");
    }

    /// <summary>
    /// Sets per-FBX keepOriginalPositionY on infantry animation files so that:
    ///   - Idle/Attack/Die FBX files → keepOriginalPositionY=1 (root Y ignored, Hips stays at bind pose)
    ///   - Run FBX file          → keepOriginalPositionY=0 (root Y active, Run clip lifts feet naturally)
    /// This prevents Idle root Y curves from sinking the Hips during capture/playback,
    /// while allowing the Run clip's root Y curve to keep the character at the correct height.
    /// </summary>
    private static void FixInfantryAnimationImportSettings()
    {
        Debug.Log("--- FIX INFANTRY ANIMATION IMPORT SETTINGS ---");

        void SetKeepY(string path, bool targetKeepY)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"  Cannot open ModelImporter for: {path}");
                return;
            }

            bool modified = false;
            ModelImporterClipAnimation[] clips = importer.clipAnimations;

            if (clips == null || clips.Length == 0)
            {
                // FBX has no explicit clip definitions (auto-generated clips).
                ModelImporterClipAnimation defaultClip = new ModelImporterClipAnimation();
                defaultClip.name = "Take 001";
                defaultClip.takeName = "";
                defaultClip.firstFrame = 0;
                defaultClip.lastFrame = importer.defaultClipAnimations != null && importer.defaultClipAnimations.Length > 0
                    ? importer.defaultClipAnimations[0].lastFrame : 100;
                defaultClip.loopTime = true;
                defaultClip.keepOriginalPositionY = targetKeepY;
                importer.clipAnimations = new ModelImporterClipAnimation[] { defaultClip };
                modified = true;
                Debug.Log($"  Set {path} → keepOriginalPositionY={(targetKeepY ? 1 : 0)} (explicit def)");
            }
            else
            {
                foreach (ModelImporterClipAnimation clip in clips)
                {
                    if (clip.keepOriginalPositionY != targetKeepY)
                    {
                        clip.keepOriginalPositionY = targetKeepY;
                        modified = true;
                        Debug.Log($"  Clip '{clip.name}' in {path} → keepOriginalPositionY={(targetKeepY ? 1 : 0)}");
                    }
                }
                importer.clipAnimations = clips;
            }

            if (modified)
            {
                importer.SaveAndReimport();
                Debug.Log($"  Reimported: {path}");
            }
            else
            {
                Debug.Log($"  Already correct: {path}");
            }
        }

        // Idle clip source → keep Y at bind pose (ignore root curves)
        SetKeepY("Assets/Models/NewModel/Model quân ta/model_ho_bon_quan/animation_ho_bon_quan.fbx", true);
        SetKeepY("Assets/Models/NewModel/Model quân địch/Trang_thai_cho_quan_dich.fbx", true);

        // Run clip source → allow root Y curve (keeps feet at correct height during run)
        SetKeepY("Assets/Models/NewModel/medieval knight 3d model@Sword And Shield Run.fbx", false);

        // Attack clip source → ignore root Y curves
        SetKeepY("Assets/Models/NewModel/medieval knight 3d model@Sword And Shield Slash.fbx", true);

        // Die clip source → ignore root Y curves
        SetKeepY("Assets/Models/NewModel/medieval knight 3d model@Sword And Shield Death.fbx", true);

        Debug.Log("--- END FIX INFANTRY ANIMATION IMPORT SETTINGS ---");
    }
}
