using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace NAK.FuckOffUICamera;

public class CommandBufferManager : MonoBehaviour
{
    #region Private Variables
    
    private CommandBuffer commandBuffer;
    private Camera targetCamera;
    private Renderer[] targetRenderers;
    private bool[] rendererEnabledStates;
    private const string CommandBufferName = "CustomRenderPass";
    private bool _didSetup;
    
    #endregion Private Variables

    #region Unity Events
    
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(2f); // I have no idea why this needs to be delayed
        _didSetup = true;
        OnEnable();
    }
    
    private void OnEnable()
    {
        if (!_didSetup) return;
        if (targetCamera == null || targetRenderers == null) 
            return;
        
        SetupEnabledStateCollection();
        SetupCommandBuffer();
    }
    
    private void OnDisable()
    {
        CleanupCommandBuffer();
    }

    private void LateUpdate()
    {
        if (targetRenderers == null 
            || rendererEnabledStates == null) 
            return;

        bool needsRebuild = false;
        
        // Check if any renderer enabled states have changed
        int targetRenderersLength = targetRenderers.Length;
        for (int i = 0; i < targetRenderersLength; i++)
        {
            if (targetRenderers[i] == null) continue;
            
            bool currentState = targetRenderers[i].enabled && targetRenderers[i].gameObject.activeInHierarchy;
            if (currentState == rendererEnabledStates[i]) 
                continue;
            
            rendererEnabledStates[i] = currentState;
            needsRebuild = true;
        }

        if (needsRebuild) RebuildCommandBuffer();
    }
    
    #endregion Unity Events

    #region Public Methods
    
    public static void Setup(Camera camera, params Renderer[] renderers)
    {
        CommandBufferManager manager = camera.gameObject.AddComponent<CommandBufferManager>();
        manager.targetCamera = camera;
        manager.targetRenderers = renderers;
    }
    
    #endregion Public Methods

    #region Private Methods

    private void SetupEnabledStateCollection()
    {
        if (rendererEnabledStates != null)
            Array.Resize(ref rendererEnabledStates, targetRenderers.Length);
        else
            rendererEnabledStates = new bool[targetRenderers.Length];
    }
    
    private void SetupCommandBuffer()
    {
        commandBuffer = new CommandBuffer();
        commandBuffer.name = CommandBufferName;

        // Set render target and clear depth
        commandBuffer.SetRenderTarget(new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget, 
            0, CubemapFace.Unknown, RenderTargetIdentifier.AllDepthSlices));
        
        commandBuffer.ClearRenderTarget(true, false, Color.clear);

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer renderer = targetRenderers[i];
            if (renderer == null || !rendererEnabledStates[i]) 
                continue;
            
            commandBuffer.DrawRenderer(renderer, renderer.sharedMaterial);
            renderer.forceRenderingOff = true;
        }

        targetCamera.AddCommandBuffer(CameraEvent.AfterImageEffects, commandBuffer);
        
        Debug.Log($"Command buffer setup for {targetCamera.name} with {targetRenderers.Length} renderers.");
    }

    private void RebuildCommandBuffer()
    {
        CleanupCommandBuffer();
        SetupCommandBuffer();
    }

    private void CleanupCommandBuffer()
    {
        if (targetCamera == null || commandBuffer == null) 
            return;
        
        // Re-enable normal rendering for all renderers
        if (targetRenderers != null)
        {
            foreach (Renderer renderer in targetRenderers)
            {
                if (renderer != null)
                    renderer.forceRenderingOff = false;
            }
        }
        
        targetCamera.RemoveCommandBuffer(CameraEvent.AfterImageEffects, commandBuffer);
        commandBuffer = null;
    }
    
    #endregion Private Methods
}