// using ABI_RC.Core.Player.Interaction.RaycastImpl;
// using ABI_RC.Core.Savior;
// using ABI_RC.Systems.InputManagement;
// using UnityEngine;
//
// namespace ABI_RC.Core.Player.Interaction
// {
//     public class CVRPlayerInteractionManager : MonoBehaviour
//     {
//         #region Singleton
//
//         public static CVRPlayerInteractionManager Instance { get; private set; }
//
//         #endregion Singleton
//         
//         #region Serialized Fields
//         
//         [Header("Hand Components")]
//         [SerializeField] private CVRPlayerHand handVrLeft;
//         [SerializeField] private CVRPlayerHand handVrRight;
//         [SerializeField] private CVRPlayerHand handDesktopRight; // Desktop does not have a left hand
//         
//         [Header("Raycast Transforms")]
//         [SerializeField] private Transform raycastTransformVrRight;
//         [SerializeField] private Transform raycastTransformVrLeft;
//         [SerializeField] private Transform raycastTransformDesktopRight;
//
//         [Header("Settings")]
//         [SerializeField] private bool interactionEnabled = true;
//         [SerializeField] private LayerMask interactionLayerMask = -1; // Default to all layers, will be filtered
//         
//         #endregion Serialized Fields
//         
//         #region Properties
//         
//         private CVRPlayerHand _rightHand;
//         private CVRPlayerHand _leftHand;
//         
//         private CVRPlayerRaycaster _rightRaycaster;
//         private CVRPlayerRaycaster _leftRaycaster;
//         
//         private CVRRaycastResult _rightRaycastResult;
//         private CVRRaycastResult _leftRaycastResult;
//
//         // Input handler
//         private CVRPlayerInputHandler _inputHandler;
//         
//         // Interaction flags
//         public bool InteractionEnabled
//         {
//             get => interactionEnabled;
//             set => interactionEnabled = value;
//         }
//         
//         #endregion Properties
//         
//         #region Unity Events
//
//         private void Awake()
//         {
//             if (Instance != null && Instance != this)
//             {
//                 Destroy(gameObject);
//                 return;
//             }
//             Instance = this;
//             
//             // Create the input handler
//             _inputHandler = gameObject.AddComponent<CVRPlayerInputHandler>();
//         }
//
//         private void Start()
//         {
//             // Setup interaction for current device mode
//             SetupInteractionForDeviceMode();
//             
//             // Listen for VR mode changes
//             MetaPort.Instance.onVRModeSwitch.AddListener(SetupInteractionForDeviceMode);
//         }
//
//         private void Update()
//         {
//             if (!interactionEnabled)
//                 return;
//                 
//             // Process right hand
//             if (_rightRaycaster != null)
//             {
//                 // Determine raycast flags based on current mode
//                 CVRPlayerRaycaster.RaycastFlags flags = DetermineRaycastFlags(_rightHand);
//                 
//                 // Get raycast results
//                 _rightRaycastResult = _rightRaycaster.GetRaycastResults(flags);
//                 
//                 // Process input based on raycast results
//                 _inputHandler.ProcessInput(CVRHand.Right, _rightRaycastResult);
//             }
//             
//             // Process left hand (if available)
//             if (_leftRaycaster != null)
//             {
//                 // Determine raycast flags based on current mode
//                 CVRPlayerRaycaster.RaycastFlags flags = DetermineRaycastFlags(_leftHand);
//                 
//                 // Get raycast results
//                 _leftRaycastResult = _leftRaycaster.GetRaycastResults(flags);
//                 
//                 // Process input based on raycast results
//                 _inputHandler.ProcessInput(CVRHand.Left, _leftRaycastResult);
//             }
//         }
//
//         private void OnDestroy()
//         {
//             // Clean up event listener
//             if (MetaPort.Instance != null)
//                 MetaPort.Instance.onVRModeSwitch.RemoveListener(SetupInteractionForDeviceMode);
//         }
//
//         #endregion Unity Events
//
//         #region Public Methods
//
//         /// <summary>
//         /// Register a custom tool mode
//         /// </summary>
//         public void RegisterCustomToolMode(System.Action<CVRHand, CVRRaycastResult, InputState> callback)
//         {
//             _inputHandler.RegisterCustomTool(callback);
//         }
//         
//         /// <summary>
//         /// Unregister the current custom tool mode
//         /// </summary>
//         public void UnregisterCustomToolMode()
//         {
//             _inputHandler.UnregisterCustomTool();
//         }
//         
//         /// <summary>
//         /// Set the interaction mode
//         /// </summary>
//         public void SetInteractionMode(CVRPlayerInputHandler.InteractionMode mode)
//         {
//             _inputHandler.SetInteractionMode(mode);
//         }
//         
//         /// <summary>
//         /// Get the raycast result for a specific hand
//         /// </summary>
//         public CVRRaycastResult GetRaycastResult(CVRHand hand)
//         {
//             return hand == CVRHand.Left ? _leftRaycastResult : _rightRaycastResult;
//         }
//
//         #endregion Public Methods
//
//         #region Private Methods
//         
//         private void SetupInteractionForDeviceMode()
//         {
//             bool isVr = MetaPort.Instance.isUsingVr;
//             
//             if (isVr)
//             {
//                 // VR mode
//                 _rightHand = handVrRight;
//                 _leftHand = handVrLeft;
//                 
//                 // VR uses the controller transform for raycasting
//                 _rightRaycaster = new CVRPlayerRaycasterTransform(raycastTransformVrRight);
//                 _leftRaycaster = new CVRPlayerRaycasterTransform(raycastTransformVrLeft);
//             }
//             else
//             {
//                 // Desktop mode
//                 _rightHand = handDesktopRight;
//                 _leftHand = null;
//                 
//                 // Desktop uses the mouse position for raycasting when unlocked
//                 Camera desktopCamera = PlayerSetup.Instance.desktopCam;
//                 _rightRaycaster = new CVRPlayerRaycasterMouse(raycastTransformDesktopRight, desktopCamera);
//                 _leftRaycaster = null;
//             }
//             
//             // Set the layer mask for raycasters
//             if (_rightRaycaster != null)
//                 _rightRaycaster.SetLayerMask(interactionLayerMask);
//                 
//             if (_leftRaycaster != null)
//                 _leftRaycaster.SetLayerMask(interactionLayerMask);
//         }
//         
//         private static CVRPlayerRaycaster.RaycastFlags DetermineRaycastFlags(CVRPlayerHand hand)
//         {
//             // Default to all flags
//             CVRPlayerRaycaster.RaycastFlags flags = CVRPlayerRaycaster.RaycastFlags.All;
//             
//             // Check if hand is holding a pickup
//             if (hand != null && hand.IsHoldingObject)
//             {
//                 // When holding an object, only check for COHTML interaction
//                 flags = CVRPlayerRaycaster.RaycastFlags.CohtmlInteract;
//             }
//             
//             // Could add more conditional flag adjustments here based on the current mode
//             // For example, in a teleport tool mode, you might only want world hits
//             
//             return flags;
//         }
//
//         #endregion Private Methods
//     }
// }