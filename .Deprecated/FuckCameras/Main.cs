using ABI_RC.Core.Util.AssetFiltering;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace NAK.FuckCameras;

public class FuckCamerasMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        HarmonyInstance.Patch(
            typeof(SharedFilter).GetMethod(nameof(SharedFilter.ProcessCamera),
                BindingFlags.Public | BindingFlags.Static),
            postfix: new HarmonyMethod(typeof(FuckCamerasMod).GetMethod(nameof(OnProcessCamera),
                BindingFlags.NonPublic | BindingFlags.Static))
        );
    }

    private static void OnProcessCamera(string collectionId, Camera camera)
        => camera.gameObject.AddComponent<FuckCameraComponent>();

    public class FuckCameraComponent : MonoBehaviour
    {
        private Camera _cam;
        private int _originalMask;

        private Camera _pooledCam;
        private bool fuck;

        private void Awake()
        {
            _originalMask = _cam.cullingMask;
            _cam.Reset();
            // _cam.cullingMask = _originalMask;
        }
        
        /*private void OnPreCull()
        {
            MelonLogger.Msg("PreCull");
            if (!TryGetComponent(out _cam)) return;

            if (fuck)
            {
                // unset default layer
                _originalMask &= ~(1 << 0);
                _cam.cullingMask = _originalMask;
                Destroy(this);
                return;
            }
            
            _originalMask = _cam.cullingMask;
            _cam.cullingMask = 0;
            fuck = true;
        }*/

        /*private IEnumerator OnPostRender()
        {
            MelonLogger.Msg("PostRender");
            // Restore the original mask if it has not changed since we set it to 0
            if (_cam.cullingMask == 0) _cam.cullingMask = _originalMask;
            
            _cam.enabled = false;
            yield return new WaitForEndOfFrame();
            _cam.enabled = true;
            
            MelonLogger.Msg("FuckCameraComponent: OnPostRender called for camera: " + _cam.name);
            // Destroy now that we have saved the day
            enabled = false;
            Destroy(this);
        }*/
    }
    
    
    
    public class CameraPoolManager : MonoBehaviour
    {
        private static CameraPoolManager _instance;
        public static CameraPoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new("CameraPoolManager");
                    obj.SetActive(false);
                    _instance = obj.AddComponent<CameraPoolManager>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        private readonly List<GameObject> _cameraObjects = new();

        public Camera CreatePooledCamera(Camera source)
        {
            var go = new GameObject("PooledCamera");
            go.SetActive(false);
            var camera = go.AddComponent<Camera>();
            camera.CopyFrom(source);
            _cameraObjects.Add(go);
            return camera;
        }

        public void RestoreCameraProperties(Camera target, Camera pooledCam)
        {
            // target.CopyFrom(pooledCam);
            CopyCameraProperties(pooledCam, target);
        }

        public void ReleasePooledCamera(Camera pooledCam)
        {
            if (pooledCam != null)
            {
                var go = pooledCam.gameObject;
                _cameraObjects.Remove(go);
                Destroy(go);
            }
        }
                
        // Skipped known diffs:
        // nearClipPlane
        // farClipPlane
        // fieldOfView
        // aspect
        // cullingMask
        // useOcclusionCulling
        // clearFlags
        // depthTextureMode
        // pixelRect
        // targetTexture

        public static void CopyCameraProperties(Camera source, Camera target)
        {
            if (source == null || target == null) return;

            target.nearClipPlane = source.nearClipPlane;
            target.farClipPlane = source.farClipPlane;
            
            target.fieldOfView = source.fieldOfView;
            target.aspect = source.aspect;

            int cullingMask = 0;

            cullingMask = (cullingMask & ~(1 << 0)) | (source.cullingMask & (1 << 0));
            
            cullingMask = (cullingMask & ~(1 << 1)) | (source.cullingMask & (1 << 1));
            cullingMask = (cullingMask & ~(1 << 2)) | (source.cullingMask & (1 << 2));
            cullingMask = (cullingMask & ~(1 << 3)) | (source.cullingMask & (1 << 3));
            cullingMask = (cullingMask & ~(1 << 4)) | (source.cullingMask & (1 << 4));
            
            cullingMask = (cullingMask & ~(1 << 5)) | (source.cullingMask & (1 << 5));
            cullingMask = (cullingMask & ~(1 << 6)) | (source.cullingMask & (1 << 6));
            cullingMask = (cullingMask & ~(1 << 7)) | (source.cullingMask & (1 << 7));
            cullingMask = (cullingMask & ~(1 << 8)) | (source.cullingMask & (1 << 8));
            
            cullingMask = (cullingMask & ~(1 << 9)) | (source.cullingMask & (1 << 9));
            cullingMask = (cullingMask & ~(1 << 10)) | (source.cullingMask & (1 << 10));
            cullingMask = (cullingMask & ~(1 << 11)) | (source.cullingMask & (1 << 11));
            cullingMask = (cullingMask & ~(1 << 12)) | (source.cullingMask & (1 << 12));
            cullingMask = (cullingMask & ~(1 << 13)) | (source.cullingMask & (1 << 13));
            cullingMask = (cullingMask & ~(1 << 14)) | (source.cullingMask & (1 << 14));
            cullingMask = (cullingMask & ~(1 << 15)) | (source.cullingMask & (1 << 15));
            cullingMask = (cullingMask & ~(1 << 16)) | (source.cullingMask & (1 << 16));
            cullingMask = (cullingMask & ~(1 << 17)) | (source.cullingMask & (1 << 17));
            cullingMask = (cullingMask & ~(1 << 18)) | (source.cullingMask & (1 << 18));
            cullingMask = (cullingMask & ~(1 << 19)) | (source.cullingMask & (1 << 19));
            cullingMask = (cullingMask & ~(1 << 20)) | (source.cullingMask & (1 << 20));
            cullingMask = (cullingMask & ~(1 << 21)) | (source.cullingMask & (1 << 21));
            cullingMask = (cullingMask & ~(1 << 22)) | (source.cullingMask & (1 << 22));
            cullingMask = (cullingMask & ~(1 << 23)) | (source.cullingMask & (1 << 23));
            cullingMask = (cullingMask & ~(1 << 24)) | (source.cullingMask & (1 << 24));
            cullingMask = (cullingMask & ~(1 << 25)) | (source.cullingMask & (1 << 25));
            cullingMask = (cullingMask & ~(1 << 26)) | (source.cullingMask & (1 << 26));
            cullingMask = (cullingMask & ~(1 << 27)) | (source.cullingMask & (1 << 27));
            cullingMask = (cullingMask & ~(1 << 28)) | (source.cullingMask & (1 << 28));
            cullingMask = (cullingMask & ~(1 << 29)) | (source.cullingMask & (1 << 29));
            cullingMask = (cullingMask & ~(1 << 30)) | (source.cullingMask & (1 << 30));
            cullingMask = (cullingMask & ~(1 << 31)) | (source.cullingMask & (1 << 31));

            target.cullingMask = cullingMask;
            
            target.clearFlags = source.clearFlags;
            target.depthTextureMode = source.depthTextureMode;
            target.useOcclusionCulling = source.useOcclusionCulling;
            target.pixelRect = source.pixelRect;
            target.targetTexture = source.targetTexture;
            
            target.renderingPath = source.renderingPath;
            target.allowHDR = source.allowHDR;
            target.allowMSAA = source.allowMSAA;
            target.allowDynamicResolution = source.allowDynamicResolution;
            target.forceIntoRenderTexture = source.forceIntoRenderTexture;
            target.orthographic = source.orthographic;
            target.orthographicSize = source.orthographicSize;
            target.depth = source.depth;
            target.eventMask = source.eventMask;
            target.layerCullSpherical = source.layerCullSpherical;
            target.backgroundColor = source.backgroundColor;
            target.clearStencilAfterLightingPass = source.clearStencilAfterLightingPass;
            target.usePhysicalProperties = source.usePhysicalProperties;
            // target.iso = source.iso;
            // target.shutterSpeed = source.shutterSpeed;
            // target.aperture = source.aperture;
            // target.focusDistance = source.focusDistance;
            // target.bladeCount = source.bladeCount;
            // target.curvature = source.curvature;
            // target.barrelClipping = source.barrelClipping;
            // target.anamorphism = source.anamorphism;
            // target.enabled = source.enabled;
            // target.transform.position = source.transform.position;
            // target.transform.rotation = source.transform.rotation;
            // target.transform.localScale = source.transform.localScale;
            target.focalLength = source.focalLength;
            target.sensorSize = source.sensorSize;
            target.lensShift = source.lensShift;
            target.gateFit = source.gateFit;
            target.rect = source.rect;

            target.targetDisplay = source.targetDisplay;
            target.stereoSeparation = source.stereoSeparation;
            target.stereoConvergence = source.stereoConvergence;
            target.stereoTargetEye = source.stereoTargetEye;
        }
    }
}
