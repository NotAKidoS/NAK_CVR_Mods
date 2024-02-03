using System;
using System.Collections.Generic;
using ABI_RC.Core;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NAK.BetterShadowClone;

public static class ShadowCloneHelper
{
    public static ComputeShader shader;
    public static Material shadowMaterial;
    
    #region Avatar Setup
    
    public static void SetupAvatar(GameObject avatar)
    {
        Animator animator = avatar.GetComponent<Animator>();
        if (animator == null || animator.avatar == null || animator.avatar.isHuman == false)
        {
            ShadowCloneMod.Logger.Warning("Avatar is not humanoid!");
            return;
        }
        
        Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        if (headBone == null)
        {
            ShadowCloneMod.Logger.Warning("Head bone not found!");
            return;
        }
        
        var renderers = avatar.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            ShadowCloneMod.Logger.Warning("No renderers found!");
            return;
        }
        
        // create shadow clones
        ProcessRenderers(renderers, avatar.transform, headBone);
    }
    
    private static void ProcessRenderers(IEnumerable<Renderer> renderers, Transform root, Transform headBone)
    {
        var exclusions = CollectExclusions2(root, headBone);
        
        foreach (Renderer renderer in renderers)
        {
            ConfigureRenderer(renderer);

            if (ModSettings.EntryUseShadowClone.Value)
            {
                IShadowClone clone = ShadowCloneManager.CreateShadowClone(renderer);
                if (clone != null) ShadowCloneManager.Instance.AddShadowClone(clone);
            }
            
            TransformHiderManager.CreateTransformHider(renderer, exclusions);
        }
    }
    
    #endregion
    
    #region FPR Exclusion Processing
    
    private static Dictionary<Transform, FPRExclusion> CollectExclusions2(Transform root, Transform headBone)
    {
        // add an fpr exclusion to the head bone
        headBone.gameObject.AddComponent<FPRExclusion>().target = headBone;
        
        // get all FPRExclusions
        var fprExclusions = root.GetComponentsInChildren<FPRExclusion>(true).ToList();

        // get all valid exclusion targets, and destroy invalid exclusions
        Dictionary<Transform, FPRExclusion> exclusionTargetRoots = new();
        for (int i = fprExclusions.Count - 1; i >= 0; i--)
        {
            FPRExclusion exclusion = fprExclusions[i];
            if (exclusion.target == null)
            {
                Object.Destroy(exclusion);
                continue;
            }
            
            exclusionTargetRoots.Add(exclusion.target, exclusion);
        }

        // process each FPRExclusion (recursive)
        foreach (FPRExclusion exclusion in fprExclusions)
            ProcessExclusion(exclusion, exclusion.target);
        
        // log totals
        ShadowCloneMod.Logger.Msg($"Exclusions: {fprExclusions.Count}");
        return exclusionTargetRoots;

        void ProcessExclusion(FPRExclusion exclusion, Transform transform)
        {
            if (exclusionTargetRoots.ContainsKey(transform)
                && exclusionTargetRoots[transform] != exclusion) return; // found other exclusion root
            
            exclusion.affectedChildren.Add(transform); // associate with the exclusion
            
            foreach (Transform child in transform)
                ProcessExclusion(exclusion, child); // process children
        }
    }

    #endregion
    
    #region Generic Renderer Configuration

    internal static void ConfigureRenderer(Renderer renderer, bool isShadowClone = false)
    {
        // generic optimizations
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        
        // don't let visual/shadow mesh cull in weird worlds
        renderer.allowOcclusionWhenDynamic = false; // (third person stripped local player naked when camera was slightly occluded)
        
        // shadow clone optimizations (always MeshRenderer)
        if (isShadowClone)
        {
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return;
        }
        
        if (renderer is not SkinnedMeshRenderer skinnedMeshRenderer) 
            return;

        // GraphicsBuffer becomes stale randomly otherwise ???
        //skinnedMeshRenderer.updateWhenOffscreen = true;
        
        // skin mesh renderer optimizations
        skinnedMeshRenderer.skinnedMotionVectors = false;
        skinnedMeshRenderer.forceMatrixRecalculationPerRender = false; // expensive
        skinnedMeshRenderer.quality = SkinQuality.Bone4;
    }

    #endregion
}