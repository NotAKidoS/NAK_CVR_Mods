using System.Reflection;
using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.UI;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NAK.FuckOffUICamera;

public class FuckOffUICameraMod : MelonMod
{
    private static MelonLogger.Instance Logger;
    
    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
    }
    
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (buildIndex != 2) return;
        if (_isInitialized) return;
        SetupShittyMod();
        _isInitialized = true;
    }
    
    private bool _isInitialized;

    private static void SetupShittyMod()
    {
        // Find all renderers under Cohtml object
        GameObject cohtml = GameObject.Find("Cohtml");
        if (cohtml == null)
        {
            Logger.Error("Cohtml object not found!");
            return;
        }
        
        // Find all CohtmlControlledView objects
        var allMenuCohtml = Object.FindObjectsOfType<CohtmlControlledView>(includeInactive: true);
        var allUiInternalRenderers = Object.FindObjectsOfType<Renderer>(includeInactive: true)
            .Where(x => x.gameObject.layer == CVRLayers.UIInternal)
            .ToArray();        
        
        //var allMenuRenderers = cohtml.GetComponentsInChildren<Renderer>(true);
        
        // Add hud renderer to the list of renderers
        Renderer hudRenderer = CohtmlHud.Instance.GetComponent<Renderer>();
        // Array.Resize(ref allMenuRenderers, allMenuRenderers.Length + 1);
        // allMenuRenderers[^1] = hudRenderer;
        
        // Fix shader on the hud renderer
        Material material = hudRenderer.sharedMaterial;
        material.shader = Shader.Find("Alpha Blend Interactive/MenuFX");
        
        // Setup command buffer manager for desktop camera
        CommandBufferManager.Setup(PlayerSetup.Instance.desktopCam, allUiInternalRenderers);
        CohtmlRenderForwarder.Setup(PlayerSetup.Instance.desktopCam, allMenuCohtml);
        
        // Setup command buffer manager for vr camera
        CommandBufferManager.Setup(PlayerSetup.Instance.vrCam, allUiInternalRenderers);
        CohtmlRenderForwarder.Setup(PlayerSetup.Instance.vrCam, allMenuCohtml);
        
        // Disable the ui cameras
        PlayerSetup.Instance.desktopUiCam.gameObject.SetActive(false);
        PlayerSetup.Instance.vrUiCam.gameObject.SetActive(false);
        
        Logger.Msg("Disabled UI cameras and setup command buffer manager for Cohtml renderers.");
    }
}