using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// Creates a standalone Animator Controller for the enemy general,
/// following the same approach as the player king:
/// - Extract standalone .anim clips from the FBX (with path normalisation)
/// - Assign those extracted clips to the controller states
/// This avoids the T-pose caused by using embedded FBX clips directly.
/// </summary>
public class CreateEnemyGeneralController
{
    [MenuItem("Tools/PRU/Create Enemy General Controller")]
    public static void CreateController()
    {
        string enemyFBXPath = "Assets/Models/NewModel/Model quân địch/model_tuong_quan_dich/animation_tuong_quan_dich.fbx";
        string outputDir    = "Assets/Art/Animations/EnemyGeneralClips";
        string controllerPath = "Assets/Art/Animations/QuanTuongDichAnimatorController.controller";

        // ---- 1. Extract standalone .anim clips (same approach as King) ----
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }
        Directory.CreateDirectory(outputDir);
        AssetDatabase.Refresh();

        Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(enemyFBXPath);
        if (subAssets == null || subAssets.Length == 0)
        {
            Debug.LogError("CreateEnemyGeneralController: No sub-assets found in " + enemyFBXPath);
            return;
        }

        foreach (Object asset in subAssets)
        {
            AnimationClip sourceClip = asset as AnimationClip;
            if (sourceClip == null || sourceClip.name.StartsWith("__preview__")) continue;

            AnimationClip destClip = new AnimationClip();

            // Copy float curves – normalise any "Armature.xxx" path variants
            foreach (var binding in AnimationUtility.GetCurveBindings(sourceClip))
            {
                var newBinding = NormalisePath(binding);

                // Strip root rotation to prevent tilting.
                // IMPORTANT: Only strip the Armature empty-parent transform, NOT mixamorig:Hips!
                // mixamorig:Hips is the functional root bone of the skeleton — stripping its
                // rotation keeps the entire skeleton at bind-pose (T-pose).
                bool isRoot = newBinding.path == "" || newBinding.path == "Armature";
                if (isRoot && newBinding.propertyName.Contains("m_LocalRotation")) continue;

                // Strip X/Z root translation (root motion)
                if (isRoot && (newBinding.propertyName.Contains("m_LocalPosition.x") || newBinding.propertyName.Contains("m_LocalPosition.z"))) continue;

                AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                AnimationUtility.SetEditorCurve(destClip, newBinding, curve);
            }

            // Copy object-reference curves
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(sourceClip))
            {
                var newBinding = NormalisePath(binding);
                bool isRoot = newBinding.path == "" || newBinding.path == "Armature";
                if (isRoot && newBinding.propertyName.Contains("m_LocalRotation")) continue;
                if (isRoot && (newBinding.propertyName.Contains("m_LocalPosition.x") || newBinding.propertyName.Contains("m_LocalPosition.z"))) continue;

                var keyframes = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
                AnimationUtility.SetObjectReferenceCurve(destClip, newBinding, keyframes);
            }

            // Copy clip settings (loop, wrap mode, etc.)
            var settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
            AnimationUtility.SetAnimationClipSettings(destClip, settings);

            string safeName = sourceClip.name.Replace('|', '_').Replace('.', '_');
            string outPath  = outputDir + "/" + safeName + ".anim";
            AssetDatabase.CreateAsset(destClip, outPath);
            Debug.Log("CreateEnemyGeneralController: Extracted clip → " + outPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ---- 2. Load extracted clips ----
        AnimationClip idleClip   = null;
        AnimationClip runClip    = null;
        AnimationClip attackClip = null;
        AnimationClip dieClip    = null;

        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new string[] { outputDir });
        foreach (string guid in guids)
        {
            string clipPath = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) continue;

            string n = clip.name.ToLower();

            if (n.Contains("idle")   && idleClip   == null) { idleClip   = clip; EnableLoop(clip); Debug.Log(">> Enemy General Idle: "   + clip.name); }
            else if (n.Contains("run")    && runClip    == null) { runClip    = clip; EnableLoop(clip); Debug.Log(">> Enemy General Run: "    + clip.name); }
            else if (n.Contains("attack") && !n.Contains("combo") && !n.Contains("attack_1") && attackClip == null) { attackClip = clip; Debug.Log(">> Enemy General Attack: " + clip.name); }
            else if ((n.Contains("dying") || n.Contains("death") || n.Contains("die")) && dieClip == null) { dieClip = clip; Debug.Log(">> Enemy General Die: " + clip.name); }
        }

        if (idleClip   == null) Debug.LogWarning("CreateEnemyGeneralController: Could not load Idle clip from " + outputDir);
        if (runClip    == null) Debug.LogWarning("CreateEnemyGeneralController: Could not load Run clip from "  + outputDir);
        if (attackClip == null) Debug.LogWarning("CreateEnemyGeneralController: Could not load Attack clip from " + outputDir);
        if (dieClip    == null) Debug.LogWarning("CreateEnemyGeneralController: Could not load Die clip from "  + outputDir);

        // ---- 3. Build Animator Controller ----
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        controller.AddParameter("IsMoving",    AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead",      AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack",      AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die",         AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState idleState   = sm.AddState("Idle");   idleState.motion   = idleClip;
        AnimatorState runState    = sm.AddState("Run");    runState.motion    = runClip;
        AnimatorState attackState = sm.AddState("Attack"); attackState.motion = attackClip;
        AnimatorState dieState    = sm.AddState("Die");    dieState.motion    = dieClip;

        sm.defaultState = idleState;

        // Idle <-> Run
        var t = idleState.AddTransition(runState);
        t.hasExitTime = false;
        t.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");

        t = runState.AddTransition(idleState);
        t.hasExitTime = false;
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

        // Any -> Attack
        t = sm.AddAnyStateTransition(attackState);
        t.hasExitTime = false;
        t.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        // Attack -> Idle (with exit time)
        t = attackState.AddTransition(idleState);
        t.hasExitTime = true;
        t.exitTime    = 0.9f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

        // Any -> Die
        t = sm.AddAnyStateTransition(dieState);
        t.hasExitTime = false;
        t.AddCondition(AnimatorConditionMode.If, 0, "IsDead");

        t = sm.AddAnyStateTransition(dieState);
        t.hasExitTime = false;
        t.AddCondition(AnimatorConditionMode.If, 0, "Die");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("CreateEnemyGeneralController: Done! Controller saved to " + controllerPath);
    }

    private static EditorCurveBinding NormalisePath(EditorCurveBinding b)
    {
        b.path = b.path
            .Replace("Armature.001/", "Armature/")
            .Replace("Armature.002/", "Armature/")
            .Replace("Armature.003/", "Armature/")
            .Replace("Armature.004/", "Armature/");

        if (b.path.StartsWith("Armature.001")) b.path = "Armature" + b.path.Substring(12);
        if (b.path.StartsWith("Armature.002")) b.path = "Armature" + b.path.Substring(12);
        if (b.path.StartsWith("Armature.003")) b.path = "Armature" + b.path.Substring(12);
        if (b.path.StartsWith("Armature.004")) b.path = "Armature" + b.path.Substring(12);

        return b;
    }

    private static void EnableLoop(AnimationClip clip)
    {
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }
}
