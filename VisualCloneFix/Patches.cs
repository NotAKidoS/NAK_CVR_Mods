using ABI_RC.Core.Player.LocalClone;
using ABI_RC.Core.Player.TransformHider;
using ABI.CCK.Components;
using HarmonyLib;
using UnityEngine;

namespace NAK.VisualCloneFix;

public static class Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TransformHiderUtils), nameof(TransformHiderUtils.SetupAvatar))]
    private static bool OnSetupAvatar(GameObject avatar)
    {
        if (!VisualCloneFixMod.EntryUseVisualClone.Value)
            return true;

        LocalCloneHelper.SetupAvatar(avatar);
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(LocalCloneHelper), nameof(LocalCloneHelper.CollectTransformToExclusionMap))]
    private static bool CollectTransformToExclusionMap(
        Component root, Transform headBone, 
        ref Dictionary<Transform, FPRExclusion> __result)
    {
        // add an fpr exclusion to the head bone
        if (!headBone.TryGetComponent(out FPRExclusion headExclusion))
        {
            headExclusion = headBone.gameObject.AddComponent<FPRExclusion>();
            headExclusion.isShown = false; // default to hidden
            headExclusion.target = headBone;
        }
        
        MeshHiderExclusion headExclusionBehaviour = new();
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
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SkinnedLocalClone), nameof(SkinnedLocalClone.FindExclusionVertList))]
    public static bool FindExclusionVertList(
        SkinnedMeshRenderer renderer, IReadOnlyDictionary<Transform, FPRExclusion> exclusions,
        ref int[] __result) 
    {
        Mesh sharedMesh = renderer.sharedMesh;
        var boneWeights = sharedMesh.boneWeights;
        int[] vertexIndices = new int[sharedMesh.vertexCount];
    
        // Pre-map bone transforms to their exclusion ids if applicable
        var bones = renderer.bones;
        int[] boneIndexToExclusionId = new int[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            Transform bone = bones[i];
            if (bone != null && exclusions.TryGetValue(bone, out FPRExclusion exclusion))
                boneIndexToExclusionId[i] = ((MeshHiderExclusion)(exclusion.behaviour)).id;
        }
        
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MeshHiderExclusion), nameof(MeshHiderExclusion.UpdateExclusions))]
    private static bool OnUpdateExclusions(bool isShown, bool shrinkToZero, ref int ___id)
    {
        if (isShown) LocalCloneManager.cullingMask &= ~(1 << ___id);
        else LocalCloneManager.cullingMask |= 1 << ___id;
        return false;
    }
}