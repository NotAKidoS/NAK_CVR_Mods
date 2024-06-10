using System.Reflection;
using ABI_RC.Core.Util.AnimatorManager;
using ABI.CCK.Scripts;
using HarmonyLib;
using MelonLoader;

namespace NAK.AASDefaultProfileFix;

public class AASDefaultProfileFix : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(CVRAdvancedAvatarSettings).GetMethod(nameof(CVRAdvancedAvatarSettings.LoadProfile),
                BindingFlags.Public | BindingFlags.Instance), // earliest callback (why the fuck are you public)
            prefix: new HarmonyMethod(typeof(AASDefaultProfileFix).GetMethod(nameof(OnAttemptLoadAASProfile),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    // when default profile is selected the defaultProfileName string is empty
    // so it does not load/apply anything- we will fix by forcing LoadDefault when this is detected
    private static bool OnAttemptLoadAASProfile(string profileName, AvatarAnimatorManager animatorManager,
        ref CVRAdvancedAvatarSettings __instance)
    {
        if (!string.IsNullOrEmpty(profileName) // LoadDefault sets defaultProfileName as ""
            && profileName != "default") // CVRAdvancedSettingsFile defines initial default as "default"
            return true;

        __instance.LoadDefault(animatorManager);
        return false;
    }
}