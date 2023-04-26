using MelonLoader;

namespace NAK.FuckToes;

public class FuckToes : MelonMod
{
    public static readonly MelonPreferences_Category Category = 
        MelonPreferences.CreateCategory(nameof(FuckToes));

    public static readonly MelonPreferences_Entry<bool> EntryEnabledVR = 
        Category.CreateEntry("Enabled", true, description: "Nuke VRIK toes when in Halfbody.");

    public static readonly MelonPreferences_Entry<bool> EntryEnabledFBT = 
        Category.CreateEntry("Enabled in FBT", false, description: "Nuke VRIK toes when in FBT.");

    public override void OnInitializeMelon()
    {
        //Apply patches (i stole)
        ApplyPatches(typeof(HarmonyPatches.VRIKPatches));
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