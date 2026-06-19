using UnityEngine;

namespace NAK.DehumanizePlayers;

public static class HumanoidAvatarRebinder
{
    private static readonly HashSet<Transform> s_BoneTransforms = new();
    private static readonly HashSet<string> s_HumanBoneNames = new();
    private static readonly List<(Transform t, string original)> s_Renames = new();
    private static readonly List<Transform> s_AllTransforms = new();

    public static Animator RebindToParentHumanoidAnimator(GameObject avatarGameObject)
    {
        if (avatarGameObject == null) return null;

        var parent = avatarGameObject.transform.parent;
        if (parent == null) return null;

        var innerAnimator = avatarGameObject.GetComponent<Animator>();
        if (innerAnimator == null || innerAnimator.avatar == null || !innerAnimator.avatar.isHuman)
        {
            DehumanizePlayersMod.Logger.Error("[HumanoidAvatarRebinder] avatarGameObject must have an Animator with a humanoid Avatar.");
            return null;
        }

        s_BoneTransforms.Clear();
        s_HumanBoneNames.Clear();
        s_Renames.Clear();
        s_AllTransforms.Clear();

        var humanDescription = innerAnimator.avatar.humanDescription;
        var sourceAvatarName = innerAnimator.avatar.name;
        
        var outerAnimator = parent.GetComponent<Animator>();
        if (outerAnimator) UnityEngine.Object.DestroyImmediate(outerAnimator);
        
        //if (outerAnimator == null)
        outerAnimator = parent.gameObject.AddComponent<Animator>();
        outerAnimator.enabled = false;
        outerAnimator.applyRootMotion = false;
        outerAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        outerAnimator.runtimeAnimatorController = null;

        for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
        {
            var bone = innerAnimator.GetBoneTransform((HumanBodyBones)i);
            if (bone != null) s_BoneTransforms.Add(bone);
        }
        
        // As we cannot rebind the inner animator, unassign now ...?
        // Cannot undo renames without breaking things, assuming need to wait a frame.
        innerAnimator.avatar = null;
        // innerAnimator.Rebind();

        if (humanDescription.skeleton != null && humanDescription.skeleton.Length > 0)
        {
            var expectedRootName = humanDescription.skeleton[0].name;
            if (!string.IsNullOrEmpty(expectedRootName) && avatarGameObject.name != expectedRootName)
            {
                s_Renames.Add((avatarGameObject.transform, avatarGameObject.name));
                avatarGameObject.name = expectedRootName;
            }
        }

        foreach (var bone in humanDescription.human)
            if (!string.IsNullOrEmpty(bone.boneName)) s_HumanBoneNames.Add(bone.boneName);

        parent.GetComponentsInChildren(true, s_AllTransforms);

        int renamed = 0;
        foreach (var t in s_AllTransforms)
        {
            if (s_HumanBoneNames.Contains(t.name) && !s_BoneTransforms.Contains(t))
            {
                s_Renames.Add((t, t.name));
                t.name = t.name + "__nb";
                renamed++;
            }
        }

        if (renamed > 0)
            DehumanizePlayersMod.Logger.Msg($"[HumanoidAvatarRebinder] Renamed {renamed} non-bone transforms.");

        var rebuiltAvatar = AvatarBuilder.BuildHumanAvatar(parent.gameObject, humanDescription);
        if (!rebuiltAvatar.isValid)
        {
            DehumanizePlayersMod.Logger.Error("[HumanoidAvatarRebinder] Rebuilt Avatar is invalid.");
            RevertRenames();
            return null;
        }
        rebuiltAvatar.name = sourceAvatarName + "_Rebound";
        outerAnimator.avatar = rebuiltAvatar;
        outerAnimator.Rebind();

        // RevertRenames();

        return outerAnimator;
    }

    private static void RevertRenames()
    {
        for (int i = s_Renames.Count - 1; i >= 0; i--)
        {
            var (t, original) = s_Renames[i];
            if (t != null) t.name = original;
        }
        s_Renames.Clear();
    }
}