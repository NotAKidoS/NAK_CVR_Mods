using MelonLoader;

namespace NAK.SmoothRay;

// ChilloutVR adaptation of:
// https://github.com/kinsi55/BeatSaber_SmoothedController
// https://github.com/kinsi55/BeatSaber_SmoothedController/blob/master/LICENSE

public class SmoothRay : MelonMod
{
    public override void OnInitializeMelon()
    {
        ApplyPatches(typeof(HarmonyPatches.PlayerSetupPatches));
    }

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
}