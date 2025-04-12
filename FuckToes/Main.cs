using ABI_RC.Core.Savior;
using ABI_RC.Systems.IK;
using MelonLoader;
using RootMotion.FinalIK;
using System.Reflection;
using ABI_RC.Core;

namespace NAK.FuckToes;

public class FuckToesMod : MelonMod
{
    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(FuckToes));

    private static readonly MelonPreferences_Entry<bool> EntryEnabledVR =
        Category.CreateEntry("use_in_halfbody", true, display_name:"No Toes in Halfbody", description: "Nuke VRIK toes when in Halfbody.");

    private static readonly MelonPreferences_Entry<bool> EntryEnabledFBT =
        Category.CreateEntry("use_in_fbt", true, display_name:"No Toes in Fullbody", description: "Nuke VRIK toes when in FBT.");

    #endregion Melon Preferences

    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(VRIK).GetMethod(nameof(VRIK.AutoDetectReferences)),
            prefix: new HarmonyLib.HarmonyMethod(typeof(FuckToesMod).GetMethod(nameof(OnVRIKAutoDetectReferences_Prefix), 
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    #endregion Melon Events

    #region Harmony Patches
    
    private static void OnVRIKAutoDetectReferences_Prefix(ref VRIK __instance)
    {
        try
        {
            // Must be PlayerLocal layer and in VR
            if (__instance.gameObject.layer != CVRLayers.PlayerLocal
                || !MetaPort.Instance.isUsingVr)
                return;

            switch (IKSystem.Instance.BodySystem.FullBodyActive)
            {
                case false when !EntryEnabledVR.Value: // Not in FBT, and not enabled, perish
                case true when !EntryEnabledFBT.Value: // In FBT, and not enabled in fbt, perish
                    return;
                default:
                    __instance.references.leftToes = null;
                    __instance.references.rightToes = null;
                    break;
            }
        }
        catch (Exception e)
        {
            MelonLogger.Error($"Error during the patched method {nameof(OnVRIKAutoDetectReferences_Prefix)}");
            MelonLogger.Error(e);
        }
    }
    
    #endregion Harmony Patches
}