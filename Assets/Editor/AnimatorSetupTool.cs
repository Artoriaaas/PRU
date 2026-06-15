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
        // Auto-run once on project compilation if the animator controller doesn't exist yet
        string controllerPath = "Assets/Art/Animations/QuanLinhAnimatorController.controller";
        if (!File.Exists(controllerPath))
        {
            EditorApplication.delayCall += () => {
                SetupAnimator();
            };
        }
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

        string controllerPath = folderPath + "/QuanLinhAnimatorController.controller";
        
        // 1. Create or load the Animator Controller
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            Debug.Log("Created Animator Controller at: " + controllerPath);
        }

        // 2. Add Parameters if they don't exist
        AddParameterIfNotExists(controller, "IsMoving", AnimatorControllerParameterType.Bool);
        AddParameterIfNotExists(controller, "IsAttacking", AnimatorControllerParameterType.Bool);
        AddParameterIfNotExists(controller, "IsDead", AnimatorControllerParameterType.Bool);
        AddParameterIfNotExists(controller, "Attack", AnimatorControllerParameterType.Trigger);
        AddParameterIfNotExists(controller, "Die", AnimatorControllerParameterType.Trigger);

        // 3. Set up State Machine states
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Find or create states
        AnimatorState idleState = GetOrCreateState(rootStateMachine, "Idle");
        AnimatorState runState = GetOrCreateState(rootStateMachine, "Run");
        AnimatorState attackState = GetOrCreateState(rootStateMachine, "Attack");
        AnimatorState dieState = GetOrCreateState(rootStateMachine, "Die");

        // Set default state explicitly
        rootStateMachine.defaultState = idleState;

        // 4. Try to find animation clips in the project to automatically bind them!
        AssignClipsToStates(idleState, runState, attackState, dieState);

        // 5. Setup Transitions
        // Idle <-> Run
        AddTransitionIfNotExists(idleState, runState, new AnimatorCondition[] {
            new AnimatorCondition { mode = AnimatorConditionMode.If, parameter = "IsMoving", threshold = 0 }
        }, false);

        AddTransitionIfNotExists(runState, idleState, new AnimatorCondition[] {
            new AnimatorCondition { mode = AnimatorConditionMode.IfNot, parameter = "IsMoving", threshold = 0 }
        }, false);

        // Any State -> Attack
        AddAnyStateTransitionIfNotExists(rootStateMachine, attackState, new AnimatorCondition[] {
            new AnimatorCondition { mode = AnimatorConditionMode.If, parameter = "Attack", threshold = 0 }
        });

        // Attack -> Idle (Exit Time)
        AddTransitionWithExitTimeIfNotExists(attackState, idleState);

        // Any State -> Die
        AddAnyStateTransitionIfNotExists(rootStateMachine, dieState, new AnimatorCondition[] {
            new AnimatorCondition { mode = AnimatorConditionMode.If, parameter = "IsDead", threshold = 0 }
        });
        AddAnyStateTransitionIfNotExists(rootStateMachine, dieState, new AnimatorCondition[] {
            new AnimatorCondition { mode = AnimatorConditionMode.If, parameter = "Die", threshold = 0 }
        });

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        // 6. Assign to GameManager in the current active scene
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        if (manager != null)
        {
            Undo.RecordObject(manager, "Assign Animator Controller");
            manager.unitAnimatorController = controller;
            EditorUtility.SetDirty(manager);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            Debug.Log("Assigned Animator Controller to GameManager in scene!");
        }
        else
        {
            Debug.LogWarning("GameManager not found in current scene. Please open the correct scene and run this tool again to auto-assign it.");
        }

        Debug.Log("Animator Controller Setup completed successfully!");
    }

    private static void AddParameterIfNotExists(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (!controller.parameters.Any(p => p.name == name))
        {
            controller.AddParameter(name, type);
        }
    }

    private static AnimatorState GetOrCreateState(AnimatorStateMachine stateMachine, string name)
    {
        foreach (var state in stateMachine.states)
        {
            if (state.state.name == name)
            {
                return state.state;
            }
        }
        AnimatorState newState = stateMachine.AddState(name);
        return newState;
    }

    private static void AddTransitionIfNotExists(AnimatorState from, AnimatorState to, AnimatorCondition[] conditions, bool hasExitTime)
    {
        if (from.transitions.Any(t => t.destinationState == to)) return;

        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = hasExitTime;
        transition.exitTime = hasExitTime ? 1.0f : 0.0f;
        transition.duration = 0.1f;
        foreach (var cond in conditions)
        {
            transition.AddCondition(cond.mode, cond.threshold, cond.parameter);
        }
    }

    private static void AddTransitionWithExitTimeIfNotExists(AnimatorState from, AnimatorState to)
    {
        if (from.transitions.Any(t => t.destinationState == to)) return;

        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = 0.85f; // transition near the end of attack
        transition.duration = 0.15f;
    }

    private static void AddAnyStateTransitionIfNotExists(AnimatorStateMachine stateMachine, AnimatorState to, AnimatorCondition[] conditions)
    {
        if (stateMachine.anyStateTransitions.Any(t => t.destinationState == to)) return;

        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.1f;
        foreach (var cond in conditions)
        {
            transition.AddCondition(cond.mode, cond.threshold, cond.parameter);
        }
    }

    private static void AssignClipsToStates(AnimatorState idleState, AnimatorState runState, AnimatorState attackState, AnimatorState dieState)
    {
        // Log all animation clips to help find the correct ones
        Debug.Log("--- START LISTING ALL ANIMATION CLIPS ---");
        
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("Assets/Models") && !path.Contains("Assets/Art")) continue;

            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in allAssets)
            {
                AnimationClip clip = asset as AnimationClip;
                if (clip == null) continue;
                if (clip.name.StartsWith("__preview__")) continue;

                Debug.Log($"Found Clip: '{clip.name}' in path: '{path}'");

                string clipName = clip.name.ToLower();
                string pathName = path.ToLower();

                // Explicit rule for chay_dibo_doi folder to ensure correct mapping of NlaTrack clips
                if (pathName.Contains("chay_dibo_doi"))
                {
                    if (clip.name.EndsWith("NlaTrack"))
                    {
                        runState.motion = clip;
                        EnableLoopTime(clip);
                        Debug.Log(">> Explicitly Assigned Run (chay) clip: " + clip.name + " (" + path + ")");
                    }
                    else if (clip.name.EndsWith("NlaTrack.002"))
                    {
                        idleState.motion = clip;
                        EnableLoopTime(clip);
                        Debug.Log(">> Explicitly Assigned Idle (doi) clip: " + clip.name + " (" + path + ")");
                    }
                    else if (clip.name.EndsWith("NlaTrack.001"))
                    {
                        Debug.Log(">> Found Walk (dibo) clip (skipped, not used as state): " + clip.name + " (" + path + ")");
                    }
                    continue;
                }

                // Check both clip name and file path for keywords
                bool isIdle = clipName.Contains("idle") || pathName.Contains("idle") || pathName.Contains("doi");
                bool isRun = clipName.Contains("run") || clipName.Contains("walk") || pathName.Contains("chay") || pathName.Contains("dibo");
                bool isAttack = clipName.Contains("attack") || clipName.Contains("hit") || clipName.Contains("slash");
                bool isDie = clipName.Contains("die") || clipName.Contains("death") || clipName.Contains("dead") || clipName.Contains("dying") || pathName.Contains("dying");

                if (isIdle && idleState.motion == null)
                {
                    idleState.motion = clip;
                    EnableLoopTime(clip);
                    Debug.Log(">> Assigned Idle clip: " + clip.name + " (" + path + ")");
                }
                else if (isRun && runState.motion == null)
                {
                    runState.motion = clip;
                    EnableLoopTime(clip);
                    Debug.Log(">> Assigned Run/Walk clip: " + clip.name + " (" + path + ")");
                }
                else if (isAttack && attackState.motion == null)
                {
                    attackState.motion = clip;
                    Debug.Log(">> Assigned Attack clip: " + clip.name + " (" + path + ")");
                }
                else if (isDie && dieState.motion == null)
                {
                    dieState.motion = clip;
                    Debug.Log(">> Assigned Die clip: " + clip.name + " (" + path + ")");
                }
            }
        }
        Debug.Log("--- END LISTING ALL ANIMATION CLIPS ---");
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
                                          pathLower.Contains("chay") || pathLower.Contains("dibo") || pathLower.Contains("doi");
                        
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
}
