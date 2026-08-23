using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public static class B3AnimatorSetup
{
    const string ControllerPath = "Assets/Art/player/B3_Monkey/Materials/B3_Monkey.controller";

    [MenuItem("Tools/B3 Validator/Setup B3 Animator Automatically")]
    public static void Setup()
    {
        GameObject root = Selection.activeGameObject;
        if (!root) { Debug.LogError("[B3 Animator] Select Monkey_B3 (1) first."); return; }

        string fbx = AssetDatabase.FindAssets("Monkey_B3 t:Model")
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(x => x.EndsWith("Monkey_B3.fbx", System.StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(fbx)) { Debug.LogError("[B3 Animator] Monkey_B3.fbx not found."); return; }

        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(fbx)
            .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
        if (clips.Length == 0) { Debug.LogError("[B3 Animator] No animation clips found."); return; }

        Animator animator = root.GetComponent<Animator>();
        if (!animator) animator = root.AddComponent<Animator>();

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (!controller)
        {
            EnsureFolder("Assets/Art/player/B3_Monkey/Materials");
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        controller.layers = new AnimatorControllerLayer[0];
        controller.AddLayer("B3 Locomotion");
        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimationClip idle = Find(clips, "Idle", "Idle2");
        AnimationClip run = Find(clips, "Run", "RunR");
        AnimationClip jump = Find(clips, "Jump", "Jump2", "Jump3");
        AnimationClip eat = Find(clips, "Eat");
        AnimationClip roar = Find(clips, "Roar", "Roar2");
        AnimationClip die = Find(clips, "Die");

        if (!idle) { Debug.LogError("[B3 Animator] Idle clip not found."); return; }

        AnimatorState sIdle = State(sm, "Idle", idle);
        AnimatorState sRun = State(sm, "Run", run);
        AnimatorState sJump = State(sm, "Jump", jump);
        AnimatorState sEat = State(sm, "Eat", eat);
        AnimatorState sRoar = State(sm, "Roar", roar);
        AnimatorState sDie = State(sm, "Die", die);
        sm.defaultState = sIdle;

        AddParam(controller, "Speed", AnimatorControllerParameterType.Float);
        AddParam(controller, "Jump", AnimatorControllerParameterType.Trigger);
        AddParam(controller, "Attack", AnimatorControllerParameterType.Trigger);
        AddParam(controller, "Die", AnimatorControllerParameterType.Trigger);

        if (sRun)
        {
            var t = sIdle.AddTransition(sRun); t.hasExitTime=false;
            t.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");
            t = sRun.AddTransition(sIdle); t.hasExitTime=false;
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        }
        if (sJump) { var t=sIdle.AddTransition(sJump); t.hasExitTime=false; t.AddCondition(AnimatorConditionMode.If, 0f, "Jump"); }
        if (sEat) { var t=sIdle.AddTransition(sEat); t.hasExitTime=false; t.AddCondition(AnimatorConditionMode.If, 0f, "Attack"); }
        if (sRoar) { var t=sIdle.AddTransition(sRoar); t.hasExitTime=false; t.AddCondition(AnimatorConditionMode.If, 0f, "Attack"); }
        if (sDie) { var t=sIdle.AddTransition(sDie); t.hasExitTime=false; t.AddCondition(AnimatorConditionMode.If, 0f, "Die"); }

        AssetDatabase.SaveAssets();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        EditorUtility.SetDirty(root);
        if (root.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);

        Debug.Log("[B3 Animator] COMPLETE. Clips=" + clips.Length +
            ", Idle=" + idle.name + ", Run=" + Name(run) + ", Jump=" + Name(jump) +
            ", Eat=" + Name(eat) + ", Roar=" + Name(roar) + ", Die=" + Name(die));
    }

    static AnimatorState State(AnimatorStateMachine sm,string n,AnimationClip c)
    { if(!c)return null; var s=sm.AddState(n); s.motion=c; return s; }

    static void AddParam(AnimatorController c,string n,AnimatorControllerParameterType t)
    { if(!c.parameters.Any(p=>p.name==n)) c.AddParameter(n,t); }

    static AnimationClip Find(AnimationClip[] cs,params string[] ns)
    {
        foreach(var n in ns){var x=cs.FirstOrDefault(c=>c.name.Equals(n,System.StringComparison.OrdinalIgnoreCase));if(x)return x;}
        foreach(var n in ns){var x=cs.FirstOrDefault(c=>c.name.IndexOf(n,System.StringComparison.OrdinalIgnoreCase)>=0);if(x)return x;}
        return null;
    }

    static string Name(AnimationClip c)=>c?c.name:"NOT FOUND";

    static void EnsureFolder(string path)
    {
        var a=path.Split('/'); var cur=a[0];
        for(int i=1;i<a.Length;i++){var next=cur+"/"+a[i];if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(cur,a[i]);cur=next;}
    }
}
