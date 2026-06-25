using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;

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
        // 0. Ensure rigs are Humanoid
        ConvertRigsToHumanoid();

        string folderPath = "Assets/Art/Animations";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        // Setup Infantry Controller
        string infantryControllerPath = folderPath + "/QuanLinhAnimatorController.controller";
        AnimatorController infantryController = SetupController(infantryControllerPath, false);

        // Setup Archer Controller
        string archerControllerPath = folderPath + "/QuanCungAnimatorController.controller";
        AnimatorController archerController = SetupController(archerControllerPath, true);

        // Assign to GameManager in the current active scene
        GameManager[] managers = Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (managers.Length > 0)
        {
            foreach (GameManager manager in managers)
            {
                Undo.RecordObject(manager, "Assign Animator Controllers");
                manager.unitAnimatorController = infantryController;
                manager.archerAnimatorController = archerController;
                manager.archerModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta/animation_ban_cung_quan_ta.fbx");
                manager.archerScale = 60f;
                manager.archerRotationOffset = Vector3.zero;
                manager.forceCapsuleForTesting = false;
                manager.unitModelPrefab = null;
                manager.unitBaseColorTexture = null;
                EditorUtility.SetDirty(manager);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }

            Debug.Log("Assigned Animator Controllers and archer model settings to all GameManagers in scene!");
        }
        else
        {
            Debug.LogWarning("GameManager not found in current scene. Please open the correct scene and run this tool again to auto-assign it.");
        }

        Debug.Log("Animator Controller Setup completed successfully!");
    }

    private static AnimatorController SetupController(string controllerPath, bool isArcher)
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

        if (isArcher)
        {
            AssignArcherClipsToStates(idleState, runState, attackState, dieState);
            attackState.speed = 1.5f; // Played at 1.5x speed matches natural bow draw and release (~2.88s duration)
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

        string[] guids = AssetDatabase.FindAssets("t:AnimationClip");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Only look inside the Assets/Models/NewModel folder
            if (!path.Replace('\\', '/').Contains("Assets/Models/NewModel")) continue;
            // Exclude archer files from infantry setup.
            if (path.Replace('\\', '/').Contains("Model_cung_quan_ta")) continue;
            if (path.Replace('\\', '/').Contains("animation_cung_quan_ta")) continue;

            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in allAssets)
            {
                AnimationClip clip = asset as AnimationClip;
                if (clip == null) continue;
                if (clip.name.StartsWith("__preview__")) continue;

                string clipName = clip.name.ToLower();
                string pathName = path.ToLower().Replace('\\', '/');

                Debug.Log($"Found Clip: '{clip.name}' in path: '{path}'");

                bool isIdle = pathName.Contains("trang_thai_cho") || clipName.Contains("idle") || pathName.Contains("doi") || pathName.Contains("cho");
                bool isRun = pathName.Contains("run") || clipName.Contains("run") || clipName.Contains("walk") || pathName.Contains("chay") || pathName.Contains("dibo");
                bool isAttack = pathName.Contains("slash") || clipName.Contains("attack") || clipName.Contains("hit") || clipName.Contains("slash");
                bool isDie = pathName.Contains("death") || clipName.Contains("die") || clipName.Contains("dead");

                if (isIdle && idleState.motion == null)
                {
                    idleState.motion = clip;
                    EnableLoopTime(clip);
                    Debug.Log(">> Assigned Idle clip from NewModel: " + clip.name + " (" + path + ")");
                }
                else if (isRun && runState.motion == null)
                {
                    runState.motion = clip;
                    EnableLoopTime(clip);
                    Debug.Log(">> Assigned Run clip from NewModel: " + clip.name + " (" + path + ")");
                }
                else if (isAttack && attackState.motion == null)
                {
                    attackState.motion = clip;
                    Debug.Log(">> Assigned Attack clip from NewModel: " + clip.name + " (" + path + ")");
                }
                else if (isDie && dieState.motion == null)
                {
                    dieState.motion = clip;
                    Debug.Log(">> Assigned Die clip from NewModel: " + clip.name + " (" + path + ")");
                }
            }
        }
        Debug.Log("--- END ASSIGNING CLIPS FROM NEWMODEL FOLDER ---");
    }

    private static void AssignArcherClipsToStates(AnimatorState idleState, AnimatorState runState, AnimatorState attackState, AnimatorState dieState)
    {
        Debug.Log("--- START ASSIGNING ARCHER CLIPS ---");
        idleState.motion = null;
        runState.motion = null;
        attackState.motion = null;
        dieState.motion = null;

        const string archerFolder = "Assets/Models/NewModel/Model quân ta/Model_cung_quan_ta";

        idleState.motion = LoadArcherClip(archerFolder + "/model_quan_cung@Standing Idle.fbx", "Standing Idle", true);
        runState.motion = LoadArcherClip(archerFolder + "/model_quan_cung@Standing Run Forward.fbx", "Standing Run Forward", true);
        attackState.motion = LoadArcherClip(archerFolder + "/animation_ban_cung_quan_ta.fbx", "Armature.001|animation_ban_cung", false, 130, 260, true);
        dieState.motion = LoadArcherClip(archerFolder + "/Standing Death Forward.fbx", "mixamo.com", false);

        Debug.Log("--- END ASSIGNING ARCHER CLIPS ---");
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
}
