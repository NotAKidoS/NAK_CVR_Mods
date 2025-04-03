using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ABI_RC.Core.Player.LocalClone;
using ABI_RC.Core.Player.TransformHider;
using ABI.CCK.Components;
using HarmonyLib;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NAK.VisualCloneFix;

public static class Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TransformHiderUtils), nameof(TransformHiderUtils.SetupAvatar))]
    private static bool OnSetupAvatar(GameObject avatar)
    {
        if (!VisualCloneFixMod.EntryUseVisualClone.Value) return true;
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

        // get all FPRExclusions
        var fprExclusions = root.GetComponentsInChildren<FPRExclusion>(true);

        // get all valid exclusion targets, and destroy invalid exclusions
        Dictionary<Transform, FPRExclusion> exclusionTargets = new();
        
        int nextId = 2;
        foreach (FPRExclusion exclusion in fprExclusions)
        {
            if (exclusion.target == null 
                || exclusionTargets.ContainsKey(exclusion.target) 
                || !exclusion.target.gameObject.scene.IsValid())
                continue; // invalid exclusion

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
        int exclusionCount = exclusionTargets.Values.Count;
        for (var index = 0; index < exclusionCount; index++)
        {
            FPRExclusion exclusion = exclusionTargets.Values.ElementAt(index);
            ProcessExclusion(exclusion, exclusion.target);
            exclusion.UpdateExclusions(); // initial state
        }

        __result = exclusionTargets;
        return false;
        
        void ProcessExclusion(FPRExclusion exclusion, Transform transform)
        {
            if (exclusionTargets.ContainsKey(transform)
                && exclusionTargets[transform] != exclusion) return; // found other exclusion root
            
            exclusionTargets.TryAdd(transform, exclusion); // add to the dictionary (yes its wasteful)
            foreach (Transform child in transform)
                ProcessExclusion(exclusion, child); // process children
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SkinnedLocalClone), nameof(SkinnedLocalClone.FindExclusionVertList))]
    private static bool FindExclusionVertList(
        SkinnedMeshRenderer renderer, IReadOnlyDictionary<Transform, FPRExclusion> exclusions,
        ref int[] __result)
    {
        // Start the stopwatch
        Stopwatch stopwatch = new();
        stopwatch.Start();

        var boneWeights = renderer.sharedMesh.boneWeights;
        var bones = renderer.bones;
        int boneCount = bones.Length;
        
        bool[] boneHasExclusion = new bool[boneCount];

        // Populate the weights array
        for (int i = 0; i < boneCount; i++)
        {
            Transform bone = bones[i];
            if (bone == null) continue;
            if (exclusions.ContainsKey(bone))
                boneHasExclusion[i] = true;
        }

        const float minWeightThreshold = 0.2f;

        int[] vertexIndices = new int[renderer.sharedMesh.vertexCount];
        
        // Check bone weights and add vertex to exclusion list if needed
        for (int i = 0; i < boneWeights.Length; i++)
        {
            BoneWeight weight = boneWeights[i];
            Transform bone;

            if (boneHasExclusion[weight.boneIndex0] && weight.weight0 > minWeightThreshold)
                bone = bones[weight.boneIndex0];
            else if (boneHasExclusion[weight.boneIndex1] && weight.weight1 > minWeightThreshold)
                bone = bones[weight.boneIndex1];
            else if (boneHasExclusion[weight.boneIndex2] && weight.weight2 > minWeightThreshold)
                bone = bones[weight.boneIndex2];
            else if (boneHasExclusion[weight.boneIndex3] && weight.weight3 > minWeightThreshold)
                bone = bones[weight.boneIndex3];
            else continue;

            if (exclusions.TryGetValue(bone, out FPRExclusion exclusion))
                vertexIndices[i] = ((MeshHiderExclusion)(exclusion.behaviour)).id;
        }

        // Stop the stopwatch
        stopwatch.Stop();

        // Log the execution time
        Debug.Log($"FindExclusionVertList execution time: {stopwatch.ElapsedMilliseconds} ms");
        
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