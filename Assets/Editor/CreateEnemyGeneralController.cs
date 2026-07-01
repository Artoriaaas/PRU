using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class CreateEnemyGeneralController
{
    [MenuItem("Tools/PRU/Create Enemy General Controller")]
    public static void CreateController()
    {
        string path = "Assets/Art/Animations/QuanTuongDichAnimatorController.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // Add parameters
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        // Load animations
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Animations/KingClips/animation_idle_vua_animation_idle.anim");
        AnimationClip runClip = LoadFBXAnimationClip("Assets/Models/NewModel/medieval knight 3d model@Sword And Shield Run.fbx");
        AnimationClip attackClip = LoadFBXAnimationClip("Assets/Models/NewModel/medieval knight 3d model@Sword And Shield Slash.fbx");
        AnimationClip dieClip = LoadFBXAnimationClip("Assets/Models/NewModel/medieval knight 3d model@Sword And Shield Death.fbx");

        if (idleClip == null) Debug.LogWarning("CreateEnemyGeneralController: Could not load Idle animation clip!");
        if (runClip == null) Debug.LogWarning("CreateEnemyGeneralController: Could not load Run animation clip!");
        if (attackClip == null) Debug.LogWarning("CreateEnemyGeneralController: Could not load Attack animation clip!");
        if (dieClip == null) Debug.LogWarning("CreateEnemyGeneralController: Could not load Die animation clip!");

        // Get the root state machine
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Create states
        AnimatorState idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;

        AnimatorState runState = rootStateMachine.AddState("Run");
        runState.motion = runClip;

        AnimatorState attackState = rootStateMachine.AddState("Attack");
        attackState.motion = attackClip;

        AnimatorState dieState = rootStateMachine.AddState("Die");
        dieState.motion = dieClip;

        // Add transitions
        // Idle -> Run
        var transition = idleState.AddTransition(runState);
        transition.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        transition.hasExitTime = false;

        // Run -> Idle
        transition = runState.AddTransition(idleState);
        transition.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        transition.hasExitTime = false;

        // Any State -> Attack
        transition = rootStateMachine.AddAnyStateTransition(attackState);
        transition.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
        transition.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        transition.hasExitTime = false;

        // Attack -> Idle
        transition = attackState.AddTransition(idleState);
        transition.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
        transition.hasExitTime = true; // Wait for attack to finish

        // Any State -> Die
        transition = rootStateMachine.AddAnyStateTransition(dieState);
        transition.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        transition.AddCondition(AnimatorConditionMode.If, 0, "Die");
        transition.hasExitTime = false;

        AssetDatabase.SaveAssets();
        Debug.Log("Created Enemy General Controller successfully at " + path);
    }

    private static AnimationClip LoadFBXAnimationClip(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (var asset in assets)
        {
            if (asset is AnimationClip clip)
            {
                // Ignore the default __preview__ clips if any
                if (!clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
        }
        return null;
    }
}
