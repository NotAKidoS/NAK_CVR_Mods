namespace NAK.SuperAwesomeMod;

using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class DepthTextureFix : MonoBehaviour
{
    private Camera cam;
    private CommandBuffer beforeCommandBuffer;
    private CommandBuffer afterCommandBuffer;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        // Ensure camera generates depth texture
        cam.depthTextureMode |= DepthTextureMode.Depth;
        
        // Create command buffers
        beforeCommandBuffer = new CommandBuffer();
        beforeCommandBuffer.name = "DepthTextureFix_Before";
        
        afterCommandBuffer = new CommandBuffer();
        afterCommandBuffer.name = "DepthTextureFix_After";
        
        // Add command buffers at the right events
        cam.AddCommandBuffer(CameraEvent.BeforeDepthTexture, beforeCommandBuffer);
        cam.AddCommandBuffer(CameraEvent.AfterDepthTexture, afterCommandBuffer);
    }

    void OnPreRender()
    {
        // Set up command buffers each frame to handle dynamic changes
        SetupCommandBuffers();
    }

    void SetupCommandBuffers()
    {
        // Get current camera viewport in pixels
        Rect pixelRect = cam.pixelRect;
        
        // Before depth texture: override viewport to full screen
        beforeCommandBuffer.Clear();
        beforeCommandBuffer.SetViewport(new Rect(0, 0, Screen.width, Screen.height));
        
        // After depth texture: restore original viewport
        afterCommandBuffer.Clear();
        afterCommandBuffer.SetViewport(pixelRect);
    }

    void OnDestroy()
    {
        // Clean up
        if (cam != null)
        {
            if (beforeCommandBuffer != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.BeforeDepthTexture, beforeCommandBuffer);
                beforeCommandBuffer.Dispose();
            }
            
            if (afterCommandBuffer != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, afterCommandBuffer);
                afterCommandBuffer.Dispose();
            }
        }
    }

    void OnDisable()
    {
        if (cam != null)
        {
            if (beforeCommandBuffer != null)
                cam.RemoveCommandBuffer(CameraEvent.BeforeDepthTexture, beforeCommandBuffer);
            if (afterCommandBuffer != null)
                cam.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, afterCommandBuffer);
        }
    }

    void OnEnable()
    {
        if (cam != null && beforeCommandBuffer != null && afterCommandBuffer != null)
        {
            cam.AddCommandBuffer(CameraEvent.BeforeDepthTexture, beforeCommandBuffer);
            cam.AddCommandBuffer(CameraEvent.AfterDepthTexture, afterCommandBuffer);
        }
    }
}