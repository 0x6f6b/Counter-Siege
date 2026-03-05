#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SceneDiagnostic
{
    public static string Execute()
    {
        var sb = new System.Text.StringBuilder();

        // Check animator controller asset
        string ctrlPath = "Assets/_Project/Animations/CharacterAnimator.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
        {
            sb.AppendLine("Controller not found");
            return sb.ToString();
        }

        sb.AppendLine($"Controller: {ctrl.name}");
        sb.AppendLine($"Parameters: {ctrl.parameters.Length}");
        foreach (var p in ctrl.parameters)
            sb.AppendLine($"  {p.name} ({p.type})");

        var rootSM = ctrl.layers[0].stateMachine;
        sb.AppendLine($"\nStates: {rootSM.states.Length}");
        foreach (var s in rootSM.states)
        {
            sb.AppendLine($"  {s.state.name} (hash={s.state.nameHash})");
            sb.AppendLine($"    Transitions: {s.state.transitions.Length}");
            foreach (var t in s.state.transitions)
            {
                string dest = t.destinationState != null ? t.destinationState.name : "null";
                sb.AppendLine($"    -> {dest} (hasExit={t.hasExitTime}, dur={t.duration:F2})");
                foreach (var c in t.conditions)
                    sb.AppendLine($"       Condition: {c.parameter} {c.mode} {c.threshold}");
            }
        }

        // Runtime check
        var animator = Object.FindFirstObjectByType<Animator>();
        if (animator != null)
        {
            sb.AppendLine($"\nRuntime WeaponType: {animator.GetInteger("WeaponType")}");
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            sb.AppendLine($"Current state hash: {stateInfo.shortNameHash}");

            // Check specific state hashes
            sb.AppendLine($"Rifle_Locomotion hash: {Animator.StringToHash("Rifle_Locomotion")}");
            sb.AppendLine($"Pistol_Locomotion hash: {Animator.StringToHash("Pistol_Locomotion")}");
        }

        return sb.ToString();
    }
}
#endif
