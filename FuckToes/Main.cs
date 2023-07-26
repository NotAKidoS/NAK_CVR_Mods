using ABI_RC.Core.Savior;
using ABI_RC.Systems.IK;
using MelonLoader;
using RootMotion.FinalIK;
using System.Reflection;

namespace NAK.FuckToes;

public class FuckToes : MelonMod
{
    internal static MelonLogger.Instance Logger;

    public static readonly MelonPreferences_Category Category =
        MelonPreferences.CreateCategory(nameof(FuckToes));

    public static readonly MelonPreferences_Entry<bool> EntryEnabledVR =
        Category.CreateEntry("Enabled in HalfBody", true, description: "Nuke VRIK toes when in Halfbody.");

    public static readonly MelonPreferences_Entry<bool> EntryEnabledFBT =
        Category.CreateEntry("Enabled in FBT", true, description: "Nuke VRIK toes when in FBT.");

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        HarmonyInstance.Patch(
            typeof(VRIK).GetMethod(nameof(VRIK.AutoDetectReferences)),
            prefix: new HarmonyLib.HarmonyMethod(typeof(FuckToes).GetMethod(nameof(OnVRIKAutoDetectReferences_Prefix), BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    private static void OnVRIKAutoDetectReferences_Prefix(ref VRIK __instance)
    {
        try
        {
            // Must be PlayerLocal layer and in VR
            if (__instance.gameObject.layer != 8 || !MetaPort.Instance.isUsingVr)
                return;

            // Not in FBT, and not enabled, perish
            if (!IKSystem.Instance.BodySystem.FBTActive() && !EntryEnabledVR.Value)
                return;

            // In FBT, and not enabled in fbt, perish
            if (IKSystem.Instance.BodySystem.FBTActive() && !EntryEnabledFBT.Value)
                return;

            __instance.references.leftToes = null;
            __instance.references.rightToes = null;
        }
        catch (Exception e)
        {
            Logger.Error($"Error during the patched method {nameof(OnVRIKAutoDetectReferences_Prefix)}");
            Logger.Error(e);
        }
    }
}