using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI_RC.Systems.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace NAK.LegacyContentMitigation;

public class FakeMultiPassHack : MonoBehaviour
{
    private static readonly int s_WorldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");

    public static Action<bool> OnMultiPassActiveChanged;

    #region Properties
    
    public static FakeMultiPassHack Instance { get; set; }
    public bool IsActive => IsEnabled && isActiveAndEnabled;
    public bool IsEnabled { get; private set; }
    public Camera.MonoOrStereoscopicEye RenderingEye { get; private set; }

    #endregion

    #region Private Fields
    
    private Camera _mainCamera;
    private Camera _leftEye;
    private Camera _rightEye;
    
    private GameObject _leftEyeObject;
    private GameObject _rightEyeObject;
    
    private RenderTexture _leftTexture;
    private RenderTexture _rightTexture;

    private CommandBuffer _shaderGlobalBuffer;
    private CommandBuffer _leftEyeBuffer;
    
    private int CachedCullingMask;
    private bool _isInitialized;
    
    #endregion

    #region Unity Lifecycle
    
    private void OnEnable()
    {
        Camera.onPreRender += OnPreRenderCallback;
        if (IsEnabled) _mainCamera.cullingMask = 0;
    }
    
    private void OnDisable()
    {
        Camera.onPreRender -= OnPreRenderCallback;
        if (IsEnabled) _mainCamera.cullingMask = CachedCullingMask;
    }

    private void OnDestroy()
    {
        if (_leftEye != null) RemoveCameraFromWorldTransitionSystem(_leftEye);
        if (_rightEye != null) RemoveCameraFromWorldTransitionSystem(_rightEye);
        
        if (_leftTexture != null) _leftTexture.Release();
        if (_rightTexture != null) _rightTexture.Release();
        _shaderGlobalBuffer?.Release();
        _leftEyeBuffer?.Release();

        if (_leftEyeObject != null) Destroy(_leftEyeObject);
        if (_rightEyeObject != null) Destroy(_rightEyeObject);
        
        return;
        void RemoveCameraFromWorldTransitionSystem(Camera cam)
        {
            if (cam.TryGetComponent(out WorldTransitionCamera effectCam)) Destroy(effectCam);
            WorldTransitionSystem.Cameras.Remove(cam);
        }
    }
    
    #endregion

    #region Public Methods
    
    public void SetMultiPassActive(bool active)
    {
        if (active == IsEnabled) return;
        IsEnabled = active;
        
        if (active && !_isInitialized) DoInitialSetup();

        _mainCamera.cullingMask = IsActive ? 0 : CachedCullingMask;

        OnMultiPassActiveChanged?.Invoke(active);
    }
    
    public void OnMainCameraChanged()
    {
        if (!_isInitialized) return;
        
        CachedCullingMask = _mainCamera.cullingMask;
        if (IsActive) _mainCamera.cullingMask = 0;
        
        CVRTools.CopyToDestCam(_mainCamera, _leftEye);
        CVRTools.CopyToDestCam(_mainCamera, _rightEye);
    }
    
    #endregion

    #region Initialization
    
    private void DoInitialSetup()
    {
        _mainCamera = GetComponent<Camera>();
        CachedCullingMask = _mainCamera.cullingMask;
        
        _shaderGlobalBuffer = new CommandBuffer();
        _leftEyeBuffer = new CommandBuffer();
        
        SetupEye("Left Eye", out _leftEyeObject, out _leftEye, _leftEyeBuffer);
        SetupEye("Right Eye", out _rightEyeObject, out _rightEye, null);
        
        _isInitialized = true;
        
        return;
        void SetupEye(string camName, out GameObject eyeObj, out Camera eye, CommandBuffer eyeBuffer)
        {
            eyeObj = new GameObject(camName);
            eyeObj.transform.parent = transform;
            eyeObj.transform.localScale = Vector3.one;
            eye = eyeObj.AddComponent<Camera>();
            eye.enabled = false;
            
            // Correct camera world space pos (nameplate shader)
            eye.AddCommandBuffer(CameraEvent.BeforeDepthTexture, _shaderGlobalBuffer);
            eye.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _shaderGlobalBuffer);
            eye.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _shaderGlobalBuffer);

            // normalizedViewport parameter is ignored, so we cannot draw mesh on right eye :)
            if (eyeBuffer != null)
            {
                eye.AddCommandBuffer(CameraEvent.BeforeDepthTexture, eyeBuffer);
                eye.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, eyeBuffer);
                eye.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, eyeBuffer);
                // notice how we pass fucked param vs UnityEngine.Rendering.XRUtils
                eyeBuffer.DrawOcclusionMesh(new RectInt(0, 0, 0, 0));
            }
            
            WorldTransitionSystem.AddCamera(eye);
            CVRTools.CopyToDestCam(_mainCamera, eye);
        }
    }
    
    #endregion

    #region Rendering
    
    private void OnPreRenderCallback(Camera cam)
    {
        if (!IsEnabled || !_isInitialized) return;
        
        if (cam.CompareTag("MainCamera"))
        {
            EnsureRenderTexturesCreated();
            RenderEyePair();
        }
    }
    
    private void EnsureRenderTexturesCreated()
    {
        int eyeWidth = XRSettings.eyeTextureWidth;
        int eyeHeight = XRSettings.eyeTextureHeight;

        bool needsUpdate = _leftTexture == null || _rightTexture == null ||
                          _leftTexture.width != eyeWidth || _leftTexture.height != eyeHeight;

        if (!needsUpdate) return;
        
        if (_leftTexture != null) _leftTexture.Release();
        if (_rightTexture != null) _rightTexture.Release();
            
        _leftTexture = new RenderTexture(eyeWidth, eyeHeight, 24, RenderTextureFormat.ARGBHalf);
        _rightTexture = new RenderTexture(eyeWidth, eyeHeight, 24, RenderTextureFormat.ARGBHalf);
    }

    private void RenderEyePair()
    {
        _shaderGlobalBuffer.Clear();
        _shaderGlobalBuffer.SetGlobalVector(s_WorldSpaceCameraPos, _mainCamera.transform.position);

        Camera realVRCamera = PlayerSetup.Instance.vrCam;
        
        RenderingEye = Camera.MonoOrStereoscopicEye.Left;
        PlayerSetup.Instance.vrCam = _leftEye; // so we trigger head hiding
        RenderEye(_leftEye, _leftTexture, Camera.StereoscopicEye.Left);
        
        RenderingEye = Camera.MonoOrStereoscopicEye.Right;
        PlayerSetup.Instance.vrCam = _rightEye; // so we trigger head hiding
        RenderEye(_rightEye, _rightTexture, Camera.StereoscopicEye.Right);

        RenderingEye = Camera.MonoOrStereoscopicEye.Mono; // bleh
        PlayerSetup.Instance.vrCam = realVRCamera; // reset back to real cam
        
        return;
        void RenderEye(Camera eyeCamera, RenderTexture targetTexture, Camera.StereoscopicEye eye)
        {
            eyeCamera.CopyFrom(_mainCamera);
            eyeCamera.targetTexture = targetTexture;
            eyeCamera.cullingMask = CachedCullingMask;
            eyeCamera.stereoTargetEye = StereoTargetEyeMask.None;
            eyeCamera.cullingMatrix = _mainCamera.cullingMatrix;
            eyeCamera.projectionMatrix = _mainCamera.GetStereoProjectionMatrix(eye);
            eyeCamera.worldToCameraMatrix = _mainCamera.GetStereoViewMatrix(eye);
            eyeCamera.Render();
            eyeCamera.ResetCullingMatrix();
        }
    }
    
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!IsEnabled || !_isInitialized || _leftTexture == null || _rightTexture == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        Graphics.CopyTexture(_leftTexture, 0, destination, 0);
        Graphics.CopyTexture(_rightTexture, 0, destination, 1);
    }
    #endregion
}