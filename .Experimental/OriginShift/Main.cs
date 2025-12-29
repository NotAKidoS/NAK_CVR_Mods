#if !UNITY_EDITOR
using ABI_RC.Core.Util.AssetFiltering;
using MelonLoader;
using NAK.OriginShift.Components;
using NAK.OriginShiftMod.Integrations;
using OriginShift.Integrations;

namespace NAK.OriginShift;

// Links I looked at:
// Controller/Event Listener Setup: https://manuel-rauber.com/2022/04/06/floating-origin-in-unity/amp/
// Move Scene Roots: https://gist.github.com/brihernandez/9ebbaf35070181fa1ee56f9e702cc7a5
// Looked cool but didn't really find anything to use: https://docs.coherence.io/coherence-sdk-for-unity/world-origin-shifting
// One Day when we move to 2022: https://docs.unity3d.com/6000.0/Documentation/Manual/LightProbes-Moving.html

public class OriginShiftMod : MelonMod
{
    internal static MelonLogger.Instance Logger;
    internal static HarmonyLib.Harmony HarmonyInst;

    #region Melon Mod Overrides
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        HarmonyInst = HarmonyInstance;
        
        ModSettings.Initialize();
        
        ApplyPatches(typeof(Patches.BetterBetterCharacterControllerPatches)); // origin shift monitor
        ApplyPatches(typeof(Patches.CVRSpawnablePatches)); // components & remote spawnable pos
        
        // Compatibility Mode
        ApplyPatches(typeof(Patches.PlayerSetupPatches)); // net ik, camera occlusion culling
        ApplyPatches(typeof(Patches.CVRSyncHelperPatches)); // spawnable pos
        ApplyPatches(typeof(Patches.PuppetMasterPatches)); // remote player pos
        ApplyPatches(typeof(Patches.CVRObjectSyncPatches)); // remote object pos
        
        ApplyPatches(typeof(Patches.DbJobsAvatarManagerPatches)); // dynamic bones
        ApplyPatches(typeof(Patches.CVRPortalManagerPatches)); // portals
        ApplyPatches(typeof(Patches.RCC_SkidmarksManagerPatches)); // skidmarks
        ApplyPatches(typeof(Patches.CVRPickupObjectPatches)); // pickup object respawn height
        
        ApplyPatches(typeof(Patches.PortableCameraPatches)); // camera occlusion culling
        ApplyPatches(typeof(Patches.PathingCameraPatches)); // camera occlusion culling
        
        // add our components to the world whitelist
        WorldFilter._Base.Add(typeof(OriginShiftController)); // base component
        WorldFilter._Base.Add(typeof(OriginShiftEventReceiver)); // generic event listener
        
        WorldFilter._Base.Add(typeof(OriginShiftParticleSystemReceiver)); // particle system
        WorldFilter._Base.Add(typeof(OriginShiftRigidbodyReceiver)); // rigidbody
        WorldFilter._Base.Add(typeof(OriginShiftTrailRendererReceiver)); // trail renderer
        WorldFilter._Base.Add(typeof(OriginShiftTransformReceiver)); // transform
        
        // chunk controller
        WorldFilter._Base.Add(typeof(ChunkController));
        WorldFilter._Base.Add(typeof(ChunkListener));
        WorldFilter._Base.Add(typeof(ChunkCreator));
        
        InitializeIntegration("BTKUILib", BtkUiAddon.Initialize); // quick menu ui
        InitializeIntegration("ThirdPerson", ThirdPersonAddon.Initialize); // camera occlusion culling
        InitializeIntegration("PlayerRagdollMod", RagdollAddon.Initialize); // ragdoll rigidbodys
    }
    
    #endregion Melon Mod Overrides

    #region Melon Mod Utilities

    private static void InitializeIntegration(string modName, Action integrationAction)
    {
        if (RegisteredMelons.All(it => it.Info.Name != modName))
            return;

        Logger.Msg($"Initializing {modName} integration.");
        integrationAction.Invoke();
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

    #endregion Melon Mod Utilities
}
#endif