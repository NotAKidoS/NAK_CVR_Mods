using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using NAK.LegacyContentMitigation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace LegacyContentMitigation.Components;

public class FaceMirror : MonoBehaviour
{
    private Camera _parentCamera;
    private Camera _camera;
    public Rect shiftRect;
    private CommandBuffer _viewportBuffer;

    private void Start() {
        _parentCamera = GetComponent<Camera>();
        _camera = new GameObject("Face Mirror").AddComponent<Camera>();
        _camera.transform.parent = transform;
        _camera.CopyFrom(_parentCamera);
        _camera.ResetReplacementShader();
        _camera.depth = 99;
        _camera.clearFlags = CameraClearFlags.Depth;
        _camera.transform.position += transform.forward * 0.5f;
        _camera.transform.rotation *= Quaternion.Euler(0, 180, 0);

        // View only CVRLayers.PlayerLocal
        _camera.cullingMask = 1 << CVRLayers.PlayerLocal;

        // Create and cache the command buffer
        _viewportBuffer = new CommandBuffer();
        _viewportBuffer.SetViewport(shiftRect);
    
        _camera.AddCommandBuffer(CameraEvent.BeforeDepthTexture, _viewportBuffer);
        _camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _viewportBuffer);
        _camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _viewportBuffer);
        _camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, _viewportBuffer);
    }

    private void Update()
    {
        if (ModSettings.EntryUseFaceMirror.Value == false)
        {
            _camera.enabled = false;
            return;
        }
        _camera.enabled = true;
        
        // Update camera distance
        _camera.transform.localPosition = Vector3.forward * ModSettings.EntryFaceMirrorDistance.Value;

        // Get the display resolution based on VR status
        int displayWidth, displayHeight;
        if (MetaPort.Instance.isUsingVr)
        {
            displayWidth = XRSettings.eyeTextureWidth;
            displayHeight = XRSettings.eyeTextureHeight;
        }
        else 
        {
            displayWidth = Screen.width;
            displayHeight = Screen.height;
        }

        // Calculate pixel sizes first
        float pixelSizeX = ModSettings.EntryFaceMirrorSizeX.Value * displayWidth;
        float pixelSizeY = ModSettings.EntryFaceMirrorSizeY.Value * displayHeight;

        // Calculate offsets from center
        float pixelOffsetX = (ModSettings.EntryFaceMirrorOffsetX.Value * displayWidth) - (pixelSizeX * 0.5f) + (displayWidth * 0.5f);
        float pixelOffsetY = (ModSettings.EntryFaceMirrorOffsetY.Value * displayHeight) - (pixelSizeY * 0.5f) + (displayHeight * 0.5f);

        _camera.transform.localScale = Vector3.one * ModSettings.EntryFaceMirrorCameraScale.Value;
        
        Vector3 playerup = PlayerSetup.Instance.transform.up;
        Vector3 cameraForward = _parentCamera.transform.forward;

        // Check if playerup and cameraForward are nearly aligned
        if (Mathf.Abs(Vector3.Dot(playerup, cameraForward)) <= Mathf.Epsilon) {
            playerup = -_parentCamera.transform.forward;
            cameraForward = _parentCamera.transform.up;
        }
        
        _camera.transform.rotation = Quaternion.LookRotation(-cameraForward, playerup);
        
        // Create viewport rect with pixel values
        shiftRect = new Rect(
            pixelOffsetX,
            pixelOffsetY,
            pixelSizeX,
            pixelSizeY
        );

        // Update the cached buffer's viewport
        _viewportBuffer.Clear();
        _viewportBuffer.SetViewport(shiftRect);
    }
}