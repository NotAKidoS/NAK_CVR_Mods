using System.Reflection;
using ABI_RC.Core.Player.LocalClone;
using ABI_RC.Core.Player.TransformHider;
using ABI.CCK.Components;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.VisualCloneFix;

public class VisualCloneFixMod : MelonMod
{
    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(VisualCloneFix));

    private static readonly MelonPreferences_Entry<bool> EntryUseVisualClone =
        Category.CreateEntry("use_visual_clone", true,
            "Use Visual Clone", description: "Uses the potentially faster Visual Clone setup for the local avatar.");
    
    #endregion Melon Preferences

    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch( // add option back as melonpref
            typeof(TransformHiderUtils).GetMethod(nameof(TransformHiderUtils.SetupAvatar),
                BindingFlags.Public | BindingFlags.Static),
            prefix: new HarmonyMethod(typeof(VisualCloneFixMod).GetMethod(nameof(OnSetupAvatar),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch( // fix binding of ids to exclusions
            typeof(LocalCloneHelper).GetMethod(nameof(LocalCloneHelper.CollectTransformToExclusionMap),
                BindingFlags.NonPublic | BindingFlags.Static),
            prefix: new HarmonyMethod(typeof(VisualCloneFixMod).GetMethod(nameof(CollectTransformToExclusionMap),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
        
        HarmonyInstance.Patch( // fix binding of exclusion ids to compute buffer
            typeof(SkinnedLocalClone).GetMethod(nameof(SkinnedLocalClone.FindExclusionVertList),
                BindingFlags.Public | BindingFlags.Static),
            prefix: new HarmonyMethod(typeof(VisualCloneFixMod).GetMethod(nameof(FindExclusionVertList),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    #endregion Melon Events

    #region Transform Hider Setup

    private static bool OnSetupAvatar(GameObject avatar)
    {
        if (!EntryUseVisualClone.Value)
            return true;

        LocalCloneHelper.SetupAvatar(avatar);
        return false;
    }
    
    #endregion Transform Hider Setup
    
    #region FPR Exclusion Processing
    
    private static bool CollectTransformToExclusionMap(
        Component root, Transform headBone, 
        ref Dictionary<Transform, FPRExclusion> __result)
    {
        // add an fpr exclusion to the head bone
        FPRExclusion headExclusion = headBone.gameObject.AddComponent<FPRExclusion>();
        MeshHiderExclusion headExclusionBehaviour = new();
        headExclusion.target = headBone;
        headExclusion.behaviour = headExclusionBehaviour;
        headExclusionBehaviour.id = 1; // head bone is always 1
        
        // body is 0, ensure it is not masked away
        //LocalCloneManager.cullingMask &= ~(1 << 0);
        
        // get all FPRExclusions
        var fprExclusions = root.GetComponentsInChildren<FPRExclusion>(true).ToList();

        // get all valid exclusion targets, and destroy invalid exclusions
        Dictionary<Transform, FPRExclusion> exclusionTargets = new();
        
        int nextId = 2;
        for (int i = fprExclusions.Count - 1; i >= 0; i--)
        {
            FPRExclusion exclusion = fprExclusions[i];
            if (exclusion.target == null 
                || exclusionTargets.ContainsKey(exclusion.target) 
                || !exclusion.target.gameObject.scene.IsValid())
            {
                UnityEngine.Object.Destroy(exclusion);
                fprExclusions.RemoveAt(i);
                continue;
            }

            if (exclusion.behaviour == null) // head exclusion is already created
            {
                MeshHiderExclusion meshHiderExclusion = new();
                exclusion.behaviour = meshHiderExclusion;
                meshHiderExclusion.id = nextId++;
            }
            
            // first to add wins
            exclusionTargets.TryAdd(exclusion.target, exclusion);
        }
        
        // process each FPRExclusion (recursive)
        foreach (FPRExclusion exclusion in fprExclusions)
        {
            ProcessExclusion(exclusion, exclusion.target);
            exclusion.UpdateExclusions(); // initial state
        }
        
        // log totals
        //LocalCloneMod.Logger.Msg($"Exclusions: {fprExclusions.Count}");
        __result = exclusionTargets;
        return false;

        void ProcessExclusion(FPRExclusion exclusion, Transform transform)
        {
            if (exclusionTargets.ContainsKey(transform)
                && exclusionTargets[transform] != exclusion) return; // found other exclusion root
            
            //exclusion.affectedChildren.Add(transform); // associate with the exclusion
            exclusionTargets.TryAdd(transform, exclusion); // add to the dictionary (yes its wasteful)
            
            foreach (Transform child in transform)
                ProcessExclusion(exclusion, child); // process children
        }
    }

    #endregion 
    
    #region Head Hiding Methods
    
    public static bool FindExclusionVertList(
        SkinnedMeshRenderer renderer, IReadOnlyDictionary<Transform, FPRExclusion> exclusions,
        ref int[] __result) 
    {
        Mesh sharedMesh = renderer.sharedMesh;
        var boneWeights = sharedMesh.boneWeights;
        int[] vertexIndices = new int[sharedMesh.vertexCount];
    
        // Pre-map bone transforms to their exclusion ids if applicable
        int[] boneIndexToExclusionId = new int[renderer.bones.Length];
        for (int i = 0; i < renderer.bones.Length; i++)
            if (exclusions.TryGetValue(renderer.bones[i], out FPRExclusion exclusion))
                boneIndexToExclusionId[i] = ((MeshHiderExclusion)exclusion.behaviour).id;
        
        const float minWeightThreshold = 0.2f;
        for (int i = 0; i < boneWeights.Length; i++) 
        {
            BoneWeight weight = boneWeights[i];
            
            if (weight.weight0 > minWeightThreshold)
                vertexIndices[i] = boneIndexToExclusionId[weight.boneIndex0];
            else if (weight.weight1 > minWeightThreshold)
                vertexIndices[i] = boneIndexToExclusionId[weight.boneIndex1];
            else if (weight.weight2 > minWeightThreshold)
                vertexIndices[i] = boneIndexToExclusionId[weight.boneIndex2];
            else if (weight.weight3 > minWeightThreshold)
                vertexIndices[i] = boneIndexToExclusionId[weight.boneIndex3];
        }

        __result = vertexIndices;
        return false;
    }

    #endregion Head Hiding Methods
}