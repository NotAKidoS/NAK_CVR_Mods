using ABI_RC.Core.Savior;
using ABI_RC.Systems.IK;
using MelonLoader;
using RootMotion.FinalIK;
using System.Reflection;

namespace NAK.FuckToes;

public class FuckToesMod : MelonMod
{
    private static MelonLogger.Instance Logger;

    #region Melon Preferences

    private static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(FuckToesMod));

    private static readonly MelonPreferences_Entry<bool> EntryEnabledVR =
        Category.CreateEntry("Enabled in HalfBody", true, description: "Nuke VRIK toes when in Halfbody.");

    private static readonly MelonPreferences_Entry<bool> EntryEnabledFBT =
        Category.CreateEntry("Enabled in FBT", true, description: "Nuke VRIK toes when in FBT.");

    #endregion Melon Preferences

    #region Melon Events
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        HarmonyInstance.Patch(
            typeof(VRIK).GetMethod(nameof(VRIK.AutoDetectReferences)),
            prefix: new HarmonyLib.HarmonyMethod(typeof(FuckToesMod).GetMethod(nameof(OnVRIKAutoDetectReferences_Prefix), BindingFlags.NonPublic | BindingFlags.Static))
        );
    }
    
    #endregion Melon Events

    #region Harmony Patches
    
    private static void OnVRIKAutoDetectReferences_Prefix(ref VRIK __instance)
    {
        try
        {
            // Must be PlayerLocal layer and in VR
            if (__instance.gameObject.layer != 8 
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
            Logger.Error($"Error during the patched method {nameof(OnVRIKAutoDetectReferences_Prefix)}");
            Logger.Error(e);
        }
    }
    
    #endregion Harmony Patches
}