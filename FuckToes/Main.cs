using MelonLoader;

namespace NAK.Melons.FuckToes;

public class FuckToesMod : MelonMod
{
    internal const string SettingsCategory = "Fuck Toes";
    internal static MelonPreferences_Category m_categoryFuckToes;
    internal static MelonPreferences_Entry<bool> m_entryEnabledVR, m_entryEnabledFBT;
    public override void OnInitializeMelon()
    {
        m_categoryFuckToes = MelonPreferences.CreateCategory(SettingsCategory);
        m_entryEnabledVR = m_categoryFuckToes.CreateEntry<bool>("Enabled", true, description: "Nuke VRIK toes when in Halfbody.");
        m_entryEnabledFBT = m_categoryFuckToes.CreateEntry<bool>("Enabled in FBT", false, description: "Nuke VRIK toes when in FBT.");

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