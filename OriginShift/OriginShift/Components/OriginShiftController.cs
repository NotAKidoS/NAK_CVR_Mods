using UnityEngine;

#if !UNITY_EDITOR
using UnityEngine.SceneManagement;
using NAK.OriginShift.Utility;
#endif

// Creator Exposed component

namespace NAK.OriginShift.Components;

public class OriginShiftController : MonoBehaviour
{
    public static OriginShiftController Instance { get; private set; }
        
    #region Serialized Fields
        
    [Header("Config / Shift Params")]
        
    [SerializeField] private bool _shiftVertical = true;
    [SerializeField] [Range(10, 2500)] private int _shiftThreshold = 15;
        
    [Header("Config / Scene Objects")]
        
    [SerializeField] private bool _autoMoveSceneRoots = true;
    [SerializeField] private Transform[] _toShiftTransforms = Array.Empty<Transform>();

    [Header("Config / Additive Objects")] 
    
    [SerializeField] private bool _shiftRemotePlayers = true;
    [SerializeField] private bool _shiftSpawnedObjects = true;

    #endregion Serialized Fields

    #region Internal Fields

    internal bool IsForced { get; set; }

    #endregion Internal Fields
    
#if !UNITY_EDITOR
    
    public static int ORIGIN_SHIFT_THRESHOLD = 15;
        
    #region Unity Events

    private void Awake()
    {
            if (Instance != null
                && Instance != this)
            {
                Destroy(this);
                OriginShiftMod.Logger.Error("Only one OriginShiftController can exist in a scene.");
                return;
            }
            Instance = this;
        }
        
    private void Start()
    {
            // set threshold (we can not support dynamic threshold change)
            ORIGIN_SHIFT_THRESHOLD = IsForced ? 1000 : _shiftThreshold;
        
            OriginShiftManager.OnOriginShifted += OnOriginShifted;
            OriginShiftManager.Instance.SetupManager(IsForced);
            
            // if auto, we will just move everything :)
            if (_autoMoveSceneRoots)
                GetAllSceneRootTransforms();
        
            // if we have scene roots, we will anchor all static renderers
            if (_toShiftTransforms.Length != 0)
                AnchorAllStaticRenderers();
        }

    private void OnDestroy()
    {
            OriginShiftManager.OnOriginShifted -= OnOriginShifted;
            OriginShiftManager.Instance.ResetManager();
        }
        
    #endregion Unity Events

    #region Private Methods

    private void GetAllSceneRootTransforms()
    {
            Scene scene = gameObject.scene;
            var sceneRoots = scene.GetRootGameObjects();
            _toShiftTransforms = new Transform[sceneRoots.Length + 1]; // +1 for the static batch anchor
            for (var i = 0; i < sceneRoots.Length; i++) _toShiftTransforms[i] = sceneRoots[i].transform;
        }

    private void AnchorAllStaticRenderers()
    {
            // create an anchor object at 0,0,0
            Transform anchor = new GameObject("NAK.StaticBatchAnchor").transform;
            anchor.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            anchor.localScale = Vector3.one;
            
            // add to end of root transforms
            _toShiftTransforms[^1] = anchor;
            
            // crawl all children and find Renderers part of static batch
            foreach (Transform toShiftTransform in _toShiftTransforms)
            {
                var renderers = toShiftTransform.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.isPartOfStaticBatch) // access staticBatchRootTransform using reflection and override it
                        RendererReflectionUtility.SetStaticBatchRootTransform(renderer, anchor);
                }
            }
        }

    #endregion Private Methods
        
    #region Origin Shift Events
        
    private void OnOriginShifted(Vector3 shift)
    {
            foreach (Transform toShiftTransform in _toShiftTransforms)
            {
                if (toShiftTransform == null) continue; // skip nulls
                toShiftTransform.position += shift;
            }
        }
        
    #endregion Origin Shift Events
    
#endif
}