using ABI_RC.Core;
using MelonLoader;
using UnityEngine;

namespace NAK.AvatarCloneTest;

public class AvatarCloneTestMod : MelonMod
{
    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(AvatarCloneTest));

    internal static readonly MelonPreferences_Entry<bool> EntryUseAvatarCloneTest =
        Category.CreateEntry("use_avatar_clone_test", true,
            "Use Avatar Clone", description: "Uses the Avatar Clone setup for the local avatar.");
    
    // internal static readonly MelonPreferences_Entry<bool> EntryCopyBlendShapes =
    //     Category.CreateEntry("copy_blend_shapes", true,
    //         "Copy Blend Shapes", description: "Copies the blend shapes from the original avatar to the clone.");
    // 
    // internal static readonly MelonPreferences_Entry<bool> EntryCopyMaterials =
    //     Category.CreateEntry("copy_materials", true,
    //         "Copy Materials", description: "Copies the materials from the original avatar to the clone.");
    // 
    // internal static readonly MelonPreferences_Entry<bool> EntryCopyMeshes =
    //     Category.CreateEntry("copy_meshes", true,
    //         "Copy Meshes", description: "Copies the meshes from the original avatar to the clone.");
    
    #endregion Melon Preferences

    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(Patches)); // slapped together a fix cause HarmonyInstance.Patch was null ref for no reason?
    }

    public override void OnUpdate()
    {
        // press f1 to find all cameras that arent tagged main and set them tno not render CVRLayers.PlayerClone
        if (Input.GetKeyDown(KeyCode.F1))
        {
            foreach (var camera in UnityEngine.Object.FindObjectsOfType<UnityEngine.Camera>())
            {
                if (camera.tag != "MainCamera")
                {
                    camera.cullingMask &= ~(1 << CVRLayers.PlayerClone);
                }
            }
        }
    }
    
    #endregion Melon Events

    #region Melon Mod Utilities

    private void ApplyPatches(Type type)
    {
        try
        {
            HarmonyInstance.PatchAll(type);
        }
        catch (Exception e)
        {
            LoggerInstance.Msg($"Failed while patching {type.Name}!");
            LoggerInstance.Error(e);
        }
    }
    
    #endregion Melon Mod Utilities
}